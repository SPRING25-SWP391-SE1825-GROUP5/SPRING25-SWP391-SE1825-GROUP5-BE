using System;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.IRepositories;
using EVServiceCenter.Application.Constants;

namespace EVServiceCenter.Application.Service
{
    public class DashboardSummaryService : IDashboardSummaryService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IInvoiceRepository _invoiceRepository;

        public DashboardSummaryService(
            IPaymentRepository paymentRepository,
            IAccountRepository accountRepository,
            IBookingRepository bookingRepository,
            IInvoiceRepository invoiceRepository)
        {
            _paymentRepository = paymentRepository;
            _accountRepository = accountRepository;
            _bookingRepository = bookingRepository;
            _invoiceRepository = invoiceRepository;
        }

        public async Task<DashboardSummaryResponse> GetDashboardSummaryAsync(DashboardSummaryRequest? request = null)
        {
            try
            {
                var fromDate = request?.FromDate ?? DateTime.Today.AddDays(-30);
                var toDate = request?.ToDate ?? DateTime.Today;

                fromDate = fromDate.Date;
                toDate = toDate.Date.AddDays(1).AddTicks(-1);

                var totalRevenue = await GetTotalRevenueAsync(fromDate, toDate);

                var totalEmployees = await GetTotalEmployeesAsync();

                var totalCompletedBookings = await GetTotalCompletedBookingsAsync(fromDate, toDate);

                var serviceRevenue = await GetServiceRevenueAsync(fromDate, toDate);

                var partsRevenue = await GetPartsRevenueAsync(fromDate, toDate);

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
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<decimal> GetTotalRevenueAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var allInvoices = await _invoiceRepository.GetAllAsync();

                decimal totalRevenue = 0;

                foreach (var invoice in allInvoices)
                {
                    var completedPayments = await _paymentRepository.GetByInvoiceIdAsync(
                        invoice.InvoiceId,
                        status: BookingStatusConstants.Completed,
                        method: null,
                        from: fromDate,
                        to: toDate);
                    var paidPayments = await _paymentRepository.GetByInvoiceIdAsync(
                        invoice.InvoiceId,
                        status: BookingStatusConstants.Paid,
                        method: null,
                        from: fromDate,
                        to: toDate);

                    totalRevenue += completedPayments.Sum(p => p.Amount) + paidPayments.Sum(p => p.Amount);
                }

                return totalRevenue;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<int> GetTotalEmployeesAsync()
        {
            try
            {
                var staffUsers = await _accountRepository.GetAllUsersWithRoleAsync("STAFF");

                var technicianUsers = await _accountRepository.GetAllUsersWithRoleAsync("TECHNICIAN");

                var managerUsers = await _accountRepository.GetAllUsersWithRoleAsync("MANAGER");

                var activeStaffCount = staffUsers.Count(u => u.IsActive);
                var activeTechnicianCount = technicianUsers.Count(u => u.IsActive);
                var activeManagerCount = managerUsers.Count(u => u.IsActive);

                return activeStaffCount + activeTechnicianCount + activeManagerCount;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<int> GetTotalCompletedBookingsAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var allBookings = await _bookingRepository.GetAllBookingsAsync();

                var completedBookings = allBookings.Count(b =>
                    !string.IsNullOrEmpty(b.Status) &&
                    (b.Status.ToUpperInvariant() == BookingStatusConstants.Completed || b.Status.ToUpperInvariant() == BookingStatusConstants.Paid) &&
                    b.CreatedAt >= fromDate && b.CreatedAt <= toDate);

                return completedBookings;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<decimal> GetServiceRevenueAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var allInvoices = await _invoiceRepository.GetAllAsync();

                decimal serviceRevenue = 0;

                foreach (var invoice in allInvoices)
                {
                    var completedPayments = await _paymentRepository.GetByInvoiceIdAsync(
                        invoice.InvoiceId,
                        status: BookingStatusConstants.Completed,
                        method: null,
                        from: fromDate,
                        to: toDate);
                    var paidPayments = await _paymentRepository.GetByInvoiceIdAsync(
                        invoice.InvoiceId,
                        status: BookingStatusConstants.Paid,
                        method: null,
                        from: fromDate,
                        to: toDate);

                    var totalPaymentAmount = completedPayments.Sum(p => p.Amount) + paidPayments.Sum(p => p.Amount);
                    if (totalPaymentAmount <= 0) continue;

                    var allocatedPartsAmount = Math.Min(invoice.PartsAmount, totalPaymentAmount);
                    var allocatedServiceAmount = totalPaymentAmount - allocatedPartsAmount;

                    if (allocatedServiceAmount > 0)
                    {
                        serviceRevenue += allocatedServiceAmount;
                    }
                }

                return serviceRevenue;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<decimal> GetPartsRevenueAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var allInvoices = await _invoiceRepository.GetAllAsync();

                decimal partsRevenue = 0;

                foreach (var invoice in allInvoices)
                {
                    var completedPayments = await _paymentRepository.GetByInvoiceIdAsync(
                        invoice.InvoiceId,
                        status: BookingStatusConstants.Completed,
                        method: null,
                        from: fromDate,
                        to: toDate);
                    var paidPayments = await _paymentRepository.GetByInvoiceIdAsync(
                        invoice.InvoiceId,
                        status: BookingStatusConstants.Paid,
                        method: null,
                        from: fromDate,
                        to: toDate);

                    var totalPaymentAmount = completedPayments.Sum(p => p.Amount) + paidPayments.Sum(p => p.Amount);
                    if (totalPaymentAmount <= 0) continue;

                    var allocatedPartsAmount = Math.Min(invoice.PartsAmount, totalPaymentAmount);

                    if (allocatedPartsAmount > 0)
                    {
                        partsRevenue += allocatedPartsAmount;
                    }
                }

                return partsRevenue;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}

