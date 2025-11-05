using System;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.IRepositories;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Application.Service
{
    /// <summary>
    /// Service implementation cho Dashboard Summary - KPI tổng quan toàn hệ thống
    /// </summary>
    public class DashboardSummaryService : IDashboardSummaryService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly ILogger<DashboardSummaryService> _logger;

        public DashboardSummaryService(
            IPaymentRepository paymentRepository,
            IAccountRepository accountRepository,
            IBookingRepository bookingRepository,
            IInvoiceRepository invoiceRepository,
            ILogger<DashboardSummaryService> logger)
        {
            _paymentRepository = paymentRepository;
            _accountRepository = accountRepository;
            _bookingRepository = bookingRepository;
            _invoiceRepository = invoiceRepository;
            _logger = logger;
        }

        /// <summary>
        /// Lấy KPI tổng quan của toàn hệ thống
        /// </summary>
        public async Task<DashboardSummaryResponse> GetDashboardSummaryAsync(DashboardSummaryRequest? request = null)
        {
            try
            {
                // Set default values nếu không có request
                var fromDate = request?.FromDate ?? DateTime.Today.AddDays(-30);
                var toDate = request?.ToDate ?? DateTime.Today;

                // Normalize dates
                fromDate = fromDate.Date;
                toDate = toDate.Date.AddDays(1).AddTicks(-1); // End of day

                _logger.LogInformation(
                    "Bắt đầu tính toán Dashboard Summary - KPI tổng quan toàn hệ thống từ {FromDate} đến {ToDate}",
                    fromDate, toDate);

                // 1. Tổng doanh thu toàn hệ thống (từ tất cả payments COMPLETED trong date range)
                var totalRevenue = await GetTotalRevenueAsync(fromDate, toDate);

                // 2. Tổng số nhân viên (STAFF + TECHNICIAN) - không phụ thuộc vào date range
                var totalEmployees = await GetTotalEmployeesAsync();

                // 3. Tổng số lịch hẹn hoàn thành (status COMPLETED hoặc PAID trong date range)
                var totalCompletedBookings = await GetTotalCompletedBookingsAsync(fromDate, toDate);

                // 4. Doanh thu từ dịch vụ (trong date range)
                var serviceRevenue = await GetServiceRevenueAsync(fromDate, toDate);

                // 5. Doanh thu từ phụ tùng (trong date range)
                var partsRevenue = await GetPartsRevenueAsync(fromDate, toDate);

                _logger.LogInformation(
                    "Dashboard Summary tính toán thành công: Revenue={TotalRevenue}, Employees={TotalEmployees}, " +
                    "CompletedBookings={TotalCompletedBookings}, ServiceRevenue={ServiceRevenue}, PartsRevenue={PartsRevenue}",
                    totalRevenue, totalEmployees, totalCompletedBookings, serviceRevenue, partsRevenue);

                return new DashboardSummaryResponse
                {
                    Success = true,
                    GeneratedAt = DateTime.UtcNow,
                    FromDate = fromDate,
                    ToDate = toDate,
                    Summary = new DashboardSummaryData
                    {
                        TotalRevenue = totalRevenue,
                        TotalEmployees = totalEmployees,
                        TotalCompletedBookings = totalCompletedBookings,
                        ServiceRevenue = serviceRevenue,
                        PartsRevenue = partsRevenue
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tính toán Dashboard Summary");
                throw;
            }
        }

        /// <summary>
        /// Tính tổng doanh thu toàn hệ thống từ tất cả payments COMPLETED trong date range
        /// </summary>
        private async Task<decimal> GetTotalRevenueAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                // Lấy tất cả invoices
                var allInvoices = await _invoiceRepository.GetAllAsync();

                decimal totalRevenue = 0;

                // Tính tổng từ tất cả payments COMPLETED của tất cả invoices trong date range
                foreach (var invoice in allInvoices)
                {
                    var completedPayments = await _paymentRepository.GetByInvoiceIdAsync(
                        invoice.InvoiceId, 
                        status: "COMPLETED", 
                        method: null, 
                        from: fromDate, 
                        to: toDate);
                    
                    totalRevenue += completedPayments.Sum(p => p.Amount);
                }

                return totalRevenue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tính tổng doanh thu toàn hệ thống");
                throw;
            }
        }

        /// <summary>
        /// Đếm tổng số nhân viên (STAFF + TECHNICIAN) của toàn hệ thống
        /// </summary>
        private async Task<int> GetTotalEmployeesAsync()
        {
            try
            {
                // Lấy tất cả users có role STAFF
                var staffUsers = await _accountRepository.GetAllUsersWithRoleAsync("STAFF");
                
                // Lấy tất cả users có role TECHNICIAN
                var technicianUsers = await _accountRepository.GetAllUsersWithRoleAsync("TECHNICIAN");

                // Chỉ đếm những user đang active
                var activeStaffCount = staffUsers.Count(u => u.IsActive);
                var activeTechnicianCount = technicianUsers.Count(u => u.IsActive);

                return activeStaffCount + activeTechnicianCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đếm tổng số nhân viên");
                throw;
            }
        }

        /// <summary>
        /// Đếm tổng số lịch hẹn hoàn thành (status COMPLETED hoặc PAID) trong date range
        /// </summary>
        private async Task<int> GetTotalCompletedBookingsAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                // Lấy tất cả bookings
                var allBookings = await _bookingRepository.GetAllBookingsAsync();

                // Đếm bookings có status COMPLETED hoặc PAID và CreatedAt trong date range
                var completedBookings = allBookings.Count(b => 
                    !string.IsNullOrEmpty(b.Status) && 
                    (b.Status.ToUpperInvariant() == "COMPLETED" || b.Status.ToUpperInvariant() == "PAID") &&
                    b.CreatedAt >= fromDate && b.CreatedAt <= toDate);

                return completedBookings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đếm tổng số booking hoàn thành");
                throw;
            }
        }

        /// <summary>
        /// Tính doanh thu từ dịch vụ của toàn hệ thống trong date range
        /// Doanh thu dịch vụ = Tổng tiền dịch vụ từ invoice (tính từ booking -> service price)
        /// </summary>
        private async Task<decimal> GetServiceRevenueAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                // Lấy tất cả bookings
                var allBookings = await _bookingRepository.GetAllBookingsAsync();

                // Chỉ tính bookings đã hoàn thành (COMPLETED hoặc PAID) trong date range
                var completedBookings = allBookings.Where(b => 
                    !string.IsNullOrEmpty(b.Status) && 
                    (b.Status.ToUpperInvariant() == "COMPLETED" || b.Status.ToUpperInvariant() == "PAID") &&
                    b.CreatedAt >= fromDate && b.CreatedAt <= toDate);

                decimal serviceRevenue = 0;

                // Với mỗi booking hoàn thành, lấy invoice và tính doanh thu dịch vụ
                foreach (var booking in completedBookings)
                {
                    var invoice = await _invoiceRepository.GetByBookingIdAsync(booking.BookingId);
                    if (invoice == null) continue;

                    // Lấy payments COMPLETED của invoice này trong date range
                    var completedPayments = await _paymentRepository.GetByInvoiceIdAsync(
                        invoice.InvoiceId, 
                        status: "COMPLETED", 
                        method: null, 
                        from: fromDate, 
                        to: toDate);

                    // Doanh thu dịch vụ = Tổng amount của payments - PartsAmount - Discounts
                    // Hoặc đơn giản hơn: tính từ Service.BasePrice của booking
                    if (booking.Service != null)
                    {
                        // Tính từ service BasePrice (chưa trừ discount)
                        var servicePrice = booking.Service.BasePrice;
                        
                        // Nếu có payments, lấy phần dịch vụ từ payments
                        // Giả sử: serviceRevenue = totalPaymentAmount - partsAmount - discounts
                        var totalPaymentAmount = completedPayments.Sum(p => p.Amount);
                        
                        // Trừ đi phần phụ tùng và discount
                        var partsAmount = invoice.PartsAmount;
                        var discounts = invoice.PackageDiscountAmount + invoice.PromotionDiscountAmount;
                        
                        // Doanh thu dịch vụ = Tổng payment - Phần phụ tùng - Discount
                        // Hoặc đơn giản: tính từ service price trừ discount
                        var serviceAmount = totalPaymentAmount > 0 
                            ? totalPaymentAmount - partsAmount - discounts
                            : servicePrice - discounts;
                        
                        // Đảm bảo không âm
                        if (serviceAmount > 0)
                        {
                            serviceRevenue += serviceAmount;
                        }
                        else if (servicePrice > 0)
                        {
                            // Fallback: nếu không có payment, dùng service price
                            serviceRevenue += servicePrice;
                        }
                    }
                }

                return serviceRevenue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tính doanh thu từ dịch vụ");
                throw;
            }
        }

        /// <summary>
        /// Tính doanh thu từ phụ tùng của toàn hệ thống trong date range
        /// Doanh thu phụ tùng = Tổng PartsAmount từ tất cả invoices của bookings đã hoàn thành
        /// </summary>
        private async Task<decimal> GetPartsRevenueAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                // Lấy tất cả invoices
                var allInvoices = await _invoiceRepository.GetAllAsync();

                decimal partsRevenue = 0;

                // Tính tổng PartsAmount từ các invoices có booking đã hoàn thành
                foreach (var invoice in allInvoices)
                {
                    if (invoice.BookingId == null) continue;

                    var booking = await _bookingRepository.GetBookingByIdAsync(invoice.BookingId.Value);
                    if (booking == null) continue;

                    // Chỉ tính invoices của bookings đã hoàn thành trong date range
                    var isCompleted = !string.IsNullOrEmpty(booking.Status) && 
                                     (booking.Status.ToUpperInvariant() == "COMPLETED" || 
                                      booking.Status.ToUpperInvariant() == "PAID") &&
                                     booking.CreatedAt >= fromDate && booking.CreatedAt <= toDate;

                    if (isCompleted)
                    {
                        // Kiểm tra xem có payment COMPLETED không trong date range
                        var completedPayments = await _paymentRepository.GetByInvoiceIdAsync(
                            invoice.InvoiceId, 
                            status: "COMPLETED", 
                            method: null, 
                            from: fromDate, 
                            to: toDate);

                        // Chỉ tính nếu có payment thành công
                        if (completedPayments.Any())
                        {
                            partsRevenue += invoice.PartsAmount;
                        }
                    }
                }

                return partsRevenue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tính doanh thu từ phụ tùng");
                throw;
            }
        }
    }
}

