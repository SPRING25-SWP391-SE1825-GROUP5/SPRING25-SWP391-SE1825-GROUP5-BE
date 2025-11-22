using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service
{
    public class RevenueReportService : IRevenueReportService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IServiceRepository _serviceRepository;

        public RevenueReportService(
            IBookingRepository bookingRepository,
            IInvoiceRepository invoiceRepository,
            IPaymentRepository paymentRepository,
            IServiceRepository serviceRepository)
        {
            _bookingRepository = bookingRepository;
            _invoiceRepository = invoiceRepository;
            _paymentRepository = paymentRepository;
            _serviceRepository = serviceRepository;
        }

        public async Task<RevenueReportResponse> GetRevenueReportAsync(int centerId, RevenueReportRequest request)
        {
            try
            {
                var allCenterBookings = await _bookingRepository.GetBookingsByCenterIdAsync(
                    centerId, page: 1, pageSize: int.MaxValue, status: null);

                var bookingById = allCenterBookings.ToDictionary(b => b.BookingId, b => b);

                var bookingPaymentAmounts = new List<(DateTime paidAt, int bookingId, decimal amount)>();
                foreach (var booking in allCenterBookings)
                {
                    var invoice = await _invoiceRepository.GetByBookingIdAsync(booking.BookingId);
                    if (invoice == null) continue;
                    var payments = await _paymentRepository.GetByInvoiceIdAsync(invoice.InvoiceId, status: "COMPLETED", method: null, from: request.StartDate, to: request.EndDate);
                    foreach (var p in payments)
                    {
                        if (p.PaidAt.HasValue)
                        {
                            bookingPaymentAmounts.Add((p.PaidAt.Value, booking.BookingId, p.Amount));
                        }
                    }
                }

                var statuses = new[] { "PAID" };
                var allPaidPayments = await _paymentRepository.GetPaymentsByStatusesAndDateRangeAsync(
                    statuses, request.StartDate, request.EndDate);
                var paidBookingPayments = allPaidPayments
                    .Where(p => p.Invoice != null 
                             && p.Invoice.BookingId != null 
                             && p.Invoice.Booking != null 
                             && p.Invoice.Booking.CenterId == centerId
                             && p.PaidAt.HasValue)
                    .ToList();
                foreach (var p in paidBookingPayments)
                {
                    if (p.Invoice.BookingId.HasValue && p.PaidAt.HasValue)
                    {
                        bookingPaymentAmounts.Add((p.PaidAt.Value, p.Invoice.BookingId.Value, p.Amount));
                    }
                }

                var orderPayments = await _paymentRepository.GetCompletedPaymentsByFulfillmentCenterAndDateRangeAsync(
                    centerId, request.StartDate, request.EndDate);

                var orderPaymentAmounts = new List<(DateTime paidAt, int orderId, decimal amount)>();
                foreach (var p in orderPayments)
                {
                    if (p.PaidAt.HasValue && p.Invoice?.OrderId != null)
                    {
                        orderPaymentAmounts.Add((p.PaidAt.Value, p.Invoice.OrderId.Value, p.Amount));
                    }
                }

                var allPaymentAmounts = bookingPaymentAmounts
                    .Select(bp => (bp.paidAt, bp.bookingId, bp.amount))
                    .Concat(orderPaymentAmounts.Select(op => (op.paidAt, -1, op.amount)))
                    .ToList();

                var revenueByPeriod = CalculateRevenueByPeriodFromPayments(
                    allPaymentAmounts, 
                    request.Period);

                var summary = CalculateSummaryFromPayments(revenueByPeriod);

                var groupedData = CalculateGroupedDataFromPayments(bookingPaymentAmounts, bookingById, request.GroupBy);

                var alerts = await CalculateAlertsAsync(centerId, revenueByPeriod, request.StartDate, request.EndDate);

                var trends = CalculateTrends(revenueByPeriod);

                RevenueComparison? comparison = null;
                if (request.CompareWithPrevious)
                {
                    comparison = await CalculateComparisonAsync(centerId, request.StartDate, request.EndDate);
                }

                return new RevenueReportResponse
                {
                    Summary = summary,
                    RevenueByPeriod = revenueByPeriod,
                    GroupedData = groupedData,
                    Alerts = alerts,
                    Trends = trends,
                    Comparison = comparison
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        private List<RevenueByPeriod> CalculateRevenueByPeriodFromPayments(List<(DateTime paidAt, int bookingId, decimal amount)> bookingPayments, string period)
        {
            var result = bookingPayments
                .GroupBy(x => GetPeriodKey(x.paidAt, period))
                .Select(g => new RevenueByPeriod
                {
                    Period = g.Key,
                    Revenue = g.Sum(x => x.amount),
                    Bookings = g.Select(x => x.bookingId).Distinct().Count(),
                    Services = g.Sum(x => x.amount),
                    Parts = 0
                })
                .OrderBy(r => r.Period)
                .ToList();
            return result;
        }

        private RevenueSummary CalculateSummaryFromPayments(List<RevenueByPeriod> revenueByPeriod)
        {
            var totalRevenue = revenueByPeriod.Sum(r => r.Revenue);
            var totalBookings = revenueByPeriod.Sum(r => r.Bookings);
            var averageRevenuePerBooking = totalBookings > 0 ? totalRevenue / totalBookings : 0;
            return new RevenueSummary
            {
                TotalRevenue = totalRevenue,
                TotalBookings = totalBookings,
                AverageRevenuePerBooking = averageRevenuePerBooking,
                GrowthRate = "+15%",
                AlertLevel = "normal"
            };
        }

        private string GetPeriodKey(DateTime date, string period)
        {
            return period.ToLower() switch
            {
                "daily" => date.ToString("yyyy-MM-dd"),
                "weekly" => GetWeekKey(date),
                "monthly" => date.ToString("yyyy-MM"),
                "quarterly" => GetQuarterKey(date),
                _ => date.ToString("yyyy-MM-dd")
            };
        }

        private string GetWeekKey(DateTime date)
        {
            var startOfWeek = date.AddDays(-(int)date.DayOfWeek);
            return startOfWeek.ToString("yyyy-MM-dd");
        }

        private string GetQuarterKey(DateTime date)
        {
            var quarter = (date.Month - 1) / 3 + 1;
            return $"{date.Year}-Q{quarter}";
        }

        private string GetYearKey(DateTime date)
        {
            return date.ToString("yyyy");
        }

        public async Task<RevenueByPeriodResponse> GetRevenueByPeriodAsync(int centerId, DateTime? fromDate, DateTime? toDate, string granularity)
        {
            try
            {
                var validGranularities = new[] { "day", "week", "month", "quarter", "year" };
                var normalizedGranularity = granularity?.ToLower() ?? "day";
                if (!validGranularities.Contains(normalizedGranularity))
                {
                    throw new ArgumentException($"Granularity không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validGranularities)}", nameof(granularity));
                }

                var start = (fromDate ?? DateTime.Today.AddDays(-30)).Date;
                var end = (toDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

                if (start > end)
                {
                    throw new ArgumentException("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc", nameof(fromDate));
                }

                // Lấy tất cả payments COMPLETED và PAID trong khoảng thời gian
                // Sau đó filter theo centerId để đảm bảo không bỏ sót payment nào
                var statuses = new[] { "COMPLETED", "PAID" };
                var allPayments = await _paymentRepository.GetPaymentsByStatusesAndDateRangeAsync(statuses, start, end);

                // Filter payments theo centerId: từ Booking hoặc Order
                var payments = allPayments
                    .Where(p => p.PaidAt != null 
                             && p.PaidAt >= start 
                             && p.PaidAt <= end
                             && p.Invoice != null
                             && (
                                 // Payment từ booking
                                 (p.Invoice.BookingId != null 
                                  && p.Invoice.Booking != null 
                                  && p.Invoice.Booking.CenterId == centerId)
                                 ||
                                 // Payment từ order
                                 (p.Invoice.OrderId != null 
                                  && p.Invoice.Order != null 
                                  && p.Invoice.Order.FulfillmentCenterId == centerId)
                             ))
                    .DistinctBy(p => p.PaymentId) // Loại bỏ trùng lặp nếu có
                    .ToList();

                var allPeriods = GenerateAllPeriodsInRange(start, end, normalizedGranularity);
                
                var periodDateRanges = new Dictionary<string, (DateTime startDate, DateTime endDate)>();
                foreach (var period in allPeriods)
                {
                    if (normalizedGranularity == "week" && period.Contains("_to_"))
                    {
                        var parts = period.Split("_to_");
                        if (parts.Length == 2 && DateTime.TryParse(parts[0], out var weekStart) && DateTime.TryParse(parts[1], out var weekEnd))
                        {
                            periodDateRanges[period] = (weekStart.Date, weekEnd.Date.AddDays(1).AddTicks(-1));
                        }
                    }
                }

                var revenueByKey = new Dictionary<string, decimal>();
                decimal total = 0m;

                foreach (var payment in payments)
                {
                    if (payment.PaidAt == null) continue;

                    string periodKey;
                    
                    if (normalizedGranularity == "week")
                    {
                        periodKey = periodDateRanges
                            .FirstOrDefault(kv => payment.PaidAt.Value.Date >= kv.Value.startDate && payment.PaidAt.Value.Date <= kv.Value.endDate)
                            .Key;
                        
                        if (string.IsNullOrEmpty(periodKey))
                        {
                            periodKey = GetPeriodKeyForGranularity(payment.PaidAt.Value, normalizedGranularity);
                        }
                    }
                    else
                    {
                        periodKey = GetPeriodKeyForGranularity(payment.PaidAt.Value, normalizedGranularity);
                    }
                    
                    if (!revenueByKey.ContainsKey(periodKey))
                    {
                        revenueByKey[periodKey] = 0m;
                    }
                    
                    revenueByKey[periodKey] += payment.Amount;
                    total += payment.Amount;
                }
                
                var items = allPeriods
                    .Select(period => new RevenueByPeriodItem
                    {
                        Period = period,
                        Revenue = revenueByKey.ContainsKey(period) ? revenueByKey[period] : 0m
                    })
                    .OrderBy(item => item.Period)
                    .ToList();

                return new RevenueByPeriodResponse
                {
                    Success = true,
                    TotalRevenue = total,
                    Granularity = normalizedGranularity,
                    Items = items
                };
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private string GetPeriodKeyForGranularity(DateTime date, string granularity)
        {
            return granularity switch
            {
                "day" => date.ToString("yyyy-MM-dd"),
                "week" => GetWeekKey(date),
                "month" => date.ToString("yyyy-MM"),
                "quarter" => GetQuarterKey(date),
                "year" => GetYearKey(date),
                _ => date.ToString("yyyy-MM-dd")
            };
        }

        private List<string> GenerateAllPeriodsInRange(DateTime startDate, DateTime endDate, string granularity)
        {
            var periods = new List<string>();
            var currentDate = startDate.Date;

            while (currentDate <= endDate.Date)
            {
                string periodKey;
                DateTime nextDate;

                switch (granularity)
                {
                    case "day":
                        periodKey = currentDate.ToString("yyyy-MM-dd");
                        nextDate = currentDate.AddDays(1);
                        break;

                    case "week":
                        if (currentDate == startDate.Date)
                        {
                            var dayOfWeek = (int)currentDate.DayOfWeek;
                            var endOfWeek = currentDate.AddDays(6 - dayOfWeek);
                            
                            var weekEnd = endOfWeek < endDate.Date ? endOfWeek : endDate.Date;
                            
                            periodKey = $"{currentDate:yyyy-MM-dd}_to_{weekEnd:yyyy-MM-dd}";
                            nextDate = weekEnd.AddDays(1);
                        }
                        else
                        {
                            var dayOfWeek = (int)currentDate.DayOfWeek;
                            var endOfWeek = currentDate.AddDays(6 - dayOfWeek);
                            
                            var weekEnd = endOfWeek < endDate.Date ? endOfWeek : endDate.Date;
                            
                            periodKey = $"{currentDate:yyyy-MM-dd}_to_{weekEnd:yyyy-MM-dd}";
                            nextDate = weekEnd.AddDays(1);
                        }
                        break;

                    case "month":
                        periodKey = currentDate.ToString("yyyy-MM");
                        nextDate = currentDate.AddMonths(1);
                        nextDate = new DateTime(nextDate.Year, nextDate.Month, 1);
                        break;

                    case "quarter":
                        var quarter = (currentDate.Month - 1) / 3 + 1;
                        periodKey = $"{currentDate.Year}-Q{quarter}";
                        var nextQuarter = quarter + 1;
                        if (nextQuarter > 4)
                        {
                            nextDate = new DateTime(currentDate.Year + 1, 1, 1);
                        }
                        else
                        {
                            nextDate = new DateTime(currentDate.Year, (nextQuarter - 1) * 3 + 1, 1);
                        }
                        break;

                    case "year":
                        periodKey = currentDate.ToString("yyyy");
                        nextDate = new DateTime(currentDate.Year + 1, 1, 1);
                        break;

                    default:
                        periodKey = currentDate.ToString("yyyy-MM-dd");
                        nextDate = currentDate.AddDays(1);
                        break;
                }

                if (!periods.Contains(periodKey))
                {
                    periods.Add(periodKey);
                }

                if (nextDate > endDate.Date)
                {
                    break;
                }

                currentDate = nextDate;
            }

            return periods;
        }

        public async Task<RevenueByServiceResponse> GetRevenueByServiceAsync(int centerId, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var start = (fromDate ?? DateTime.Today.AddDays(-30)).Date;
                var end = (toDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

                if (start > end)
                {
                    throw new ArgumentException("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc", nameof(fromDate));
                }

                var allServices = await _serviceRepository.GetAllServicesAsync();

                var payments = await _paymentRepository.GetCompletedPaymentsByCenterAndDateRangeAsync(centerId, start, end);

                var revenueByService = new Dictionary<int, (decimal revenue, HashSet<int> bookingIds)>();

                foreach (var payment in payments)
                {
                    if (payment.PaidAt == null || payment.Invoice?.Booking == null) continue;

                    var booking = payment.Invoice.Booking;
                    var serviceId = booking.ServiceId;

                    if (!revenueByService.ContainsKey(serviceId))
                    {
                        revenueByService[serviceId] = (0m, new HashSet<int>());
                    }

                    var current = revenueByService[serviceId];
                    revenueByService[serviceId] = (
                        current.revenue + payment.Amount,
                        current.bookingIds
                    );
                    current.bookingIds.Add(booking.BookingId);
                }

                var items = allServices
                    .Select(service => new RevenueByServiceItem
                    {
                        ServiceId = service.ServiceId,
                        ServiceName = service.ServiceName ?? $"Service #{service.ServiceId}",
                        BookingCount = revenueByService.ContainsKey(service.ServiceId) 
                            ? revenueByService[service.ServiceId].bookingIds.Count 
                            : 0,
                        Revenue = revenueByService.ContainsKey(service.ServiceId) 
                            ? revenueByService[service.ServiceId].revenue 
                            : 0m
                    })
                    .OrderBy(x => x.ServiceId)
                    .ToList();

                var totalRevenue = items.Sum(x => x.Revenue);

                return new RevenueByServiceResponse
                {
                    Success = true,
                    TotalRevenue = totalRevenue,
                    Items = items
                };
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private RevenueSummary CalculateSummary(List<RevenueByPeriod> revenueByPeriod, List<Booking> bookings)
        {
            var totalRevenue = revenueByPeriod.Sum(r => r.Revenue);
            var totalBookings = revenueByPeriod.Sum(r => r.Bookings);
            var averageRevenuePerBooking = totalBookings > 0 ? totalRevenue / totalBookings : 0;

            return new RevenueSummary
            {
                TotalRevenue = totalRevenue,
                TotalBookings = totalBookings,
                AverageRevenuePerBooking = averageRevenuePerBooking,
                GrowthRate = "+15%",
                AlertLevel = "normal"
            };
        }

        private GroupedRevenueData CalculateGroupedDataFromPayments(List<(DateTime paidAt, int bookingId, decimal amount)> bookingPayments, Dictionary<int, Booking> bookingById, string groupBy)
        {
            var result = new GroupedRevenueData();
            var bookingAmount = bookingPayments
                .GroupBy(x => x.bookingId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.amount));

            if (groupBy == "service")
            {
                var data = bookingAmount
                    .Select(kv => new { bookingId = kv.Key, amount = kv.Value, booking = bookingById.GetValueOrDefault(kv.Key) })
                    .Where(x => x.booking != null)
                    .GroupBy(x => x.booking!.ServiceId)
                    .Select(g => new ServiceRevenue
                    {
                        ServiceId = g.Key,
                        ServiceName = g.First().booking!.Service?.ServiceName ?? "Unknown",
                        Revenue = g.Sum(x => x.amount),
                        Bookings = g.Count(),
                        Percentage = 0
                    })
                    .ToList();
                result.ByService = data;
            }

            if (groupBy == "technician")
            {
                var data = bookingAmount
                    .Select(kv => new { bookingId = kv.Key, amount = kv.Value, booking = bookingById.GetValueOrDefault(kv.Key) })
                    .Where(x => x.booking != null && x.booking.TechnicianSlotId.HasValue)
                    .GroupBy(x => x.booking!.TechnicianTimeSlot?.TechnicianId ?? 0)
                    .Select(g => new TechnicianRevenue
                    {
                        TechnicianId = g.Key,
                        TechnicianName = g.First().booking!.TechnicianTimeSlot?.Technician?.User?.FullName ?? "Unknown",
                        Revenue = g.Sum(x => x.amount),
                        Bookings = g.Count(),
                        AverageRating = 4.5
                    })
                    .ToList();
                result.ByTechnician = data;
            }

            return result;
        }

        private Task<List<RevenueAlert>> CalculateAlertsAsync(int centerId, List<RevenueByPeriod> revenueByPeriod, DateTime startDate, DateTime endDate)
        {
            var alerts = new List<RevenueAlert>();

            return Task.FromResult(alerts);
        }

        private RevenueTrends CalculateTrends(List<RevenueByPeriod> revenueByPeriod)
        {
            if (!revenueByPeriod.Any())
            {
                return new RevenueTrends();
            }

            var revenues = revenueByPeriod.Select(r => r.Revenue).ToList();
            var isIncreasing = revenues.Count > 1 && revenues.Last() > revenues.First();
            var volatility = CalculateVolatility(revenues);

            return new RevenueTrends
            {
                Direction = isIncreasing ? "increasing" : "decreasing",
                Volatility = volatility,
                PeakDay = revenueByPeriod.OrderByDescending(r => r.Revenue).First().Period,
                LowestDay = revenueByPeriod.OrderBy(r => r.Revenue).First().Period
            };
        }

        private string CalculateVolatility(List<decimal> revenues)
        {
            if (revenues.Count < 2) return "low";

            var average = revenues.Average();
            var variance = revenues.Sum(r => Math.Pow((double)(r - average), 2)) / revenues.Count;
            var standardDeviation = Math.Sqrt(variance);

            return standardDeviation switch
            {
                < 100000 => "low",
                < 500000 => "medium",
                _ => "high"
            };
        }

        private Task<RevenueComparison> CalculateComparisonAsync(int centerId, DateTime startDate, DateTime endDate)
        {
            var periodLength = endDate - startDate;
            var previousStartDate = startDate - periodLength;
            var previousEndDate = startDate;

            return Task.FromResult(new RevenueComparison
            {
                PreviousPeriod = $"{previousStartDate:yyyy-MM-dd} to {previousEndDate:yyyy-MM-dd}",
                RevenueChange = 0,
                PercentageChange = "+0%",
                BookingChange = 0
            });
        }
    }
}
