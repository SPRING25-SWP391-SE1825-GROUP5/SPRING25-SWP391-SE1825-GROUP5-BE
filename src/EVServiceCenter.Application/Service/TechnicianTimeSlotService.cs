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
            var response = new CreateTechnicianTimeSlotResponse();

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
                response.Message = $"Lỗi khi tạo lịch technician: {ex.Message}";
                response.Errors.Add(ex.Message);
                return response;
            }
        }

        public async Task<CreateWeeklyTechnicianTimeSlotResponse> CreateWeeklyTechnicianTimeSlotAsync(CreateWeeklyTechnicianTimeSlotRequest request)
        {
            var response = new CreateWeeklyTechnicianTimeSlotResponse();

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

                // Create time slots for the specified date range
                var currentDate = request.StartDate;
                while (currentDate <= request.EndDate)
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
                    currentDate = currentDate.AddDays(1);
                }

                response.Success = true;
                response.Message = $"Tạo lịch tuần cho technician thành công. Đã tạo {createdTimeSlots.Count} lịch trình";
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
            var response = new CreateAllTechniciansTimeSlotResponse();

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
                        TechnicianCode = technician.TechnicianCode,
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
            var response = new CreateAllTechniciansWeeklyTimeSlotResponse();

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
                        TechnicianCode = technician.TechnicianCode,
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

            // Get all technicians in the center
            var technicians = await _technicianRepository.GetTechniciansByCenterIdAsync(centerId);

            foreach (var technician in technicians)
            {
                var availability = new TechnicianAvailabilityResponse
                {
                    TechnicianId = technician.TechnicianId,
                    TechnicianName = technician.User?.FullName ?? "N/A",
                    TechnicianCode = technician.TechnicianCode,
                    Date = startDate,
                    AvailableSlots = new List<TimeSlotAvailability>()
                };

                // Get available slots for this technician
                var availableSlots = await _technicianTimeSlotRepository.GetAvailableSlotsAsync(startDate, centerId);
                
                // Group by date and create availability response
                var currentDate = startDate;
                while (currentDate <= endDate)
                {
                    var daySlots = availableSlots.Where(s => s.WorkDate.Date == currentDate.Date).ToList();
                    
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
                                    TechnicianName = technician.User?.FullName ?? "N/A"
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

        private TechnicianTimeSlotResponse MapToTechnicianTimeSlotResponse(TechnicianTimeSlot timeSlot)
        {
            return new TechnicianTimeSlotResponse
            {
                TechnicianSlotId = timeSlot.TechnicianSlotId,
                TechnicianId = timeSlot.TechnicianId,
                TechnicianName = timeSlot.Technician?.User?.FullName ?? "N/A",
                TechnicianCode = timeSlot.Technician?.TechnicianCode ?? "N/A",
                SlotId = timeSlot.SlotId,
                SlotTime = timeSlot.Slot?.SlotTime.ToString() ?? "N/A",
                WorkDate = timeSlot.WorkDate,
                IsAvailable = timeSlot.IsAvailable,
                Notes = timeSlot.Notes,
                CreatedAt = timeSlot.CreatedAt
            };
        }
    }
}