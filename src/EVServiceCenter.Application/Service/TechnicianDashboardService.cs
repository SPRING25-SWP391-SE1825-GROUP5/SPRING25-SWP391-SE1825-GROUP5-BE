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
    public class TechnicianDashboardService : ITechnicianDashboardService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ITechnicianRepository _technicianRepository;
        private readonly ITechnicianTimeSlotRepository _technicianTimeSlotRepository;
        private readonly ILogger<TechnicianDashboardService> _logger;

        public TechnicianDashboardService(
            IBookingRepository bookingRepository,
            ITechnicianRepository technicianRepository,
            ITechnicianTimeSlotRepository technicianTimeSlotRepository,
            ILogger<TechnicianDashboardService> logger)
        {
            _bookingRepository = bookingRepository;
            _technicianRepository = technicianRepository;
            _technicianTimeSlotRepository = technicianTimeSlotRepository;
            _logger = logger;
        }

        public async Task<TechnicianDashboardResponse> GetDashboardAsync(int technicianId)
        {
            try
            {
                var stats = await GetStatsAsync(technicianId);
                var today = DateTime.Today;
                
                // Lấy upcoming bookings (booking hôm nay và ngày mai)
                var allBookings = await _bookingRepository.GetByTechnicianAsync(technicianId);
                var upcomingBookings = allBookings.Where(b => 
                    b.CreatedAt.Date >= today && 
                    (b.Status == "CONFIRMED" || b.Status == "PENDING")
                ).Take(5).ToList();

                // Lấy lịch hôm nay
                var todaySchedule = await GetTodayScheduleAsync(technicianId);
                
                // Lấy performance summary
                var performance = await GetPerformanceAsync(technicianId);

                return new TechnicianDashboardResponse
                {
                    Stats = stats,
                    UpcomingBookings = upcomingBookings.Select(MapToUpcomingBooking).ToList(),
                    TodaySchedule = todaySchedule,
                    Performance = performance
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dashboard cho technician {TechnicianId}", technicianId);
                throw;
            }
        }

        public async Task<TechnicianBookingListResponse> GetTodayBookingsAsync(int technicianId)
        {
            try
            {
                var today = DateTime.Today;
                var allBookings = await _bookingRepository.GetByTechnicianAsync(technicianId);
                var todayBookings = allBookings.Where(b => b.CreatedAt.Date == today).ToList();

                return new TechnicianBookingListResponse
                {
                    Bookings = todayBookings.Select(MapToBookingItem).ToList(),
                    TotalCount = todayBookings.Count,
                    PageNumber = 1,
                    PageSize = todayBookings.Count,
                    TotalPages = 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy booking hôm nay cho technician {TechnicianId}", technicianId);
                throw;
            }
        }

        public async Task<TechnicianBookingListResponse> GetPendingBookingsAsync(int technicianId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var allBookings = await _bookingRepository.GetByTechnicianAsync(technicianId);
                var pendingBookings = allBookings.Where(b => b.Status == "PENDING").ToList();
                
                var totalCount = pendingBookings.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var pagedBookings = pendingBookings.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                return new TechnicianBookingListResponse
                {
                    Bookings = pagedBookings.Select(MapToBookingItem).ToList(),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy pending bookings cho technician {TechnicianId}", technicianId);
                throw;
            }
        }

        public async Task<TechnicianBookingListResponse> GetInProgressBookingsAsync(int technicianId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var allBookings = await _bookingRepository.GetByTechnicianAsync(technicianId);
                var inProgressBookings = allBookings.Where(b => b.Status == "IN_PROGRESS").ToList();
                
                var totalCount = inProgressBookings.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var pagedBookings = inProgressBookings.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                return new TechnicianBookingListResponse
                {
                    Bookings = pagedBookings.Select(MapToBookingItem).ToList(),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy in-progress bookings cho technician {TechnicianId}", technicianId);
                throw;
            }
        }

        public async Task<TechnicianBookingListResponse> GetCompletedBookingsAsync(int technicianId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var allBookings = await _bookingRepository.GetByTechnicianAsync(technicianId);
                var completedBookings = allBookings.Where(b => b.Status == "COMPLETED" || b.Status == "PAID").ToList();
                
                var totalCount = completedBookings.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var pagedBookings = completedBookings.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                return new TechnicianBookingListResponse
                {
                    Bookings = pagedBookings.Select(MapToBookingItem).ToList(),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy completed bookings cho technician {TechnicianId}", technicianId);
                throw;
            }
        }

        public async Task<TechnicianStats> GetStatsAsync(int technicianId)
        {
            try
            {
                var allBookings = await _bookingRepository.GetByTechnicianAsync(technicianId);
                var today = DateTime.Today;
                var todayBookings = allBookings.Where(b => b.CreatedAt.Date == today).ToList();
                var thisMonthBookings = allBookings.Where(b => b.CreatedAt.Month == DateTime.Now.Month).ToList();

                return new TechnicianStats
                {
                    BookingsToday = todayBookings.Count,
                    PendingTasks = todayBookings.Count(b => b.Status == "PENDING"),
                    CompletedToday = todayBookings.Count(b => b.Status == "COMPLETED" || b.Status == "PAID"),
                    AverageRating = 4.5, // TODO: Get from actual rating
                    MonthlyRevenue = thisMonthBookings.Where(b => b.Status == "PAID").Sum(b => b.Service?.BasePrice ?? 0),
                    ActiveHours = 8, // TODO: Calculate actual hours
                    TotalBookings = allBookings.Count,
                    PendingBookings = allBookings.Count(b => b.Status == "PENDING"),
                    InProgressBookings = allBookings.Count(b => b.Status == "IN_PROGRESS")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy stats cho technician {TechnicianId}", technicianId);
                throw;
            }
        }

        public async Task<PerformanceSummary> GetPerformanceAsync(int technicianId)
        {
            try
            {
                var allBookings = await _bookingRepository.GetByTechnicianAsync(technicianId);
                var today = DateTime.Now;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                var startOfMonth = new DateTime(today.Year, today.Month, 1);

                var thisWeekBookings = allBookings.Where(b => 
                    b.CreatedAt >= startOfWeek && 
                    (b.Status == "COMPLETED" || b.Status == "PAID")).ToList();

                var thisMonthBookings = allBookings.Where(b => 
                    b.CreatedAt >= startOfMonth && 
                    (b.Status == "COMPLETED" || b.Status == "PAID")).ToList();

                return new PerformanceSummary
                {
                    ThisWeek = new PeriodPerformance
                    {
                        BookingsCompleted = thisWeekBookings.Count,
                        AverageRating = 4.8,
                        RevenueGenerated = thisWeekBookings.Sum(b => b.Service?.BasePrice ?? 0),
                        TotalHoursWorked = thisWeekBookings.Count * 2 // TODO: Calculate actual hours
                    },
                    ThisMonth = new PeriodPerformance
                    {
                        BookingsCompleted = thisMonthBookings.Count,
                        AverageRating = 4.7,
                        RevenueGenerated = thisMonthBookings.Sum(b => b.Service?.BasePrice ?? 0),
                        TotalHoursWorked = thisMonthBookings.Count * 2 // TODO: Calculate actual hours
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy performance cho technician {TechnicianId}", technicianId);
                throw;
            }
        }

        private async Task<List<TodayScheduleItem>> GetTodayScheduleAsync(int technicianId)
        {
            try
            {
                var today = DateTime.Today;
                var slots = await _technicianTimeSlotRepository.GetByTechnicianAndDateAsync(technicianId, today);
                
                return slots.Select(s => new TodayScheduleItem
                {
                    TimeSlot = s.Slot?.SlotTime.ToString() ?? "",
                    Type = s.BookingId.HasValue ? "BOOKED" : "AVAILABLE",
                    CustomerName = s.Booking?.Customer?.User?.FullName,
                    BookingId = s.BookingId
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch hôm nay cho technician {TechnicianId}", technicianId);
                return new List<TodayScheduleItem>();
            }
        }

        private UpcomingBooking MapToUpcomingBooking(Booking booking)
        {
            return new UpcomingBooking
            {
                BookingId = booking.BookingId,
                CustomerName = booking.Customer?.User?.FullName ?? "Unknown",
                VehicleInfo = $"{booking.Vehicle?.VehicleModel?.ModelName} - {booking.Vehicle?.LicensePlate}",
                ServiceName = booking.Service?.ServiceName ?? "Unknown",
                TimeSlot = booking.TechnicianTimeSlot?.Slot?.SlotTime.ToString() ?? "",
                Status = booking.Status ?? "UNKNOWN",
                BookingDate = booking.CreatedAt,
                CustomerPhone = booking.Customer?.User?.PhoneNumber ?? ""
            };
        }

        private TechnicianBookingItem MapToBookingItem(Booking booking)
        {
            return new TechnicianBookingItem
            {
                BookingId = booking.BookingId,
                Status = booking.Status ?? "UNKNOWN",
                Date = booking.CreatedAt.ToString("yyyy-MM-dd"),
                ServiceId = booking.ServiceId,
                ServiceName = booking.Service?.ServiceName ?? "Unknown",
                CenterId = booking.CenterId,
                CenterName = booking.Center?.CenterName ?? "Unknown",
                SlotId = booking.TechnicianTimeSlot?.SlotId ?? 0,
                TechnicianSlotId = booking.TechnicianSlotId ?? 0,
                SlotTime = booking.TechnicianTimeSlot?.Slot?.SlotTime.ToString() ?? "",
                SlotLabel = booking.TechnicianTimeSlot?.Slot?.SlotTime.ToString() ?? "",
                CustomerName = booking.Customer?.User?.FullName ?? "Unknown",
                CustomerPhone = booking.Customer?.User?.PhoneNumber ?? "",
                VehiclePlate = booking.Vehicle?.LicensePlate ?? "",
                WorkStartTime = null,
                WorkEndTime = null,
                
                // Dashboard fields
                BookingDate = booking.CreatedAt.Date,
                VehicleInfo = $"{booking.Vehicle?.VehicleModel?.ModelName} - {booking.Vehicle?.LicensePlate}",
                TimeSlot = booking.TechnicianTimeSlot?.Slot?.SlotTime.ToString() ?? "",
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt,
                CustomerAddress = booking.Customer?.User?.Address ?? ""
            };
        }
    }
}
