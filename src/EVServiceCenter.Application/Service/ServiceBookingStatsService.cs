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

            // Tối ưu: Lấy tất cả payments COMPLETED/PAID trong date range (theo PaidAt) với Invoice và Booking đã include
            // Đảm bảo đồng nhất với DashboardSummaryService và RevenueByStoreService
            var statuses = new[] { "COMPLETED", "PAID" };
            var payments = await _paymentRepository.GetPaymentsByStatusesAndDateRangeAsync(
                statuses, 
                fromDate, 
                toDate);

            // Lọc chỉ payments có Invoice và Booking (loại bỏ payments từ Order)
            var bookingPayments = payments
                .Where(p => p.Invoice != null 
                         && p.Invoice.BookingId != null 
                         && p.Invoice.Booking != null)
                .ToList();

            // Nhóm payments theo InvoiceId để tính toán
            var invoicePayments = bookingPayments
                .GroupBy(p => p.InvoiceId)
                .ToList();

            var serviceIdToItem = new Dictionary<int, ServiceBookingStatsItem>();

            foreach (var invoiceGroup in invoicePayments)
            {
                var invoice = invoiceGroup.First().Invoice;
                if (invoice == null || invoice.Booking == null) continue;

                var booking = invoice.Booking;
                var serviceId = booking.ServiceId;
                
                // Khởi tạo item nếu chưa có
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

                // Tính tổng số tiền đã thanh toán cho invoice này
                var totalPaid = invoiceGroup.Sum(p => (decimal)p.Amount);
                if (totalPaid <= 0) continue;

                // Phân bổ phần dịch vụ từ totalPaid bằng cách trừ phần phụ tùng tối đa
                // Logic đồng nhất với DashboardSummaryService
                var allocatedParts = Math.Min(invoice.PartsAmount, totalPaid);
                var serviceAmount = totalPaid - allocatedParts;
                
                if (serviceAmount > 0)
                {
                    item.ServiceRevenue += serviceAmount;
                }

                // Đếm booking: mỗi invoice unique = 1 booking
                // Chỉ đếm khi có payment trong date range (đảm bảo đồng bộ với doanh thu)
                item.BookingCount += 1;
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


