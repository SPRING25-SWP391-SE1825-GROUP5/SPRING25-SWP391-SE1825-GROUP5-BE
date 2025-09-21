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
    public class WeeklyTimeSlotService : IWeeklyTimeSlotService
    {
        private readonly IWeeklyScheduleRepository _weeklyScheduleRepository;
        private readonly ICenterRepository _centerRepository;
        private readonly ITechnicianRepository _technicianRepository;

        public WeeklyTimeSlotService(
            IWeeklyScheduleRepository weeklyScheduleRepository,
            ICenterRepository centerRepository,
            ITechnicianRepository technicianRepository)
        {
            _weeklyScheduleRepository = weeklyScheduleRepository;
            _centerRepository = centerRepository;
            _technicianRepository = technicianRepository;
        }

        public async Task<WeeklyTimeSlotSummaryResponse> CreateWeeklyTimeSlotsAsync(CreateWeeklyTimeSlotRequest request)
        {
            try
            {
                // Validate request
                if (!request.IsValid())
                {
                    throw new ArgumentException("Dữ liệu request không hợp lệ");
                }

                // Validate center exists
                var center = await _centerRepository.GetCenterByIdAsync(request.CenterId);
                if (center == null)
                {
                    throw new ArgumentException($"Không tìm thấy center với ID: {request.CenterId}");
                }

                // Validate technician exists if provided
                if (request.TechnicianId.HasValue)
                {
                    var technician = await _technicianRepository.GetTechnicianByIdAsync(request.TechnicianId.Value);
                    if (technician == null)
                    {
                        throw new ArgumentException($"Không tìm thấy technician với ID: {request.TechnicianId}");
                    }
                }

                // Check for existing schedules in the same period
                var existingSchedules = await _weeklyScheduleRepository.GetWeeklySchedulesByDateRangeAsync(
                    request.StartDate, request.EndDate);

                var conflictingSchedules = existingSchedules.Where(s => 
                    s.LocationId == request.CenterId && 
                    (request.TechnicianId == null || s.TechnicianId == request.TechnicianId) &&
                    request.DaysOfWeek.Contains(s.DayOfWeek)).ToList();

                if (conflictingSchedules.Any())
                {
                    throw new ArgumentException("Đã tồn tại lịch trình trong khoảng thời gian này");
                }

                var createdSchedules = new List<WeeklyTimeSlotResponse>();
                var dayNames = new List<string>();

                // Create schedules for each day of week
                foreach (var dayOfWeek in request.DaysOfWeek)
                {
                    var weeklySchedule = new WeeklySchedule
                    {
                        LocationId = request.CenterId,
                        TechnicianId = request.TechnicianId,
                        DayOfWeek = dayOfWeek,
                        IsOpen = true,
                        StartTime = request.StartTime,
                        EndTime = request.EndTime,
                        BreakStart = request.BreakStart,
                        BreakEnd = request.BreakEnd,
                        BufferMinutes = request.BufferMinutes,
                        StepMinutes = request.StepMinutes,
                        EffectiveFrom = request.EffectiveFrom,
                        EffectiveTo = request.EffectiveTo,
                        IsActive = request.IsActive,
                        Notes = request.Notes
                    };

                    var createdSchedule = await _weeklyScheduleRepository.CreateWeeklyScheduleAsync(weeklySchedule);
                    var response = MapToWeeklyTimeSlotResponse(createdSchedule, center, request.TechnicianId);
                    createdSchedules.Add(response);

                    dayNames.Add(GetDayOfWeekName(dayOfWeek));
                }

                return new WeeklyTimeSlotSummaryResponse
                {
                    CenterId = request.CenterId,
                    CenterName = center.CenterName,
                    TechnicianId = request.TechnicianId,
                    TechnicianName = request.TechnicianId.HasValue ? 
                        (await _technicianRepository.GetTechnicianByIdAsync(request.TechnicianId.Value))?.User?.FullName : null,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    TotalSchedulesCreated = createdSchedules.Count,
                    Schedules = createdSchedules,
                    DaysOfWeekNames = dayNames,
                    Status = "Success",
                    Message = $"Đã tạo thành công {createdSchedules.Count} lịch trình cho {center.CenterName}"
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo weekly time slots: {ex.Message}");
            }
        }

        public async Task<List<WeeklyTimeSlotResponse>> GetWeeklyTimeSlotsByLocationAsync(int centerId, DateOnly? startDate = null, DateOnly? endDate = null)
        {
            try
            {
                var schedules = await _weeklyScheduleRepository.GetWeeklySchedulesByLocationAsync(centerId);
                
                if (startDate.HasValue && endDate.HasValue)
                {
                    schedules = schedules.Where(s => 
                        s.EffectiveFrom <= endDate.Value && 
                        (s.EffectiveTo == null || s.EffectiveTo >= startDate.Value)).ToList();
                }

                var center = await _centerRepository.GetCenterByIdAsync(centerId);
                return schedules.Select(s => MapToWeeklyTimeSlotResponse(s, center, s.TechnicianId)).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy weekly time slots theo center: {ex.Message}");
            }
        }

        public async Task<List<WeeklyTimeSlotResponse>> GetWeeklyTimeSlotsByTechnicianAsync(int technicianId, DateOnly? startDate = null, DateOnly? endDate = null)
        {
            try
            {
                var schedules = await _weeklyScheduleRepository.GetWeeklySchedulesByTechnicianAsync(technicianId);
                
                if (startDate.HasValue && endDate.HasValue)
                {
                    schedules = schedules.Where(s => 
                        s.EffectiveFrom <= endDate.Value && 
                        (s.EffectiveTo == null || s.EffectiveTo >= startDate.Value)).ToList();
                }

                var technician = await _technicianRepository.GetTechnicianByIdAsync(technicianId);
                return schedules.Select(s => MapToWeeklyTimeSlotResponse(s, s.Location, technicianId)).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy weekly time slots theo technician: {ex.Message}");
            }
        }

        public async Task<List<WeeklyTimeSlotResponse>> GetActiveWeeklyTimeSlotsAsync()
        {
            try
            {
                var schedules = await _weeklyScheduleRepository.GetActiveWeeklySchedulesAsync();
                return schedules.Select(s => MapToWeeklyTimeSlotResponse(s, s.Location, s.TechnicianId)).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy active weekly time slots: {ex.Message}");
            }
        }

        public async Task<bool> DeleteWeeklyTimeSlotAsync(int weeklyScheduleId)
        {
            try
            {
                return await _weeklyScheduleRepository.DeleteWeeklyScheduleAsync(weeklyScheduleId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa weekly time slot: {ex.Message}");
            }
        }

        public async Task<WeeklyTimeSlotResponse> UpdateWeeklyTimeSlotAsync(int weeklyScheduleId, CreateWeeklyTimeSlotRequest request)
        {
            try
            {
                var existingSchedule = await _weeklyScheduleRepository.GetWeeklyScheduleByIdAsync(weeklyScheduleId);
                if (existingSchedule == null)
                {
                    throw new ArgumentException($"Không tìm thấy weekly schedule với ID: {weeklyScheduleId}");
                }

                // Update properties
                existingSchedule.LocationId = request.CenterId;
                existingSchedule.TechnicianId = request.TechnicianId;
                existingSchedule.StartTime = request.StartTime;
                existingSchedule.EndTime = request.EndTime;
                existingSchedule.BreakStart = request.BreakStart;
                existingSchedule.BreakEnd = request.BreakEnd;
                existingSchedule.BufferMinutes = request.BufferMinutes;
                existingSchedule.StepMinutes = request.StepMinutes;
                existingSchedule.EffectiveFrom = request.EffectiveFrom;
                existingSchedule.EffectiveTo = request.EffectiveTo;
                existingSchedule.IsActive = request.IsActive;
                existingSchedule.Notes = request.Notes;

                var updatedSchedule = await _weeklyScheduleRepository.UpdateWeeklyScheduleAsync(existingSchedule);
                return MapToWeeklyTimeSlotResponse(updatedSchedule, updatedSchedule.Location, updatedSchedule.TechnicianId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật weekly time slot: {ex.Message}");
            }
        }

        private WeeklyTimeSlotResponse MapToWeeklyTimeSlotResponse(WeeklySchedule schedule, ServiceCenter? center, int? technicianId)
        {
            return new WeeklyTimeSlotResponse
            {
                WeeklyScheduleId = schedule.WeeklyScheduleId,
                CenterId = schedule.LocationId,
                CenterName = center?.CenterName,
                TechnicianId = schedule.TechnicianId,
                TechnicianName = schedule.Technician?.User?.FullName,
                DayOfWeek = schedule.DayOfWeek,
                DayOfWeekName = GetDayOfWeekName(schedule.DayOfWeek),
                IsOpen = schedule.IsOpen,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                BreakStart = schedule.BreakStart,
                BreakEnd = schedule.BreakEnd,
                BufferMinutes = schedule.BufferMinutes,
                StepMinutes = schedule.StepMinutes,
                EffectiveFrom = schedule.EffectiveFrom,
                EffectiveTo = schedule.EffectiveTo,
                IsActive = schedule.IsActive,
                Notes = schedule.Notes,
                CreatedAt = DateTime.UtcNow
            };
        }

        private string GetDayOfWeekName(byte dayOfWeek)
        {
            return dayOfWeek switch
            {
                0 => "Chủ Nhật",
                1 => "Thứ Hai",
                2 => "Thứ Ba",
                3 => "Thứ Tư",
                4 => "Thứ Năm",
                5 => "Thứ Sáu",
                6 => "Thứ Bảy",
                _ => "Không xác định"
            };
        }
    }
}
