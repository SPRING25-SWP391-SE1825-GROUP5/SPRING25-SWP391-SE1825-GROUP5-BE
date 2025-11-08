using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models;
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
        private readonly ITimeSlotRepository _timeSlotRepository;

        public TechnicianTimeSlotService(
            ITechnicianTimeSlotRepository technicianTimeSlotRepository,
            ITechnicianRepository technicianRepository,
            ICenterRepository centerRepository,
            ITimeSlotRepository timeSlotRepository)
        {
            _technicianTimeSlotRepository = technicianTimeSlotRepository;
            _technicianRepository = technicianRepository;
            _centerRepository = centerRepository;
            _timeSlotRepository = timeSlotRepository;
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

                // Không cho phép tạo lịch trong quá khứ
                var today = DateTime.Today;
                if (request.WorkDate.Date < today)
                {
                    response.Success = false;
                    response.Message = $"Không thể tạo lịch trong quá khứ. Ngày làm việc phải từ hôm nay ({today:dd/MM/yyyy}) trở đi.";
                    response.Errors.Add(response.Message);
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

                // Không cho phép tạo lịch trong quá khứ
                var today = DateTime.Today;
                if (request.StartDate.Date < today)
                {
                    response.Success = false;
                    response.Message = $"Không thể tạo lịch trong quá khứ. Ngày bắt đầu phải từ hôm nay ({today:dd/MM/yyyy}) trở đi.";
                    response.Errors.Add(response.Message);
                    return response;
                }

                var createdTimeSlots = new List<TechnicianTimeSlotResponse>();
                var skippedDates = new List<string>();
                var weekendDaysSkipped = 0;
                var weekendDatesSkipped = new List<string>();

                // Create time slots for the specified date range
                var currentDate = request.StartDate;
                while (currentDate <= request.EndDate)
                {
                    // Skip weekend (Saturday = 6, Sunday = 0)
                    if (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday)
                    {
                        weekendDaysSkipped++;
                        weekendDatesSkipped.Add(currentDate.ToString("dd/MM/yyyy"));
                        currentDate = currentDate.AddDays(1);
                        continue;
                    }

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

                    if (weekendDaysSkipped > 0)
                    {
                        message += $". Đã tự động bỏ qua {weekendDaysSkipped} ngày cuối tuần (Thứ 7 và Chủ nhật): {string.Join(", ", weekendDatesSkipped)}";
                    }

                    if (skippedDates.Count > 0)
                    {
                        message += $". Đã bỏ qua {skippedDates.Count} ngày đã có lịch: {string.Join(", ", skippedDates)}";
                    }

                    response.Message = message;
                }
                else
                {
                    response.Success = false;
                    // Phân biệt giữa các trường hợp: chỉ cuối tuần, chỉ đã có lịch, hoặc cả hai
                    var totalDaysInRange = (int)(request.EndDate.Date - request.StartDate.Date).TotalDays + 1;
                    
                    if (weekendDaysSkipped == totalDaysInRange)
                    {
                        // Tất cả ngày trong khoảng đều là cuối tuần
                        response.Message = $"Không thể tạo lịch nào. Tất cả {totalDaysInRange} ngày trong khoảng đều là cuối tuần (Thứ 7 và Chủ nhật) và đã được tự động bỏ qua: {string.Join(", ", weekendDatesSkipped)}. Vui lòng chọn khoảng ngày khác bao gồm các ngày trong tuần.";
                    }
                    else if (skippedDates.Count == (totalDaysInRange - weekendDaysSkipped))
                    {
                        // Tất cả ngày làm việc (không phải cuối tuần) đều đã có lịch
                        var workingDaysCount = totalDaysInRange - weekendDaysSkipped;
                        response.Message = $"Không thể tạo lịch nào. Tất cả {workingDaysCount} ngày làm việc trong khoảng đã có lịch cho slot này: {string.Join(", ", skippedDates)}.";
                        if (weekendDaysSkipped > 0)
                        {
                            response.Message += $" Đã tự động bỏ qua {weekendDaysSkipped} ngày cuối tuần: {string.Join(", ", weekendDatesSkipped)}.";
                        }
                    }
                    else
                    {
                        // Trường hợp tổng hợp (có cả cuối tuần và đã có lịch)
                        response.Message = "Không thể tạo lịch nào. ";
                        if (weekendDaysSkipped > 0)
                        {
                            response.Message += $"Đã tự động bỏ qua {weekendDaysSkipped} ngày cuối tuần: {string.Join(", ", weekendDatesSkipped)}. ";
                        }
                        if (skippedDates.Count > 0)
                        {
                            response.Message += $"Tất cả ngày làm việc còn lại đã có lịch: {string.Join(", ", skippedDates)}.";
                        }
                    }
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

                // Không cho phép tạo lịch trong quá khứ
                var today = DateTime.Today;
                if (request.WorkDate.Date < today)
                {
                    response.Success = false;
                    response.Message = $"Không thể tạo lịch trong quá khứ. Ngày làm việc phải từ hôm nay ({today:dd/MM/yyyy}) trở đi.";
                    response.Errors.Add(response.Message);
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

                // Không cho phép tạo lịch trong quá khứ
                var today = DateTime.Today;
                if (request.StartDate.Date < today)
                {
                    response.Success = false;
                    response.Message = $"Không thể tạo lịch trong quá khứ. Ngày bắt đầu phải từ hôm nay ({today:dd/MM/yyyy}) trở đi.";
                    response.Errors.Add(response.Message);
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
                    var weekendDaysSkippedForTechnician = 0;

                    while (currentDate <= request.EndDate)
                    {
                        // Skip weekend (Saturday = 6, Sunday = 0)
                        if (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday)
                        {
                            weekendDaysSkippedForTechnician++;
                            currentDate = currentDate.AddDays(1);
                            continue;
                        }

                        try
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
                        }
                        catch
                        {
                            // Ignore duplicates - slot already exists for this technician and date
                        }

                        currentDate = currentDate.AddDays(1);
                    }

                    // Add weekend info to summary if weekend was skipped
                    if (weekendDaysSkippedForTechnician > 0)
                    {
                        technicianSummary.DayNames.Insert(0, $"[Đã bỏ qua {weekendDaysSkippedForTechnician} ngày cuối tuần]");
                    }

                    technicianTimeSlots.Add(technicianSummary);
                }

                response.Success = true;
                var message = $"Tạo lịch tuần cho tất cả technician thành công. Đã tạo {totalCreated} lịch trình cho {technicians.Count()} technician";

                // Check if any weekend days were skipped (same for all technicians in same date range)
                var testDate = request.StartDate;
                var totalWeekendDaysInRange = 0;
                while (testDate <= request.EndDate)
                {
                    if (testDate.DayOfWeek == DayOfWeek.Saturday || testDate.DayOfWeek == DayOfWeek.Sunday)
                    {
                        totalWeekendDaysInRange++;
                    }
                    testDate = testDate.AddDays(1);
                }

                if (totalWeekendDaysInRange > 0)
                {
                    message += $". Đã tự động bỏ qua {totalWeekendDaysInRange} ngày cuối tuần (Thứ 7 và Chủ nhật) cho mỗi technician";
                }

                response.Message = message;
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
                    Success = true,
                    Message = "Lấy thông tin availability thành công",
                    Data = new List<TechnicianAvailabilityData>()
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
                        var slotLabel = slot.Slot?.SlotLabel;
                        if (slotLabel == "SA" || slotLabel == "CH")
                        {
                            slotLabel = null;
                        }
                        var timeSlotAvailability = new TimeSlotAvailability
                        {
                            SlotId = slot.SlotId,
                            SlotTime = slot.Slot?.SlotTime.ToString() ?? "N/A",
                            SlotLabel = slotLabel,
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
                        availability.Data.Add(new TechnicianAvailabilityData
                        {
                            Date = currentDate.ToString("yyyy-MM-dd"),
                            TechnicianId = technician.TechnicianId,
                            TechnicianName = technician.User?.FullName ?? "N/A",
                            IsFullyBooked = false,
                            TotalSlots = 1,
                            BookedSlots = 0,
                            AvailableSlots = 1,
                            TimeSlots = new List<TimeSlotInfo>
                            {
                                new TimeSlotInfo
                                {
                                    Time = slot.Slot?.SlotTime.ToString(@"hh\:mm") ?? "Unknown",
                                    IsAvailable = true,
                                    BookingId = null
                                }
                            }
                        });
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

        public async Task<CreateTechnicianFullWeekAllSlotsResponse> CreateTechnicianFullWeekAllSlotsAsync(CreateTechnicianFullWeekAllSlotsRequest request)
        {
            var response = new CreateTechnicianFullWeekAllSlotsResponse();
            try
            {
                if (request.TechnicianId <= 0) throw new ArgumentException("TechnicianId không hợp lệ");
                if (request.StartDate.Date > request.EndDate.Date) throw new ArgumentException("Khoảng thời gian không hợp lệ");

                // Không cho phép tạo lịch trong quá khứ
                var today = DateTime.Today;
                if (request.StartDate.Date < today)
                {
                    throw new ArgumentException($"Không thể tạo lịch trong quá khứ. Ngày bắt đầu phải từ hôm nay ({today:dd/MM/yyyy}) trở đi.");
                }

                var technician = await _technicianRepository.GetTechnicianByIdAsync(request.TechnicianId);
                if (technician == null) throw new ArgumentException("Kỹ thuật viên không tồn tại");

                var timeSlots = await _timeSlotRepository.GetAllTimeSlotsAsync();

                var totalCreated = 0;
                var weekendDaysSkipped = 0;
                var weekendDatesSkipped = new List<string>();
                var currentDate = request.StartDate.Date;

                while (currentDate <= request.EndDate.Date)
                {
                    // Skip weekend (Saturday = 6, Sunday = 0)
                    if (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday)
                    {
                        weekendDaysSkipped++;
                        weekendDatesSkipped.Add(currentDate.ToString("dd/MM/yyyy"));
                        currentDate = currentDate.AddDays(1);
                        continue;
                    }

                    foreach (var slot in timeSlots)
                    {
                        try
                        {
                            var entity = new TechnicianTimeSlot
                            {
                                TechnicianId = request.TechnicianId,
                                SlotId = slot.SlotId,
                                WorkDate = currentDate,
                                IsAvailable = request.IsAvailable,
                                Notes = request.Notes,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _technicianTimeSlotRepository.CreateAsync(entity);
                            totalCreated++;
                        }
                        catch
                        {
                            // ignore duplicates
                        }
                    }
                    currentDate = currentDate.AddDays(1);
                }

                response.Success = true;
                var totalDaysInRange = (int)(request.EndDate.Date - request.StartDate.Date).TotalDays + 1;
                var workingDays = totalDaysInRange - weekendDaysSkipped;
                response.TotalDays = workingDays;
                response.TotalSlotsCreated = totalCreated;
                response.WeekendDaysSkipped = weekendDaysSkipped;
                response.WeekendDatesSkipped = weekendDatesSkipped;

                var message = $"Đã tạo lịch full tuần với toàn bộ slot cho {workingDays} ngày làm việc";
                if (weekendDaysSkipped > 0)
                {
                    message += $". Đã tự động bỏ qua {weekendDaysSkipped} ngày cuối tuần (Thứ 7 và Chủ nhật): {string.Join(", ", weekendDatesSkipped)}";
                }
                response.Message = message;
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.Errors.Add(ex.Message);
                return response;
            }
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
                    TimeSlots = daySlots.Select(slot =>
                    {
                        var slotLabel = slot.Slot?.SlotLabel;
                        if (slotLabel == "SA" || slotLabel == "CH")
                        {
                            slotLabel = null;
                        }
                        return new TimeSlotStatus
                        {
                            SlotId = slot.SlotId,
                            SlotTime = slot.Slot?.SlotTime.ToString() ?? "N/A",
                            SlotLabel = slotLabel,
                            IsAvailable = slot.IsAvailable,
                            Notes = slot.Notes,
                            TechnicianSlotId = slot.TechnicianSlotId
                        };
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
