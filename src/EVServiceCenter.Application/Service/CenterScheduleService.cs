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
                    ScheduleDate = request.ScheduleDate,
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

        public async Task<CreateWeeklyCenterScheduleResponse> CreateWeeklyCenterScheduleAsync(CreateWeeklyCenterScheduleRequest request)
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


                var createdSchedules = new List<CenterScheduleResponse>();
                var errors = new List<string>();

                // Tạo lịch cho từ thứ 2 (1) đến thứ 7 (6)
                for (byte dayOfWeek = 1; dayOfWeek <= 6; dayOfWeek++)
                {
                    try
                    {
                        // Check for existing schedules for this day
                        var existingSchedules = await _centerScheduleRepository.GetCenterSchedulesByCenterAndDayAsync(
                            request.CenterId, dayOfWeek);

                        var overlappingSchedule = existingSchedules.FirstOrDefault(s =>
                            s.IsActive &&
                            ((s.StartTime <= request.StartTime && s.EndTime > request.StartTime) ||
                             (s.StartTime < request.EndTime && s.EndTime >= request.EndTime) ||
                             (s.StartTime >= request.StartTime && s.EndTime <= request.EndTime)));

                        if (overlappingSchedule != null)
                        {
                            var dayName = GetDayName(dayOfWeek);
                            errors.Add($"Thứ {dayName}: Đã tồn tại lịch trình trùng thời gian: {overlappingSchedule.StartTime} - {overlappingSchedule.EndTime}");
                            continue;
                        }

                        var centerSchedule = new CenterSchedule
                        {
                            CenterId = request.CenterId,
                            DayOfWeek = dayOfWeek,
                            StartTime = request.StartTime,
                            EndTime = request.EndTime,
                            IsActive = request.IsActive
                        };

                        var createdSchedule = await _centerScheduleRepository.CreateCenterScheduleAsync(centerSchedule);
                        createdSchedules.Add(MapToCenterScheduleResponse(createdSchedule, center));
                    }
                    catch (Exception ex)
                    {
                        var dayName = GetDayName(dayOfWeek);
                        errors.Add($"Thứ {dayName}: {ex.Message}");
                    }
                }

                var response = new CreateWeeklyCenterScheduleResponse
                {
                    Success = createdSchedules.Count > 0,
                    Message = createdSchedules.Count == 6 ? "Tạo lịch cả tuần thành công" : 
                              createdSchedules.Count > 0 ? $"Tạo thành công {createdSchedules.Count}/6 ngày trong tuần" :
                              "Không thể tạo lịch cho ngày nào",
                    CreatedSchedules = createdSchedules,
                    TotalCreated = createdSchedules.Count
                };

                if (errors.Any())
                {
                    response.Message += $". Lỗi: {string.Join("; ", errors)}";
                }

                return response;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo lịch cả tuần: {ex.Message}");
            }
        }

        public async Task<CreateAllCentersScheduleResponse> CreateAllCentersScheduleAsync(CreateAllCentersScheduleRequest request)
        {
            try
            {
                // Validate time range
                if (request.StartTime >= request.EndTime)
                {
                    throw new ArgumentException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc");
                }

                // Lấy tất cả trung tâm đang hoạt động
                var allCenters = await _centerRepository.GetAllCentersAsync();
                var activeCenters = allCenters.Where(c => c.IsActive).ToList();

                if (!activeCenters.Any())
                {
                    throw new ArgumentException("Không có trung tâm nào đang hoạt động");
                }

                var response = new CreateAllCentersScheduleResponse
                {
                    TotalCenters = activeCenters.Count,
                    CenterSchedules = new List<CenterScheduleSummary>(),
                    Errors = new List<string>()
                };

                var totalSchedulesCreated = 0;

                // Tạo lịch cho từng trung tâm
                foreach (var center in activeCenters)
                {
                    try
                    {
                        var centerSummary = new CenterScheduleSummary
                        {
                            CenterId = center.CenterId,
                            CenterName = center.CenterName,
                            SchedulesCreated = 0,
                            DayNames = new List<string>()
                        };

                        // Tạo lịch cho từ thứ 2 (1) đến thứ 7 (6)
                        for (byte dayOfWeek = 1; dayOfWeek <= 6; dayOfWeek++)
                        {
                            try
                            {
                                // Check for existing schedules for this center and day
                                var existingSchedules = await _centerScheduleRepository.GetCenterSchedulesByCenterAndDayAsync(
                                    center.CenterId, dayOfWeek);

                                var overlappingSchedule = existingSchedules.FirstOrDefault(s =>
                                    s.IsActive &&
                                    ((s.StartTime <= request.StartTime && s.EndTime > request.StartTime) ||
                                     (s.StartTime < request.EndTime && s.EndTime >= request.EndTime) ||
                                     (s.StartTime >= request.StartTime && s.EndTime <= request.EndTime)));

                                if (overlappingSchedule != null)
                                {
                                    var dayName = GetDayName(dayOfWeek);
                                    response.Errors.Add($"Trung tâm {center.CenterName} - Thứ {dayName}: Đã tồn tại lịch trình trùng thời gian");
                                    continue;
                                }

                                var centerSchedule = new CenterSchedule
                                {
                                    CenterId = center.CenterId,
                                    DayOfWeek = dayOfWeek,
                                    StartTime = request.StartTime,
                                    EndTime = request.EndTime,
                                    IsActive = request.IsActive
                                };

                                await _centerScheduleRepository.CreateCenterScheduleAsync(centerSchedule);
                                centerSummary.SchedulesCreated++;
                                centerSummary.DayNames.Add(GetDayName(dayOfWeek));
                                totalSchedulesCreated++;
                            }
                            catch (Exception ex)
                            {
                                var dayName = GetDayName(dayOfWeek);
                                response.Errors.Add($"Trung tâm {center.CenterName} - Thứ {dayName}: {ex.Message}");
                            }
                        }

                        response.CenterSchedules.Add(centerSummary);
                    }
                    catch (Exception ex)
                    {
                        response.Errors.Add($"Trung tâm {center.CenterName}: {ex.Message}");
                    }
                }

                response.Success = totalSchedulesCreated > 0;
                response.TotalSchedulesCreated = totalSchedulesCreated;
                response.Message = totalSchedulesCreated == (activeCenters.Count * 6) ? 
                    $"Tạo lịch thành công cho tất cả {activeCenters.Count} trung tâm" :
                    $"Tạo thành công {totalSchedulesCreated} lịch cho {activeCenters.Count} trung tâm";

                if (response.Errors.Any())
                {
                    response.Message += $". Có {response.Errors.Count} lỗi xảy ra";
                }

                return response;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo lịch cho tất cả trung tâm: {ex.Message}");
            }
        }

        private string GetDayName(byte dayOfWeek)
        {
            return dayOfWeek switch
            {
                1 => "Hai",
                2 => "Ba", 
                3 => "Tư",
                4 => "Năm",
                5 => "Sáu",
                6 => "Bảy",
                _ => "Không xác định"
            };
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


                // Update properties
                existingSchedule.CenterId = request.CenterId;
                existingSchedule.DayOfWeek = request.DayOfWeek;
                existingSchedule.StartTime = request.StartTime;
                existingSchedule.EndTime = request.EndTime;
                existingSchedule.ScheduleDate = request.ScheduleDate;
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
                ScheduleDate = schedule.ScheduleDate,
                // SlotLength removed
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

        public async Task<DeactivateScheduleResponse> DeactivateScheduleAsync(DeactivateScheduleRequest request)
        {
            var response = new DeactivateScheduleResponse();

            try
            {
                // Validate center exists
                var center = await _centerRepository.GetCenterByIdAsync(request.CenterId);
                if (center == null)
                {
                    response.Success = false;
                    response.Message = "Không tìm thấy trung tâm với ID đã cho";
                    return response;
                }

                // Validate day range
                if (request.StartDayOfWeek > request.EndDayOfWeek)
                {
                    response.Success = false;
                    response.Message = "Thứ bắt đầu không thể lớn hơn thứ kết thúc";
                    return response;
                }

                // Get all schedules matching the criteria for the day range
                var allSchedules = new List<CenterSchedule>();
                var updatedSchedules = new List<CenterScheduleResponse>();
                var updatedDays = new List<string>();

                for (byte dayOfWeek = request.StartDayOfWeek; dayOfWeek <= request.EndDayOfWeek; dayOfWeek++)
                {
                    var schedules = await _centerScheduleRepository.GetSchedulesByCenterDayAndTimeAsync(
                        request.CenterId, 
                        dayOfWeek, 
                        request.StartTime, 
                        request.EndTime);
                    
                    allSchedules.AddRange(schedules);
                    
                    if (schedules.Any())
                    {
                        updatedDays.Add(GetDayOfWeekName(dayOfWeek));
                    }
                }

                if (!allSchedules.Any())
                {
                    response.Success = false;
                    response.Message = "Không tìm thấy lịch trình phù hợp với tiêu chí đã cho";
                    return response;
                }

                // Update status for all matching schedules
                var scheduleIds = allSchedules.Select(s => s.CenterScheduleId).ToList();
                await _centerScheduleRepository.UpdateScheduleStatusAsync(scheduleIds, request.IsActive);

                // Get updated schedules for response
                foreach (var schedule in allSchedules)
                {
                    schedule.IsActive = request.IsActive;
                    updatedSchedules.Add(MapToCenterScheduleResponse(schedule, center));
                }

                response.Success = true;
                response.Message = request.IsActive ? 
                    $"Đã kích hoạt lại {updatedSchedules.Count} lịch trình thành công cho các ngày: {string.Join(", ", updatedDays)}" : 
                    $"Đã vô hiệu hóa {updatedSchedules.Count} lịch trình thành công cho các ngày: {string.Join(", ", updatedDays)}";
                response.TotalSchedulesUpdated = updatedSchedules.Count;
                response.UpdatedSchedules = updatedSchedules;
                response.UpdatedDays = updatedDays;

                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Có lỗi xảy ra khi cập nhật trạng thái lịch trình";
                response.Errors.Add(ex.Message);
                return response;
            }
        }
    }
}
