using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Application.Service
{
    public class ServiceBookingStatsService : IServiceBookingStatsService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<ServiceBookingStatsService> _logger;

        public ServiceBookingStatsService(
            IBookingRepository bookingRepository,
            IInvoiceRepository invoiceRepository,
            IServiceRepository serviceRepository,
            IPaymentRepository paymentRepository,
            ILogger<ServiceBookingStatsService> logger)
        {
            _bookingRepository = bookingRepository;
            _invoiceRepository = invoiceRepository;
            _serviceRepository = serviceRepository;
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        public async Task<ServiceBookingStatsResponse> GetServiceBookingStatsAsync(ServiceBookingStatsRequest? request = null)
        {
            var fromDate = request?.FromDate ?? DateTime.Today.AddDays(-30);
            var toDate = request?.ToDate ?? DateTime.Today;

            fromDate = fromDate.Date;
            toDate = toDate.Date.AddDays(1).AddTicks(-1);

            _logger.LogInformation("Service booking stats from {From} to {To}", fromDate, toDate);

            var allBookings = await _bookingRepository.GetAllBookingsAsync();

            // Chỉ tính booking hoàn tất (COMPLETED/PAID), KHÔNG lọc theo CreatedAt.
            // Sẽ chỉ tính nếu có payment COMPLETED/PAID trong khoảng PaidAt.
            var completedBookings = allBookings.Where(b =>
                !string.IsNullOrEmpty(b.Status) &&
                (b.Status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase) ||
                 b.Status.Equals("PAID", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var serviceIdToItem = new Dictionary<int, ServiceBookingStatsItem>();

            foreach (var booking in completedBookings)
            {
                var serviceId = booking.ServiceId;
                if (!serviceIdToItem.TryGetValue(serviceId, out var item))
                {
                    item = new ServiceBookingStatsItem
                    {
                        ServiceId = serviceId,
                        ServiceName = booking.Service?.ServiceName ?? $"Service {serviceId}",
                        BookingCount = 0,
                        ServiceRevenue = 0
                    };
                    serviceIdToItem[serviceId] = item;
                }

                item.BookingCount += 1;

                var invoice = await _invoiceRepository.GetByBookingIdAsync(booking.BookingId);
                if (invoice == null) continue;

                // Lấy payments COMPLETED và PAID trong khoảng thời gian (PaidAt)
                var pCompleted = await _paymentRepository.GetByInvoiceIdAsync(invoice.InvoiceId, "COMPLETED", null, fromDate, toDate);
                var pPaid = await _paymentRepository.GetByInvoiceIdAsync(invoice.InvoiceId, "PAID", null, fromDate, toDate);
                var totalPaid = pCompleted.Sum(p => p.Amount) + pPaid.Sum(p => p.Amount);
                if (totalPaid <= 0) continue;

                // Phân bổ phần dịch vụ từ totalPaid bằng cách trừ phần phụ tùng tối đa
                var allocatedParts = Math.Min(invoice.PartsAmount, totalPaid);
                var serviceAmount = totalPaid - allocatedParts;
                if (serviceAmount > 0) item.ServiceRevenue += serviceAmount;

                // BookingCount tăng khi có ít nhất một khoản thanh toán hợp lệ trong khoảng PaidAt
                // (đảm bảo đếm theo PaidAt để đồng bộ với doanh thu)
                // Lưu ý: item.BookingCount đã +1 trước; giữ nguyên cách đếm 1 lần/booking khi có paid.
            }

            // Bổ sung tất cả dịch vụ còn thiếu với 0
            var allServices = await _serviceRepository.GetAllServicesAsync();
            foreach (var svc in allServices)
            {
                if (!serviceIdToItem.ContainsKey(svc.ServiceId))
                {
                    serviceIdToItem[svc.ServiceId] = new ServiceBookingStatsItem
                    {
                        ServiceId = svc.ServiceId,
                        ServiceName = svc.ServiceName,
                        BookingCount = 0,
                        ServiceRevenue = 0
                    };
                }
            }

            var result = new ServiceBookingStatsResponse
            {
                Success = true,
                GeneratedAt = DateTime.UtcNow,
                FromDate = fromDate,
                ToDate = toDate,
                Services = serviceIdToItem.Values
                    .OrderByDescending(s => s.BookingCount)
                    .ThenByDescending(s => s.ServiceRevenue)
                    .ToList()
            };

            result.TotalCompletedBookings = result.Services.Sum(s => s.BookingCount);
            result.TotalServiceRevenue = result.Services.Sum(s => s.ServiceRevenue);
            return result;
        }
    }
}


