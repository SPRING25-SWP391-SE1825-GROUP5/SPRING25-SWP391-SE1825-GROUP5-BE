using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Application.Service
{
    public class RevenueReportService : IRevenueReportService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IWorkOrderPartRepository _workOrderPartRepository;
        private readonly ILogger<RevenueReportService> _logger;

        public RevenueReportService(
            IBookingRepository bookingRepository,
            IWorkOrderPartRepository workOrderPartRepository,
            ILogger<RevenueReportService> logger)
        {
            _bookingRepository = bookingRepository;
            _workOrderPartRepository = workOrderPartRepository;
            _logger = logger;
        }

        public async Task<RevenueReportResponse> GetRevenueReportAsync(int centerId, RevenueReportRequest request)
        {
            try
            {
                // Lấy dữ liệu booking đã thanh toán trong khoảng thời gian
                var paidBookings = await GetPaidBookingsAsync(centerId, request.StartDate, request.EndDate);
                
                // Lấy dữ liệu phụ tùng đã sử dụng
                var workOrderParts = await _workOrderPartRepository.GetByCenterAndDateRangeAsync(
                    centerId, request.StartDate, request.EndDate);

                // Tính toán doanh thu theo từng khoảng thời gian
                var revenueByPeriod = CalculateRevenueByPeriod(paidBookings, workOrderParts, request.Period);

                // Tính summary
                var summary = CalculateSummary(revenueByPeriod, paidBookings);

                // Phân nhóm dữ liệu
                var groupedData = CalculateGroupedData(paidBookings, workOrderParts, request.GroupBy);

                // Tính alerts
                var alerts = await CalculateAlertsAsync(centerId, revenueByPeriod, request.StartDate, request.EndDate);

                // Tính trends
                var trends = CalculateTrends(revenueByPeriod);

                // So sánh với kỳ trước nếu được yêu cầu
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo báo cáo doanh thu cho center {CenterId}", centerId);
                throw;
            }
        }

        private async Task<List<Booking>> GetPaidBookingsAsync(int centerId, DateTime startDate, DateTime endDate)
        {
            // Lấy tất cả bookings đã thanh toán của center trong khoảng thời gian
            var allBookings = await _bookingRepository.GetBookingsByCenterIdAsync(
                centerId, 
                page: 1, 
                pageSize: int.MaxValue, 
                status: "PAID");

            // Filter theo UpdatedAt vì repository filter theo CreatedAt
            var filteredBookings = allBookings.Where(b => b.UpdatedAt >= startDate && b.UpdatedAt <= endDate).ToList();
            
            // Debug logging
            _logger.LogInformation($"Found {allBookings.Count} paid bookings for center {centerId}");
            _logger.LogInformation($"After date filter: {filteredBookings.Count} bookings");
            
            foreach (var booking in filteredBookings)
            {
                _logger.LogInformation($"Booking {booking.BookingId}: Service={booking.Service?.ServiceName}, BasePrice={booking.Service?.BasePrice}, UpdatedAt={booking.UpdatedAt}");
            }
            
            return filteredBookings;
        }

        private List<RevenueByPeriod> CalculateRevenueByPeriod(List<Booking> bookings, List<WorkOrderPart> workOrderParts, string period)
        {
            var result = new List<RevenueByPeriod>();
            
            // Group by period
            var groupedBookings = bookings.GroupBy(b => GetPeriodKey(b.UpdatedAt, period));
            var groupedParts = workOrderParts.GroupBy(wop => GetPeriodKey(wop.Booking.UpdatedAt, period));

            foreach (var group in groupedBookings)
            {
                var periodKey = group.Key;
                var periodBookings = group.ToList();
                var periodParts = groupedParts.FirstOrDefault(g => g.Key == periodKey)?.ToList() ?? new List<WorkOrderPart>();

                var revenue = periodBookings.Sum(b => b.Service?.BasePrice ?? 0);
                var partsRevenue = periodParts.Sum(wop => wop.QuantityUsed * wop.Part.Price);

                result.Add(new RevenueByPeriod
                {
                    Period = periodKey,
                    Revenue = revenue + partsRevenue,
                    Bookings = periodBookings.Count,
                    Services = revenue,
                    Parts = partsRevenue
                });
            }

            return result.OrderBy(r => r.Period).ToList();
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
                GrowthRate = "+15%", // TODO: Calculate actual growth rate
                AlertLevel = "normal"
            };
        }

        private GroupedRevenueData CalculateGroupedData(List<Booking> bookings, List<WorkOrderPart> workOrderParts, string groupBy)
        {
            var result = new GroupedRevenueData();

            if (groupBy == "service")
            {
                var serviceGroups = bookings.GroupBy(b => b.ServiceId);
                result.ByService = serviceGroups.Select(g => new ServiceRevenue
                {
                    ServiceId = g.Key,
                    ServiceName = g.First().Service?.ServiceName ?? "Unknown",
                    Revenue = g.Sum(b => b.Service?.BasePrice ?? 0),
                    Bookings = g.Count(),
                    Percentage = 0 // TODO: Calculate percentage
                }).ToList();
            }

            if (groupBy == "technician")
            {
                var technicianGroups = bookings.Where(b => b.TechnicianSlotId.HasValue).GroupBy(b => b.TechnicianSlotId!.Value);
                result.ByTechnician = technicianGroups.Select(g => new TechnicianRevenue
                {
                    TechnicianId = g.First().TechnicianTimeSlot?.TechnicianId ?? 0,
                    TechnicianName = g.First().TechnicianTimeSlot?.Technician?.User?.FullName ?? "Unknown",
                    Revenue = g.Sum(b => b.Service?.BasePrice ?? 0),
                    Bookings = g.Count(),
                    AverageRating = 4.5 // TODO: Calculate actual rating
                }).ToList();
            }

            return result;
        }

        private Task<List<RevenueAlert>> CalculateAlertsAsync(int centerId, List<RevenueByPeriod> revenueByPeriod, DateTime startDate, DateTime endDate)
        {
            var alerts = new List<RevenueAlert>();

            // TODO: Implement alert logic
            // Check for revenue drops, no bookings, etc.

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

            // TODO: Get previous period data and calculate comparison
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
