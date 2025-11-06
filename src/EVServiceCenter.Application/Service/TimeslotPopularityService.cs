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
    /// <summary>
    /// Service implementation cho Timeslot Popularity - Đánh giá số lượng booking của từng timeslot
    /// </summary>
    public class TimeslotPopularityService : ITimeslotPopularityService
    {
        private readonly ITimeSlotRepository _timeSlotRepository;
        private readonly ITechnicianTimeSlotRepository _technicianTimeSlotRepository;
        private readonly ILogger<TimeslotPopularityService> _logger;

        public TimeslotPopularityService(
            ITimeSlotRepository timeSlotRepository,
            ITechnicianTimeSlotRepository technicianTimeSlotRepository,
            ILogger<TimeslotPopularityService> logger)
        {
            _timeSlotRepository = timeSlotRepository;
            _technicianTimeSlotRepository = technicianTimeSlotRepository;
            _logger = logger;
        }

        /// <summary>
        /// Lấy thống kê số lượng booking của từng timeslot (toàn hệ thống)
        /// </summary>
        public async Task<TimeslotPopularityResponse> GetTimeslotPopularityAsync(TimeslotPopularityRequest? request = null)
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
                    "Bắt đầu tính toán Timeslot Popularity từ {FromDate} đến {ToDate}",
                    fromDate, toDate);

                // Lấy tất cả timeslots
                var allTimeSlots = await _timeSlotRepository.GetAllTimeSlotsAsync();

                if (!allTimeSlots.Any())
                {
                    _logger.LogWarning("Không có timeslot nào trong hệ thống");
                    return new TimeslotPopularityResponse
                    {
                        Success = true,
                        GeneratedAt = DateTime.UtcNow,
                        FromDate = fromDate,
                        ToDate = toDate,
                        Timeslots = new List<TimeslotPopularityData>(),
                        TotalBookings = 0
                    };
                }

                // Tính popularity cho từng timeslot
                var timeslotPopularity = new List<TimeslotPopularityData>();
                int totalBookings = 0;

                foreach (var timeSlot in allTimeSlots)
                {
                    // Đếm số lượng TechnicianTimeSlot có BookingId != null, SlotId = timeSlot.SlotId
                    // và WorkDate trong date range (toàn hệ thống)
                    var bookingCount = await CountBookingsForTimeslotAsync(timeSlot.SlotId, fromDate, toDate);

                    timeslotPopularity.Add(new TimeslotPopularityData
                    {
                        SlotId = timeSlot.SlotId,
                        SlotTime = timeSlot.SlotTime,
                        SlotLabel = timeSlot.SlotLabel,
                        BookingCount = bookingCount,
                        IsActive = timeSlot.IsActive
                    });

                    totalBookings += bookingCount;
                }

                // Sắp xếp theo SlotId tăng dần (thứ tự bình thường)
                timeslotPopularity = timeslotPopularity.OrderBy(t => t.SlotId).ToList();

                _logger.LogInformation(
                    "Timeslot Popularity tính toán thành công: {TimeslotCount} timeslots, Tổng booking: {TotalBookings}",
                    timeslotPopularity.Count, totalBookings);

                return new TimeslotPopularityResponse
                {
                    Success = true,
                    GeneratedAt = DateTime.UtcNow,
                    FromDate = fromDate,
                    ToDate = toDate,
                    Timeslots = timeslotPopularity,
                    TotalBookings = totalBookings
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tính toán Timeslot Popularity");
                throw;
            }
        }

        /// <summary>
        /// Đếm số lượng booking cho một timeslot cụ thể trong date range (toàn hệ thống)
        /// Sử dụng repository method để đếm TechnicianTimeSlot có BookingId != null
        /// </summary>
        private async Task<int> CountBookingsForTimeslotAsync(int slotId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                // Sử dụng repository method để đếm trực tiếp từ database
                var count = await _technicianTimeSlotRepository.CountBookingsBySlotIdAndDateRangeAsync(
                    slotId, 
                    fromDate, 
                    toDate);
                
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đếm booking cho timeslot {SlotId}", slotId);
                // Trả về 0 nếu có lỗi để không làm gián đoạn quá trình tính toán
                return 0;
            }
        }
    }
}

