using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.IRepositories;

namespace EVServiceCenter.Application.Service
{
    public class RevenueByStoreService : IRevenueByStoreService
    {
        private readonly ICenterRepository _centerRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IPaymentRepository _paymentRepository;

        public RevenueByStoreService(
            ICenterRepository centerRepository,
            IBookingRepository bookingRepository,
            IInvoiceRepository invoiceRepository,
            IPaymentRepository paymentRepository)
        {
            _centerRepository = centerRepository;
            _bookingRepository = bookingRepository;
            _invoiceRepository = invoiceRepository;
            _paymentRepository = paymentRepository;
        }

        public async Task<RevenueByStoreResponse> GetRevenueByStoreAsync(RevenueByStoreRequest? request = null)
        {
            try
            {
                var fromDate = request?.FromDate ?? DateTime.Today.AddDays(-30);
                var toDate = request?.ToDate ?? DateTime.Today;

                fromDate = fromDate.Date;
                toDate = toDate.Date.AddDays(1).AddTicks(-1);

                var allCenters = await _centerRepository.GetAllCentersAsync();
                var activeCenters = allCenters.Where(c => c.IsActive).ToList();

                if (!activeCenters.Any())
                {
                    return new RevenueByStoreResponse
                    {
                        Success = true,
                        GeneratedAt = DateTime.UtcNow,
                        FromDate = fromDate,
                        ToDate = toDate,
                        Stores = new List<StoreRevenueData>(),
                        TotalRevenue = 0
                    };
                }

                var storeRevenues = new List<StoreRevenueData>();
                decimal totalRevenueAllStores = 0;

                foreach (var center in activeCenters)
                {
                    var storeRevenue = await CalculateStoreRevenueAsync(center.CenterId, center.CenterName, 
                        fromDate, toDate);
                    
                    storeRevenues.Add(storeRevenue);
                    totalRevenueAllStores += storeRevenue.Revenue;
                }

                storeRevenues = storeRevenues.OrderByDescending(s => s.Revenue).ToList();

                return new RevenueByStoreResponse
                {
                    Success = true,
                    GeneratedAt = DateTime.UtcNow,
                    FromDate = fromDate,
                    ToDate = toDate,
                    Stores = storeRevenues,
                    TotalRevenue = totalRevenueAllStores
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<StoreRevenueData> CalculateStoreRevenueAsync(
            int centerId,
            string centerName,
            DateTime fromDate,
            DateTime toDate)
        {
            try
            {
                var completedBookingPayments = await _paymentRepository.GetCompletedPaymentsByCenterAndDateRangeAsync(
                    centerId, 
                    fromDate, 
                    toDate);

                var statuses = new[] { "PAID" };
                var allPaidPayments = await _paymentRepository.GetPaymentsByStatusesAndDateRangeAsync(
                    statuses, 
                    fromDate, 
                    toDate);

                var paidBookingPayments = allPaidPayments
                    .Where(p => p.Invoice != null 
                             && p.Invoice.BookingId != null 
                             && p.Invoice.Booking != null 
                             && p.Invoice.Booking.CenterId == centerId)
                    .ToList();

                var bookingRevenue = completedBookingPayments.Sum(p => (decimal)p.Amount) 
                                   + paidBookingPayments.Sum(p => (decimal)p.Amount);

                var orderPayments = await _paymentRepository.GetCompletedPaymentsByFulfillmentCenterAndDateRangeAsync(
                    centerId, 
                    fromDate, 
                    toDate);

                var orderRevenue = orderPayments.Sum(p => (decimal)p.Amount);

                var totalRevenue = bookingRevenue + orderRevenue;

                var completedBookingInvoiceIds = completedBookingPayments
                    .Select(p => p.InvoiceId)
                    .Concat(paidBookingPayments.Select(p => p.InvoiceId))
                    .Distinct()
                    .ToList();

                var completedCount = completedBookingInvoiceIds.Count;

                return new StoreRevenueData
                {
                    StoreId = centerId,
                    StoreName = centerName,
                    Revenue = totalRevenue,
                    CompletedBookings = completedCount
                };
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}

