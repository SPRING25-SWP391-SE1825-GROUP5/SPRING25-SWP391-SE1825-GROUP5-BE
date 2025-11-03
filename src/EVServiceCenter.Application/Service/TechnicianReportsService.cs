using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Application.Service
{
    public class TechnicianReportsService : ITechnicianReportsService
    {
        private readonly ITechnicianRepository _technicianRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly ITechnicianTimeSlotRepository _technicianTimeSlotRepository;
        private readonly ILogger<TechnicianReportsService> _logger;

        public TechnicianReportsService(
            ITechnicianRepository technicianRepository,
            IBookingRepository bookingRepository,
            ITechnicianTimeSlotRepository technicianTimeSlotRepository,
            ILogger<TechnicianReportsService> logger)
        {
            _technicianRepository = technicianRepository;
            _bookingRepository = bookingRepository;
            _technicianTimeSlotRepository = technicianTimeSlotRepository;
            _logger = logger;
        }

        public async Task<TechnicianPerformanceResponse> GetTechnicianPerformanceAsync(int centerId, string period = "month")
        {
            try
            {
                var technicians = await _technicianRepository.GetTechniciansByCenterIdAsync(centerId);
                var technicianPerformanceItems = new List<TechnicianPerformanceItem>();

                foreach (var technician in technicians)
                {
                    var performance = await CalculateTechnicianPerformance(technician, period);
                    technicianPerformanceItems.Add(performance);
                }

                var summary = CalculatePerformanceSummary(technicianPerformanceItems);

                return new TechnicianPerformanceResponse
                {
                    Technicians = technicianPerformanceItems,
                    Summary = summary
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy hiệu suất kỹ thuật viên cho center {CenterId}", centerId);
                throw;
            }
        }

        public async Task<TechnicianScheduleResponse> GetTechnicianScheduleAsync(int centerId, DateTime date)
        {
            try
            {
                var technicians = await _technicianRepository.GetTechniciansByCenterIdAsync(centerId);
                var scheduleItems = new List<TechnicianScheduleItem>();

                foreach (var technician in technicians)
                {
                    var schedule = await GetTechnicianScheduleForDate(technician, date);
                    scheduleItems.Add(schedule);
                }

                return new TechnicianScheduleResponse
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    Technicians = scheduleItems
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch làm việc kỹ thuật viên cho center {CenterId} ngày {Date}", centerId, date);
                throw;
            }
        }

        private async Task<TechnicianPerformanceItem> CalculateTechnicianPerformance(Technician technician, string period)
        {
            var startDate = GetPeriodStartDate(period);
            var endDate = DateTime.Now;

            // Lấy bookings của technician trong khoảng thời gian
            var bookings = await _bookingRepository.GetByTechnicianAsync(technician.TechnicianId);
            var periodBookings = bookings.Where(b => b.UpdatedAt >= startDate && b.UpdatedAt <= endDate).ToList();

            var totalBookings = periodBookings.Count;
            var completedBookings = periodBookings.Count(b => b.Status == "COMPLETED");
            var pendingBookings = periodBookings.Count(b => b.Status == "PENDING");
            var totalRevenue = periodBookings.Where(b => b.Status == "PAID").Sum(b => b.Service?.BasePrice ?? 0);
            var averageRevenuePerBooking = totalBookings > 0 ? totalRevenue / totalBookings : 0;

            return new TechnicianPerformanceItem
            {
                TechnicianId = technician.TechnicianId,
                TechnicianName = technician.User?.FullName ?? "Unknown",
                Position = technician.Position ?? "Kỹ thuật viên",
                TotalBookings = totalBookings,
                CompletedBookings = completedBookings,
                PendingBookings = pendingBookings,
                AverageRating = (double)(technician.Rating ?? 0),
                TotalRevenue = totalRevenue,
                AverageRevenuePerBooking = averageRevenuePerBooking,
                AverageProcessingTimeHours = 2.5, // TODO: Calculate actual processing time
                LastActiveDate = periodBookings.Any() ? periodBookings.Max(b => b.UpdatedAt) : DateTime.MinValue,
                IsActive = technician.IsActive
            };
        }

        private async Task<TechnicianScheduleItem> GetTechnicianScheduleForDate(Technician technician, DateTime date)
        {
            // Lấy time slots của technician cho ngày cụ thể
            var timeSlots = await _technicianTimeSlotRepository.GetByTechnicianAndDateAsync(technician.TechnicianId, date);
            
            var slots = timeSlots.Select(ts => new ScheduleSlot
            {
                SlotId = ts.SlotId,
                TimeSlot = ts.Slot?.SlotTime.ToString() ?? "",
                Status = ts.BookingId.HasValue ? "BOOKED" : "AVAILABLE",
                BookingId = ts.BookingId,
                CustomerName = ts.Booking?.Customer?.User?.FullName ?? "",
                ServiceName = ts.Booking?.Service?.ServiceName ?? ""
            }).ToList();

            var totalSlots = slots.Count;
            var bookedSlots = slots.Count(s => s.Status == "BOOKED");
            var availableSlots = totalSlots - bookedSlots;
            var utilizationRate = totalSlots > 0 ? (double)bookedSlots / totalSlots * 100 : 0;

            return new TechnicianScheduleItem
            {
                TechnicianId = technician.TechnicianId,
                TechnicianName = technician.User?.FullName ?? "Unknown",
                Position = technician.Position ?? "Kỹ thuật viên",
                Slots = slots,
                TotalSlots = totalSlots,
                BookedSlots = bookedSlots,
                AvailableSlots = availableSlots,
                UtilizationRate = utilizationRate
            };
        }

        private DateTime GetPeriodStartDate(string period)
        {
            return period.ToLower() switch
            {
                "week" => DateTime.Now.AddDays(-7),
                "month" => DateTime.Now.AddMonths(-1),
                "quarter" => DateTime.Now.AddMonths(-3),
                "year" => DateTime.Now.AddYears(-1),
                _ => DateTime.Now.AddMonths(-1)
            };
        }

        private TechnicianPerformanceSummary CalculatePerformanceSummary(List<TechnicianPerformanceItem> technicians)
        {
            return new TechnicianPerformanceSummary
            {
                TotalTechnicians = technicians.Count,
                ActiveTechnicians = technicians.Count(t => t.IsActive),
                TotalBookings = technicians.Sum(t => t.TotalBookings),
                TotalRevenue = technicians.Sum(t => t.TotalRevenue),
                AverageRating = technicians.Any() ? technicians.Average(t => t.AverageRating) : 0,
                AverageProcessingTimeHours = technicians.Any() ? technicians.Average(t => t.AverageProcessingTimeHours) : 0
            };
        }

        /// <summary>
        /// Lấy thống kê số lượng booking của center và mỗi technician thực hiện trong khoảng thời gian
        /// Chỉ tính booking có trạng thái PAID hoặc COMPLETED
        /// </summary>
        public async Task<TechnicianBookingStatsResponse> GetTechnicianBookingStatsAsync(int centerId, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                // Tính toán khoảng thời gian (mặc định 30 ngày gần nhất)
                var start = (fromDate ?? DateTime.Today.AddDays(-30)).Date;
                var end = (toDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

                // Validate date range
                if (start > end)
                {
                    throw new ArgumentException("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc", nameof(fromDate));
                }

                // Lấy tất cả bookings của center trong khoảng thời gian
                var bookings = await _bookingRepository.GetBookingsByCenterIdAsync(
                    centerId, 
                    page: 1, 
                    pageSize: int.MaxValue, 
                    status: null, 
                    fromDate: start, 
                    toDate: end);

                // Lọc chỉ lấy booking có trạng thái PAID hoặc COMPLETED và có technician được gán
                var completedBookings = bookings
                    .Where(b => 
                        ((b.Status ?? string.Empty).ToUpperInvariant() == "PAID" || 
                         (b.Status ?? string.Empty).ToUpperInvariant() == "COMPLETED") &&
                        b.TechnicianTimeSlot != null)
                    .ToList();

                // Tổng số booking của center (chỉ tính booking đã gán technician)
                var totalBookings = completedBookings.Count;

                // Lấy tất cả technicians của center
                var technicians = await _technicianRepository.GetTechniciansByCenterIdAsync(centerId);

                // Nhóm bookings theo technician (thông qua TechnicianTimeSlot)
                var bookingCountByTechnician = new Dictionary<int, int>();

                foreach (var booking in completedBookings)
                {
                    // Lấy technicianId từ TechnicianTimeSlot (đã filter ở trên nên không null)
                    var technicianId = booking.TechnicianTimeSlot!.TechnicianId;
                    
                    if (!bookingCountByTechnician.ContainsKey(technicianId))
                    {
                        bookingCountByTechnician[technicianId] = 0;
                    }
                    
                    bookingCountByTechnician[technicianId]++;
                }

                // Tạo danh sách technicians với số booking đã thực hiện
                // Bao gồm cả technicians không có booking (bookingCount = 0)
                var technicianItems = technicians
                    .Select(technician => new TechnicianBookingStatsItem
                    {
                        TechnicianId = technician.TechnicianId,
                        TechnicianName = technician.User?.FullName ?? "Unknown",
                        BookingCount = bookingCountByTechnician.ContainsKey(technician.TechnicianId) 
                            ? bookingCountByTechnician[technician.TechnicianId] 
                            : 0
                    })
                    .OrderByDescending(x => x.BookingCount)
                    .ThenBy(x => x.TechnicianName)
                    .ToList();

                return new TechnicianBookingStatsResponse
                {
                    Success = true,
                    TotalBookings = totalBookings,
                    Technicians = technicianItems
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Lỗi validation khi lấy thống kê booking của technician cho center {CenterId}", centerId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thống kê booking của technician cho center {CenterId}", centerId);
                throw;
            }
        }

        /// <summary>
        /// Lấy tỉ lệ lấp đầy (utilization rate) của center theo khoảng thời gian
        /// Hỗ trợ groupBy theo day/week/month/quarter/year khi chọn khoảng thời gian dài
        /// </summary>
        public async Task<UtilizationRateResponse> GetCenterUtilizationRateAsync(int centerId, DateTime? fromDate, DateTime? toDate, string? granularity = null)
        {
            try
            {
                // Tính toán khoảng thời gian (mặc định 30 ngày gần nhất)
                var start = (fromDate ?? DateTime.Today.AddDays(-30)).Date;
                var end = (toDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

                // Validate date range
                if (start > end)
                {
                    throw new ArgumentException("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc", nameof(fromDate));
                }

                // Tự động chọn granularity dựa trên khoảng thời gian nếu không được chỉ định
                var periodLength = (end - start).TotalDays;
                var selectedGranularity = granularity ?? AutoSelectGranularity(periodLength);
                
                // Validate granularity
                var validGranularities = new[] { "day", "week", "month", "quarter", "year" };
                if (!validGranularities.Contains(selectedGranularity.ToLower()))
                {
                    throw new ArgumentException($"Granularity không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validGranularities)}", nameof(granularity));
                }

                // Lấy tất cả TechnicianTimeSlot của center trong khoảng thời gian
                var slots = await _technicianTimeSlotRepository.GetByCenterAndDateRangeAsync(centerId, start, end);

                // Danh sách status hợp lệ để tính booked slot
                var validBookingStatuses = new[] { "IN_PROGRESS", "COMPLETED", "PAID" };

                // Tính tổng số slot và số slot đã được book
                var totalSlots = slots.Count;
                var bookedSlots = slots.Count(s => 
                    s.BookingId != null && 
                    s.Booking != null &&
                    validBookingStatuses.Contains((s.Booking.Status ?? "").ToUpperInvariant()));

                // Tính utilization rate tổng
                var overallUtilizationRate = totalSlots > 0 ? (decimal)bookedSlots / totalSlots : 0m;

                // Tạo danh sách tất cả các period trong khoảng thời gian
                var allPeriods = GenerateAllPeriodsInRangeForUtilization(start, end, selectedGranularity);

                // Nhóm slots theo period dựa trên WorkDate
                var utilizationByPeriod = new Dictionary<string, (int totalSlots, int bookedSlots)>();

                foreach (var slot in slots)
                {
                    var periodKey = GetPeriodKeyForGranularity(slot.WorkDate, selectedGranularity);
                    
                    if (!utilizationByPeriod.ContainsKey(periodKey))
                    {
                        utilizationByPeriod[periodKey] = (0, 0);
                    }

                    var current = utilizationByPeriod[periodKey];
                    
                    // Tăng totalSlots
                    var newTotalSlots = current.totalSlots + 1;
                    var newBookedSlots = current.bookedSlots;

                    // Kiểm tra nếu slot đã được book với status hợp lệ
                    if (slot.BookingId != null && 
                        slot.Booking != null &&
                        validBookingStatuses.Contains((slot.Booking.Status ?? "").ToUpperInvariant()))
                    {
                        newBookedSlots = current.bookedSlots + 1;
                    }

                    utilizationByPeriod[periodKey] = (newTotalSlots, newBookedSlots);
                }

                // Tạo items với tất cả các period, nếu không có slot thì = 0
                var items = allPeriods
                    .Select(period => 
                    {
                        var (periodTotalSlots, periodBookedSlots) = utilizationByPeriod.ContainsKey(period) 
                            ? utilizationByPeriod[period] 
                            : (0, 0);
                        
                        var periodUtilizationRate = periodTotalSlots > 0 
                            ? (decimal)periodBookedSlots / periodTotalSlots 
                            : 0m;

                        return new UtilizationRateByPeriodItem
                        {
                            Period = period,
                            TotalSlots = periodTotalSlots,
                            BookedSlots = periodBookedSlots,
                            UtilizationRate = periodUtilizationRate
                        };
                    })
                    .OrderBy(item => item.Period)
                    .ToList();

                // Tính utilization rate trung bình từ các period (weighted average hoặc simple average)
                // Sử dụng simple average của các period có data
                var periodsWithData = items.Where(i => i.TotalSlots > 0).ToList();
                var averageUtilizationRate = periodsWithData.Any() 
                    ? periodsWithData.Average(i => i.UtilizationRate) 
                    : overallUtilizationRate;

                return new UtilizationRateResponse
                {
                    Success = true,
                    AverageUtilizationRate = averageUtilizationRate,
                    TotalSlots = totalSlots,
                    BookedSlots = bookedSlots,
                    Granularity = selectedGranularity,
                    Items = items
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Lỗi validation khi lấy tỉ lệ lấp đầy cho center {CenterId}", centerId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tỉ lệ lấp đầy cho center {CenterId}", centerId);
                throw;
            }
        }

        /// <summary>
        /// Tự động chọn granularity dựa trên khoảng thời gian
        /// </summary>
        private string AutoSelectGranularity(double periodLengthDays)
        {
            if (periodLengthDays <= 7) return "day";
            if (periodLengthDays <= 30) return "week";
            if (periodLengthDays <= 90) return "month";
            if (periodLengthDays <= 365) return "quarter";
            return "year";
        }

        /// <summary>
        /// Tạo danh sách tất cả các period trong khoảng thời gian cho utilization rate
        /// </summary>
        private List<string> GenerateAllPeriodsInRangeForUtilization(DateTime startDate, DateTime endDate, string granularity)
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
                        var startOfWeek = currentDate.AddDays(-(int)currentDate.DayOfWeek);
                        periodKey = $"{startOfWeek:yyyy-MM-dd}_to_{startOfWeek.AddDays(6):yyyy-MM-dd}";
                        nextDate = startOfWeek.AddDays(7);
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

        /// <summary>
        /// Lấy period key theo granularity cho utilization rate
        /// </summary>
        private string GetPeriodKeyForGranularity(DateTime date, string granularity)
        {
            return granularity switch
            {
                "day" => date.ToString("yyyy-MM-dd"),
                "week" => GetWeekKeyForUtilization(date),
                "month" => date.ToString("yyyy-MM"),
                "quarter" => GetQuarterKeyForUtilization(date),
                "year" => date.ToString("yyyy"),
                _ => date.ToString("yyyy-MM-dd")
            };
        }

        private string GetWeekKeyForUtilization(DateTime date)
        {
            var startOfWeek = date.AddDays(-(int)date.DayOfWeek);
            return $"{startOfWeek:yyyy-MM-dd}_to_{startOfWeek.AddDays(6):yyyy-MM-dd}";
        }

        private string GetQuarterKeyForUtilization(DateTime date)
        {
            var quarter = (date.Month - 1) / 3 + 1;
            return $"{date.Year}-Q{quarter}";
        }
    }
}
