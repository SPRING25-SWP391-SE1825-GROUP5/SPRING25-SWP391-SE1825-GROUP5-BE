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
    }
}
