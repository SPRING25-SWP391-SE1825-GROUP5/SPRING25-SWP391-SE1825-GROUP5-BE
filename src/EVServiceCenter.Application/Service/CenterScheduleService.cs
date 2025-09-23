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
    public class CenterScheduleService : ICenterScheduleService
    {
        private readonly ICenterScheduleRepository _centerScheduleRepository;
        private readonly ICenterRepository _centerRepository;

        public CenterScheduleService(
            ICenterScheduleRepository centerScheduleRepository,
            ICenterRepository centerRepository)
        {
            _centerScheduleRepository = centerScheduleRepository;
            _centerRepository = centerRepository;
        }

        public async Task<CenterScheduleResponse> CreateCenterScheduleAsync(CreateCenterScheduleRequest request)
        {
            try
            {
                // Validate center exists
                var center = await _centerRepository.GetCenterByIdAsync(request.CenterId);
                if (center == null)
                {
                    throw new ArgumentException($"Không tìm thấy center với ID: {request.CenterId}");
                }

                // Validate time range
                if (request.StartTime >= request.EndTime)
                {
                    throw new ArgumentException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc");
                }

                // Validate capacity
                if (request.CapacityLeft > request.CapacityTotal)
                {
                    throw new ArgumentException("Capacity còn lại không được lớn hơn tổng capacity");
                }

                // Check for overlapping schedules
                var existingSchedules = await _centerScheduleRepository.GetCenterSchedulesByCenterAndDayAsync(
                    request.CenterId, request.DayOfWeek);

                var overlappingSchedule = existingSchedules.FirstOrDefault(s =>
                    s.IsActive &&
                    ((s.StartTime <= request.StartTime && s.EndTime > request.StartTime) ||
                     (s.StartTime < request.EndTime && s.EndTime >= request.EndTime) ||
                     (s.StartTime >= request.StartTime && s.EndTime <= request.EndTime)));

                if (overlappingSchedule != null)
                {
                    throw new ArgumentException($"Đã tồn tại lịch trình trùng thời gian: {overlappingSchedule.StartTime} - {overlappingSchedule.EndTime}");
                }

                var centerSchedule = new CenterSchedule
                {
                    CenterId = request.CenterId,
                    DayOfWeek = request.DayOfWeek,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    EffectiveFrom = request.EffectiveFrom,
                    EffectiveTo = request.EffectiveTo,
                    CapacityTotal = request.CapacityTotal,
                    CapacityLeft = request.CapacityLeft,
                    IsActive = request.IsActive
                };

                var createdSchedule = await _centerScheduleRepository.CreateCenterScheduleAsync(centerSchedule);
                return MapToCenterScheduleResponse(createdSchedule, center);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo center schedule: {ex.Message}");
            }
        }

        public async Task<List<CenterScheduleResponse>> GetCenterSchedulesByCenterAsync(int centerId, byte? dayOfWeek = null)
        {
            try
            {
                var center = await _centerRepository.GetCenterByIdAsync(centerId);
                if (center == null)
                {
                    throw new ArgumentException($"Không tìm thấy center với ID: {centerId}");
                }

                List<CenterSchedule> schedules;
                if (dayOfWeek.HasValue)
                {
                    schedules = await _centerScheduleRepository.GetCenterSchedulesByCenterAndDayAsync(centerId, dayOfWeek.Value);
                }
                else
                {
                    schedules = await _centerScheduleRepository.GetCenterSchedulesByCenterAsync(centerId);
                }

                return schedules.Select(s => MapToCenterScheduleResponse(s, center)).ToList();
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy center schedules: {ex.Message}");
            }
        }

        public async Task<List<CenterScheduleResponse>> GetActiveCenterSchedulesAsync()
        {
            try
            {
                var schedules = await _centerScheduleRepository.GetActiveCenterSchedulesAsync();
                return schedules.Select(s => MapToCenterScheduleResponse(s, s.Center)).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy active center schedules: {ex.Message}");
            }
        }

        public async Task<CenterScheduleResponse> GetCenterScheduleByIdAsync(int centerScheduleId)
        {
            try
            {
                var schedule = await _centerScheduleRepository.GetCenterScheduleByIdAsync(centerScheduleId);
                if (schedule == null)
                {
                    throw new ArgumentException($"Không tìm thấy center schedule với ID: {centerScheduleId}");
                }

                return MapToCenterScheduleResponse(schedule, schedule.Center);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy center schedule: {ex.Message}");
            }
        }

        public async Task<List<CenterScheduleResponse>> GetAvailableSchedulesAsync(int centerId, byte dayOfWeek, TimeOnly startTime, TimeOnly endTime)
        {
            try
            {
                var schedules = await _centerScheduleRepository.GetAvailableSchedulesAsync(centerId, dayOfWeek, startTime, endTime);
                return schedules.Select(s => MapToCenterScheduleResponse(s, s.Center)).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy available schedules: {ex.Message}");
            }
        }

        public async Task<CenterScheduleResponse> UpdateCenterScheduleAsync(int centerScheduleId, UpdateCenterScheduleRequest request)
        {
            try
            {
                var existingSchedule = await _centerScheduleRepository.GetCenterScheduleByIdAsync(centerScheduleId);
                if (existingSchedule == null)
                {
                    throw new ArgumentException($"Không tìm thấy center schedule với ID: {centerScheduleId}");
                }

                // Validate center exists
                var center = await _centerRepository.GetCenterByIdAsync(request.CenterId);
                if (center == null)
                {
                    throw new ArgumentException($"Không tìm thấy center với ID: {request.CenterId}");
                }

                // Validate time range
                if (request.StartTime >= request.EndTime)
                {
                    throw new ArgumentException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc");
                }

                // Validate capacity
                if (request.CapacityLeft > request.CapacityTotal)
                {
                    throw new ArgumentException("Capacity còn lại không được lớn hơn tổng capacity");
                }

                // Update properties
                existingSchedule.CenterId = request.CenterId;
                existingSchedule.DayOfWeek = request.DayOfWeek;
                existingSchedule.StartTime = request.StartTime;
                existingSchedule.EndTime = request.EndTime;
                existingSchedule.EffectiveFrom = request.EffectiveFrom;
                existingSchedule.EffectiveTo = request.EffectiveTo;
                existingSchedule.CapacityTotal = request.CapacityTotal;
                existingSchedule.CapacityLeft = request.CapacityLeft;
                existingSchedule.IsActive = request.IsActive;

                var updatedSchedule = await _centerScheduleRepository.UpdateCenterScheduleAsync(existingSchedule);
                return MapToCenterScheduleResponse(updatedSchedule, center);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật center schedule: {ex.Message}");
            }
        }

        public async Task<bool> DeleteCenterScheduleAsync(int centerScheduleId)
        {
            try
            {
                return await _centerScheduleRepository.DeleteCenterScheduleAsync(centerScheduleId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa center schedule: {ex.Message}");
            }
        }

        public async Task<bool> UpdateCapacityLeftAsync(int centerScheduleId, int capacityUsed)
        {
            try
            {
                var schedule = await _centerScheduleRepository.GetCenterScheduleByIdAsync(centerScheduleId);
                if (schedule == null)
                {
                    throw new ArgumentException($"Không tìm thấy center schedule với ID: {centerScheduleId}");
                }

                if (schedule.CapacityLeft < capacityUsed)
                {
                    throw new ArgumentException("Không đủ capacity còn lại");
                }

                schedule.CapacityLeft -= capacityUsed;
                await _centerScheduleRepository.UpdateCenterScheduleAsync(schedule);
                return true;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật capacity: {ex.Message}");
            }
        }

        private CenterScheduleResponse MapToCenterScheduleResponse(CenterSchedule schedule, ServiceCenter? center)
        {
            return new CenterScheduleResponse
            {
                CenterScheduleId = schedule.CenterScheduleId,
                CenterId = schedule.CenterId,
                CenterName = center?.CenterName ?? "Unknown",
                DayOfWeek = schedule.DayOfWeek,
                DayOfWeekName = GetDayOfWeekName(schedule.DayOfWeek),
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                // SlotLength removed
                EffectiveFrom = schedule.EffectiveFrom,
                EffectiveTo = schedule.EffectiveTo,
                CapacityTotal = schedule.CapacityTotal,
                CapacityLeft = schedule.CapacityLeft,
                IsActive = schedule.IsActive,
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
