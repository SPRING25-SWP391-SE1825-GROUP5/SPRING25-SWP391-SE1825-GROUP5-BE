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
    public class TechnicianTimeSlotService : ITechnicianTimeSlotService
    {
        private readonly ITechnicianTimeSlotRepository _technicianTimeSlotRepository;
        private readonly ITechnicianRepository _technicianRepository;
        private readonly ICenterRepository _centerRepository;

        public TechnicianTimeSlotService(
            ITechnicianTimeSlotRepository technicianTimeSlotRepository,
            ITechnicianRepository technicianRepository,
            ICenterRepository centerRepository)
        {
            _technicianTimeSlotRepository = technicianTimeSlotRepository;
            _technicianRepository = technicianRepository;
            _centerRepository = centerRepository;
        }

        public async Task<CreateTechnicianTimeSlotResponse> CreateTechnicianTimeSlotAsync(CreateTechnicianTimeSlotRequest request)
        {
            var response = new CreateTechnicianTimeSlotResponse
            {
                Message = string.Empty,
                Errors = new List<string>()
            };

            try
            {
                // Validate technician exists
                var technician = await _technicianRepository.GetTechnicianByIdAsync(request.TechnicianId);
                if (technician == null)
                {
                    response.Success = false;
                    response.Message = "Không tìm thấy technician với ID đã cho";
                    return response;
                }

                // Create technician time slot
                var technicianTimeSlot = new TechnicianTimeSlot
                {
                    TechnicianId = request.TechnicianId,
                    SlotId = request.SlotId,
                    WorkDate = request.WorkDate,
                    IsAvailable = request.IsAvailable,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                var createdTimeSlot = await _technicianTimeSlotRepository.CreateAsync(technicianTimeSlot);

                response.Success = true;
                response.Message = "Tạo lịch technician thành công";
                response.CreatedTimeSlot = MapToTechnicianTimeSlotResponse(createdTimeSlot);

                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                
                // Check for duplicate key constraint violation
                if (ex.Message.Contains("UNIQUE") || ex.Message.Contains("duplicate") || ex.Message.Contains("UQ_"))
                {
                    var workDate = request.WorkDate.ToString("dd/MM/yyyy");
                    response.Message = $"Lịch cho kỹ thuật viên đã tồn tại vào ngày {workDate} với khung giờ này. Vui lòng chọn ngày khác hoặc khung giờ khác.";
                }
                else
                {
                    response.Message = $"Lỗi khi tạo lịch technician: {ex.Message}";
                }
                
                response.Errors.Add(response.Message);
                return response;
            }
        }

        public async Task<CreateWeeklyTechnicianTimeSlotResponse> CreateWeeklyTechnicianTimeSlotAsync(CreateWeeklyTechnicianTimeSlotRequest request)
        {
            var response = new CreateWeeklyTechnicianTimeSlotResponse
            {
                Message = string.Empty,
                CreatedTimeSlots = new List<TechnicianTimeSlotResponse>(),
                Errors = new List<string>()
            };

            try
            {
                // Validate technician exists
                var technician = await _technicianRepository.GetTechnicianByIdAsync(request.TechnicianId);
                if (technician == null)
                {
                    response.Success = false;
                    response.Message = "Không tìm thấy technician với ID đã cho";
                    return response;
                }

                var createdTimeSlots = new List<TechnicianTimeSlotResponse>();
                var skippedDates = new List<string>();

                // Create time slots for the specified date range
                var currentDate = request.StartDate;
                while (currentDate <= request.EndDate)
                {
                    try
                    {
                        var technicianTimeSlot = new TechnicianTimeSlot
                        {
                            TechnicianId = request.TechnicianId,
                            SlotId = request.SlotId,
                            WorkDate = currentDate,
                            IsAvailable = request.IsAvailable,
                            Notes = request.Notes,
                            CreatedAt = DateTime.UtcNow
                        };

                        var createdTimeSlot = await _technicianTimeSlotRepository.CreateAsync(technicianTimeSlot);
                        createdTimeSlots.Add(MapToTechnicianTimeSlotResponse(createdTimeSlot));
                    }
                    catch (Exception ex)
                    {
                        // Skip duplicate entries and continue with next date
                        var dateStr = currentDate.ToString("dd/MM/yyyy");
                        skippedDates.Add(dateStr);
                        
                        // Provide user-friendly error message for duplicates
                        if (ex.Message.Contains("UNIQUE") || ex.Message.Contains("duplicate") || ex.Message.Contains("UQ_"))
                        {
                            response.Errors.Add($"Bỏ qua ngày {dateStr}: Lịch đã tồn tại cho kỹ thuật viên này");
                        }
                        else
                        {
                            response.Errors.Add($"Bỏ qua ngày {dateStr}: {ex.Message}");
                        }
                    }
                    
                    currentDate = currentDate.AddDays(1);
                }

                // Determine success based on whether any slots were created
                if (createdTimeSlots.Count > 0)
                {
                    response.Success = true;
                    var message = $"Tạo lịch tuần cho technician thành công. Đã tạo {createdTimeSlots.Count} lịch trình";
                    if (skippedDates.Count > 0)
                    {
                        message += $". Đã bỏ qua {skippedDates.Count} ngày đã có lịch: {string.Join(", ", skippedDates)}";
                    }
                    response.Message = message;
                }
                else
                {
                    response.Success = false;
                    response.Message = "Không thể tạo lịch nào. Tất cả ngày trong khoảng đã có lịch cho slot này.";
                }

                response.CreatedTimeSlots = createdTimeSlots;
                response.TotalCreated = createdTimeSlots.Count;

                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Lỗi khi tạo lịch tuần cho technician: {ex.Message}";
                response.Errors.Add(ex.Message);
                return response;
            }
        }

        public async Task<CreateAllTechniciansTimeSlotResponse> CreateAllTechniciansTimeSlotAsync(CreateAllTechniciansTimeSlotRequest request)
        {
            var response = new CreateAllTechniciansTimeSlotResponse
            {
                Message = string.Empty,
                TechnicianTimeSlots = new List<TechnicianTimeSlotSummary>(),
                Errors = new List<string>()
            };

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

                // Get all technicians in the center
                var technicians = await _technicianRepository.GetTechniciansByCenterIdAsync(request.CenterId);
                if (!technicians.Any())
                {
                    response.Success = false;
                    response.Message = "Không tìm thấy technician nào trong trung tâm này";
                    return response;
                }

                var technicianTimeSlots = new List<TechnicianTimeSlotSummary>();
                var totalCreated = 0;

                foreach (var technician in technicians)
                {
                    var technicianSummary = new TechnicianTimeSlotSummary
                    {
                        TechnicianId = technician.TechnicianId,
                        TechnicianName = technician.User?.FullName ?? "N/A",
                        TimeSlotsCreated = 0,
                        TimeSlots = new List<TechnicianTimeSlotResponse>()
                    };

                    // Create technician time slot
                    var technicianTimeSlot = new TechnicianTimeSlot
                    {
                        TechnicianId = technician.TechnicianId,
                        SlotId = request.SlotId,
                        WorkDate = request.WorkDate,
                        IsAvailable = request.IsAvailable,
                        Notes = request.Notes,
                        CreatedAt = DateTime.UtcNow
                    };

                    var createdTimeSlot = await _technicianTimeSlotRepository.CreateAsync(technicianTimeSlot);
                    var timeSlotResponse = MapToTechnicianTimeSlotResponse(createdTimeSlot);

                    technicianSummary.TimeSlotsCreated = 1;
                    technicianSummary.TimeSlots.Add(timeSlotResponse);
                    technicianTimeSlots.Add(technicianSummary);
                    totalCreated++;
                }

                response.Success = true;
                response.Message = $"Tạo lịch cho tất cả technician thành công. Đã tạo {totalCreated} lịch trình cho {technicians.Count()} technician";
                response.TotalTechnicians = technicians.Count();
                response.TotalTimeSlotsCreated = totalCreated;
                response.TechnicianTimeSlots = technicianTimeSlots;

                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Lỗi khi tạo lịch cho tất cả technician: {ex.Message}";
                response.Errors.Add(ex.Message);
                return response;
            }
        }

        public async Task<CreateAllTechniciansWeeklyTimeSlotResponse> CreateAllTechniciansWeeklyTimeSlotAsync(CreateAllTechniciansWeeklyTimeSlotRequest request)
        {
            var response = new CreateAllTechniciansWeeklyTimeSlotResponse
            {
                Message = string.Empty,
                TechnicianTimeSlots = new List<TechnicianWeeklyTimeSlotSummary>(),
                Errors = new List<string>()
            };

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

                // Get all technicians in the center
                var technicians = await _technicianRepository.GetTechniciansByCenterIdAsync(request.CenterId);
                if (!technicians.Any())
                {
                    response.Success = false;
                    response.Message = "Không tìm thấy technician nào trong trung tâm này";
                    return response;
                }

                var technicianTimeSlots = new List<TechnicianWeeklyTimeSlotSummary>();
                var totalCreated = 0;

                foreach (var technician in technicians)
                {
                    var technicianSummary = new TechnicianWeeklyTimeSlotSummary
                    {
                        TechnicianId = technician.TechnicianId,
                        TechnicianName = technician.User?.FullName ?? "N/A",
                        TimeSlotsCreated = 0,
                        DayNames = new List<string>(),
                        TimeSlots = new List<TechnicianTimeSlotResponse>()
                    };

                    // Create time slots for the specified date range
                    var currentDate = request.StartDate;
                    while (currentDate <= request.EndDate)
                    {
                        var technicianTimeSlot = new TechnicianTimeSlot
                        {
                            TechnicianId = technician.TechnicianId,
                            SlotId = request.SlotId,
                            WorkDate = currentDate,
                            IsAvailable = request.IsAvailable,
                            Notes = request.Notes,
                            CreatedAt = DateTime.UtcNow
                        };

                        var createdTimeSlot = await _technicianTimeSlotRepository.CreateAsync(technicianTimeSlot);
                        var timeSlotResponse = MapToTechnicianTimeSlotResponse(createdTimeSlot);

                        technicianSummary.TimeSlotsCreated++;
                        technicianSummary.DayNames.Add(currentDate.ToString("dd/MM/yyyy"));
                        technicianSummary.TimeSlots.Add(timeSlotResponse);
                        totalCreated++;
                        currentDate = currentDate.AddDays(1);
                    }

                    technicianTimeSlots.Add(technicianSummary);
                }

                response.Success = true;
                response.Message = $"Tạo lịch tuần cho tất cả technician thành công. Đã tạo {totalCreated} lịch trình cho {technicians.Count()} technician";
                response.TotalTechnicians = technicians.Count();
                response.TotalTimeSlotsCreated = totalCreated;
                response.TechnicianTimeSlots = technicianTimeSlots;

                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Lỗi khi tạo lịch tuần cho tất cả technician: {ex.Message}";
                response.Errors.Add(ex.Message);
                return response;
            }
        }

        public async Task<TechnicianTimeSlotResponse> GetTechnicianTimeSlotByIdAsync(int id)
        {
            var timeSlot = await _technicianTimeSlotRepository.GetByIdAsync(id);
            if (timeSlot == null)
            {
                throw new ArgumentException("Không tìm thấy lịch technician với ID đã cho");
            }
            return MapToTechnicianTimeSlotResponse(timeSlot);
        }

        public async Task<List<TechnicianTimeSlotResponse>> GetTechnicianTimeSlotsByTechnicianIdAsync(int technicianId)
        {
            var timeSlots = await _technicianTimeSlotRepository.GetByTechnicianIdAsync(technicianId);
            return timeSlots.Select(MapToTechnicianTimeSlotResponse).ToList();
        }

        public async Task<List<TechnicianTimeSlotResponse>> GetTechnicianTimeSlotsByDateAsync(DateTime date)
        {
            var timeSlots = await _technicianTimeSlotRepository.GetByDateAsync(date);
            return timeSlots.Select(MapToTechnicianTimeSlotResponse).ToList();
        }

        public async Task<TechnicianTimeSlotResponse> UpdateTechnicianTimeSlotAsync(int id, UpdateTechnicianTimeSlotRequest request)
        {
            var existingTimeSlot = await _technicianTimeSlotRepository.GetByIdAsync(id);
            if (existingTimeSlot == null)
            {
                throw new ArgumentException("Không tìm thấy lịch technician với ID đã cho");
            }

            // Update properties
            existingTimeSlot.IsAvailable = request.IsAvailable;
            existingTimeSlot.Notes = request.Notes;

            var updatedTimeSlot = await _technicianTimeSlotRepository.UpdateAsync(existingTimeSlot);
            return MapToTechnicianTimeSlotResponse(updatedTimeSlot);
        }

        public async Task<bool> DeleteTechnicianTimeSlotAsync(int id)
        {
            return await _technicianTimeSlotRepository.DeleteAsync(id);
        }

        public async Task<List<TechnicianAvailabilityResponse>> GetTechnicianAvailabilityAsync(int centerId, DateTime startDate, DateTime endDate)
        {
            var response = new List<TechnicianAvailabilityResponse>();

            if (centerId <= 0) throw new ArgumentException("CenterId không hợp lệ");
            if (startDate.Date > endDate.Date) throw new ArgumentException("Khoảng thời gian không hợp lệ");

            // Get all technicians in the center
            var technicians = await _technicianRepository.GetTechniciansByCenterIdAsync(centerId);

            foreach (var technician in technicians)
            {
                var availability = new TechnicianAvailabilityResponse
                {
                    TechnicianId = technician.TechnicianId,
                    TechnicianName = technician.User?.FullName ?? "N/A",
                    Date = startDate,
                    AvailableSlots = new List<TimeSlotAvailability>()
                };

                // Get available slots for date range by aggregating per day
                var currentDate = startDate.Date;
                var aggregatedDaySlots = new List<TechnicianTimeSlot>();
                while (currentDate <= endDate.Date)
                {
                    var daySlots = await _technicianTimeSlotRepository.GetTechnicianTimeSlotsByTechnicianAndDateAsync(technician.TechnicianId, currentDate);
                    aggregatedDaySlots.AddRange(daySlots);
                    currentDate = currentDate.AddDays(1);
                }
                
                // Group by date and create availability response
                currentDate = startDate.Date;
                while (currentDate <= endDate.Date)
                {
                    var daySlots = aggregatedDaySlots.Where(s => s.WorkDate.Date == currentDate.Date).ToList();
                    
                    foreach (var slot in daySlots)
                    {
                        var timeSlotAvailability = new TimeSlotAvailability
                        {
                            SlotId = slot.SlotId,
                            SlotTime = slot.Slot?.SlotTime.ToString() ?? "N/A",
                            SlotLabel = slot.Slot?.SlotLabel ?? "N/A",
                            IsAvailable = slot.IsAvailable,
                            AvailableTechnicians = new List<TechnicianAvailability>
                            {
                                new TechnicianAvailability
                                {
                                    TechnicianId = technician.TechnicianId,
                                    TechnicianName = technician.User?.FullName ?? "N/A",
                                    IsAvailable = true,
                                    Status = "Available"
                                }
                            }
                        };
                        availability.AvailableSlots.Add(timeSlotAvailability);
                    }
                    
                    currentDate = currentDate.AddDays(1);
                }

                response.Add(availability);
            }

            return response;
        }

        public async Task<List<TechnicianTimeSlotResponse>> GetTechnicianScheduleAsync(int technicianId, DateTime startDate, DateTime endDate)
        {
            if (technicianId <= 0) throw new ArgumentException("TechnicianId không hợp lệ");
            if (startDate.Date > endDate.Date) throw new ArgumentException("Khoảng thời gian không hợp lệ");

            var technician = await _technicianRepository.GetTechnicianByIdAsync(technicianId);
            if (technician == null || !technician.IsActive) throw new InvalidOperationException("Kỹ thuật viên không tồn tại hoặc không hoạt động");

            var result = new List<TechnicianTimeSlotResponse>();
            var currentDate = startDate.Date;
            while (currentDate <= endDate.Date)
            {
                var daySlots = await _technicianTimeSlotRepository.GetTechnicianTimeSlotsByTechnicianAndDateAsync(technicianId, currentDate);
                result.AddRange(daySlots.Select(MapToTechnicianTimeSlotResponse));
                currentDate = currentDate.AddDays(1);
            }
            return result.OrderBy(r => r.WorkDate).ThenBy(r => r.SlotId).ToList();
        }

        public async Task<List<TechnicianTimeSlotResponse>> GetCenterTechnicianScheduleAsync(int centerId, DateTime startDate, DateTime endDate)
        {
            if (centerId <= 0) throw new ArgumentException("CenterId không hợp lệ");
            if (startDate.Date > endDate.Date) throw new ArgumentException("Khoảng thời gian không hợp lệ");

            var center = await _centerRepository.GetCenterByIdAsync(centerId);
            if (center == null) throw new InvalidOperationException("Trung tâm không tồn tại");

            var technicians = await _technicianRepository.GetTechniciansByCenterIdAsync(centerId);
            if (!technicians.Any()) return new List<TechnicianTimeSlotResponse>();

            var result = new List<TechnicianTimeSlotResponse>();
            foreach (var technician in technicians)
            {
                var currentDate = startDate.Date;
                while (currentDate <= endDate.Date)
                {
                    var daySlots = await _technicianTimeSlotRepository.GetTechnicianTimeSlotsByTechnicianAndDateAsync(technician.TechnicianId, currentDate);
                    result.AddRange(daySlots.Select(MapToTechnicianTimeSlotResponse));
                    currentDate = currentDate.AddDays(1);
                }
            }

            return result.OrderBy(r => r.WorkDate).ThenBy(r => r.TechnicianId).ThenBy(r => r.SlotId).ToList();
        }

        public async Task<List<TechnicianDailyScheduleResponse>> GetTechnicianDailyScheduleAsync(int technicianId, DateTime startDate, DateTime endDate)
        {
            if (technicianId <= 0) throw new ArgumentException("TechnicianId không hợp lệ");
            if (startDate.Date > endDate.Date) throw new ArgumentException("Khoảng thời gian không hợp lệ");

            var technician = await _technicianRepository.GetTechnicianByIdAsync(technicianId);
            if (technician == null || !technician.IsActive) throw new InvalidOperationException("Kỹ thuật viên không tồn tại hoặc không hoạt động");

            var result = new List<TechnicianDailyScheduleResponse>();
            var currentDate = startDate.Date;
            
            while (currentDate <= endDate.Date)
            {
                var daySlots = await _technicianTimeSlotRepository.GetTechnicianTimeSlotsByTechnicianAndDateAsync(technicianId, currentDate);
                
                var dailySchedule = new TechnicianDailyScheduleResponse
                {
                    TechnicianId = technicianId,
                    TechnicianName = technician.User?.FullName ?? "N/A",
                    WorkDate = currentDate,
                    DayOfWeek = GetDayOfWeekVietnamese(currentDate.DayOfWeek),
                    TimeSlots = daySlots.Select(slot => new TimeSlotStatus
                    {
                        SlotId = slot.SlotId,
                        SlotTime = slot.Slot?.SlotTime.ToString() ?? "N/A",
                        SlotLabel = slot.Slot?.SlotLabel ?? "N/A",
                        IsAvailable = slot.IsAvailable,
                        Notes = slot.Notes,
                        TechnicianSlotId = slot.TechnicianSlotId
                    }).OrderBy(ts => ts.SlotId).ToList()
                };
                
                result.Add(dailySchedule);
                currentDate = currentDate.AddDays(1);
            }

            return result;
        }

        

        private TechnicianTimeSlotResponse MapToTechnicianTimeSlotResponse(TechnicianTimeSlot timeSlot)
        {
            return new TechnicianTimeSlotResponse
            {
                TechnicianSlotId = timeSlot.TechnicianSlotId,
                TechnicianId = timeSlot.TechnicianId,
                TechnicianName = timeSlot.Technician?.User?.FullName ?? "N/A",
                SlotId = timeSlot.SlotId,
                SlotTime = timeSlot.Slot?.SlotTime.ToString() ?? "N/A",
                WorkDate = timeSlot.WorkDate,
                IsAvailable = timeSlot.IsAvailable,
                Notes = timeSlot.Notes,
                CreatedAt = timeSlot.CreatedAt
            };
        }

        private string GetDayOfWeekVietnamese(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Thứ 2",
                DayOfWeek.Tuesday => "Thứ 3",
                DayOfWeek.Wednesday => "Thứ 4",
                DayOfWeek.Thursday => "Thứ 5",
                DayOfWeek.Friday => "Thứ 6",
                DayOfWeek.Saturday => "Thứ 7",
                DayOfWeek.Sunday => "Chủ nhật",
                _ => "Không xác định"
            };
        }
    }
}
