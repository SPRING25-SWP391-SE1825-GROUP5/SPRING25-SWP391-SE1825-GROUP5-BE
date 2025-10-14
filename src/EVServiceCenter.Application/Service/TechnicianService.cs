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
    public class TechnicianService : ITechnicianService
    {
        private readonly ITechnicianRepository _technicianRepository;
        private readonly ITimeSlotRepository _timeSlotRepository;
        private readonly IBookingRepository _bookingRepository;

        public TechnicianService(ITechnicianRepository technicianRepository, ITimeSlotRepository timeSlotRepository, IBookingRepository bookingRepository)
        {
            _technicianRepository = technicianRepository;
            _timeSlotRepository = timeSlotRepository;
            _bookingRepository = bookingRepository;
        }
        public async Task UpsertSkillsAsync(int technicianId, UpsertTechnicianSkillsRequest request)
        {
            if (technicianId <= 0) throw new ArgumentException("TechnicianId không hợp lệ");
            if (request == null || request.Items == null || request.Items.Count == 0)
                throw new ArgumentException("Danh sách kỹ năng không được rỗng");

            var exists = await _technicianRepository.TechnicianExistsAsync(technicianId);
            if (!exists) throw new ArgumentException("Kỹ thuật viên không tồn tại.");

            var skills = request.Items.Select(i => new TechnicianSkill
            {
                TechnicianId = technicianId,
                SkillId = i.SkillId,
                // Notes removed from TechnicianSkill
            });

            await _technicianRepository.UpsertSkillsAsync(technicianId, skills);
        }

        public async Task RemoveSkillAsync(int technicianId, int skillId)
        {
            if (technicianId <= 0 || skillId <= 0)
                throw new ArgumentException("Thông tin không hợp lệ");
            var exists = await _technicianRepository.TechnicianExistsAsync(technicianId);
            if (!exists) throw new ArgumentException("Kỹ thuật viên không tồn tại.");
            await _technicianRepository.RemoveSkillAsync(technicianId, skillId);
        }

        public async Task<List<TechnicianSkillResponse>> GetTechnicianSkillsAsync(int technicianId)
        {
            // Validate input
            if (technicianId <= 0) throw new ArgumentException("TechnicianId không hợp lệ");

            // Ensure technician exists
            var exists = await _technicianRepository.TechnicianExistsAsync(technicianId);
            if (!exists) throw new ArgumentException("Kỹ thuật viên không tồn tại.");

            // Ensure technician is active
            var technician = await _technicianRepository.GetTechnicianByIdAsync(technicianId);
            if (technician == null || !technician.IsActive)
                throw new InvalidOperationException("Kỹ thuật viên không hoạt động");

            // Fetch and map skills
            var technicianSkills = await _technicianRepository.GetTechnicianSkillsAsync(technicianId);
            return technicianSkills.Select(ts => new TechnicianSkillResponse
            {
                TechnicianId = ts.TechnicianId,
                TechnicianName = ts.Technician?.User?.FullName ?? "N/A",
                SkillId = ts.SkillId,
                SkillName = ts.Skill?.Name ?? "N/A",
                SkillDescription = ts.Skill?.Description ?? "N/A",
                Notes = ts.Notes
            }).ToList();
        }
        public async Task<TechnicianBookingsResponse> GetBookingsByDateAsync(int technicianId, DateOnly date)
        {
            var result = new TechnicianBookingsResponse
            {
                TechnicianId = technicianId,
                Date = date,
                Bookings = new List<TechnicianBookingItem>()
            };

            var bookings = await _bookingRepository.GetByTechnicianAndDateAsync(technicianId, date);
            foreach (var b in bookings)
            {
                var wo = b.WorkOrders?.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
                result.Bookings.Add(new TechnicianBookingItem
                {
                    BookingId = b.BookingId,
                    BookingCode = null,
                    Status = b.Status,
                    ServiceId = b.ServiceId,
                    ServiceName = b.Service?.ServiceName ?? "N/A",
                    CenterId = b.CenterId,
                    CenterName = b.Center?.CenterName ?? "N/A",
                    SlotId = b.SlotId,
                    SlotTime = b.Slot?.SlotTime.ToString() ?? "N/A",
                    CustomerName = b.Customer?.User?.FullName ?? "N/A",
                    CustomerPhone = b.Customer?.User?.PhoneNumber,
                    VehiclePlate = b.Vehicle?.LicensePlate,
                    WorkOrderId = wo?.WorkOrderId,
                    WorkOrderStatus = wo?.Status,
                    WorkStartTime = null,
                    WorkEndTime = null
                });
            }

            return result;
        }

        public async Task<TechnicianListResponse> GetAllTechniciansAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = null, int? centerId = null)
        {
            try
            {
                var technicians = await _technicianRepository.GetAllTechniciansAsync();

                // Filtering
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    technicians = technicians.Where(t =>
                        t.User.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        t.Position.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                if (centerId.HasValue)
                {
                    technicians = technicians.Where(t => t.CenterId == centerId.Value).ToList();
                }

                // Pagination
                var totalCount = technicians.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var paginatedTechnicians = technicians.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                var technicianResponses = paginatedTechnicians.Select(t => MapToTechnicianResponse(t)).ToList();

                return new TechnicianListResponse
                {
                    Technicians = technicianResponses,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách kỹ thuật viên: {ex.Message}");
            }
        }

        public async Task<TechnicianResponse> GetTechnicianByIdAsync(int technicianId)
        {
            try
            {
                var technician = await _technicianRepository.GetTechnicianByIdAsync(technicianId);
                if (technician == null)
                    throw new ArgumentException("Kỹ thuật viên không tồn tại.");

                return MapToTechnicianResponse(technician);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin kỹ thuật viên: {ex.Message}");
            }
        }

        public async Task<TechnicianAvailabilityResponse> GetTechnicianAvailabilityAsync(int technicianId, DateOnly date)
        {
            try
            {
                // Get technician info
                var technician = await _technicianRepository.GetTechnicianByIdAsync(technicianId);
                if (technician == null)
                    throw new ArgumentException("Kỹ thuật viên không tồn tại.");

                // Get availability for the date (placeholder - implement if needed)
                var availability = new List<object>(); // await _technicianRepository.GetTechnicianAvailabilityAsync(technicianId, date);

                // Get all time slots to ensure we have complete data
                var allTimeSlots = await _timeSlotRepository.GetActiveTimeSlotsAsync();

                var timeSlotAvailability = allTimeSlots.Select(slot =>
                {
                    // var existingAvailability = availability.FirstOrDefault(a => a.SlotId == slot.SlotId);
                    
                    return new TimeSlotAvailability
                    {
                        SlotId = slot.SlotId,
                        SlotTime = slot.SlotTime.ToString(),
                        SlotLabel = slot.SlotLabel,
                        IsAvailable = true, // existingAvailability?.IsAvailable ?? false,
                        AvailableTechnicians = new List<TechnicianAvailability>()
                    };
                }).ToList();

                return new TechnicianAvailabilityResponse
                {
                    TechnicianId = technician.TechnicianId,
                    TechnicianName = technician.User.FullName,
                    Date = date.ToDateTime(TimeOnly.MinValue),
                    AvailableSlots = timeSlotAvailability
                };
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy lịch làm việc của kỹ thuật viên: {ex.Message}");
            }
        }

        public async Task<bool> UpdateTechnicianAvailabilityAsync(int technicianId, UpdateTechnicianAvailabilityRequest request)
        {
            try
            {
                // Validate technician exists
                var technician = await _technicianRepository.GetTechnicianByIdAsync(technicianId);
                if (technician == null)
                    throw new ArgumentException("Kỹ thuật viên không tồn tại.");

                // Validate time slots exist
                var allTimeSlots = await _timeSlotRepository.GetActiveTimeSlotsAsync();
                var validSlotIds = allTimeSlots.Select(ts => ts.SlotId).ToHashSet();

                foreach (var timeSlot in request.TimeSlots)
                {
                    if (!validSlotIds.Contains(timeSlot.SlotId))
                        throw new ArgumentException($"Time slot ID {timeSlot.SlotId} không tồn tại.");
                }

                // Create/update technician time slots
                var technicianTimeSlots = request.TimeSlots.Select(ts => new TechnicianTimeSlot
                {
                    TechnicianId = technicianId,
                    WorkDate = request.WorkDate.ToDateTime(TimeOnly.MinValue),
                    SlotId = ts.SlotId,
                    IsAvailable = ts.IsAvailable,
                    BookingId = null,
                    // Notes removed from TechnicianSkill
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                // await _technicianRepository.UpdateTechnicianAvailabilityAsync(technicianTimeSlots);
                // TODO: Implement availability update logic

                return true;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật lịch làm việc của kỹ thuật viên: {ex.Message}");
            }
        }

        private TechnicianResponse MapToTechnicianResponse(Technician technician)
        {
            return new TechnicianResponse
            {
                TechnicianId = technician.TechnicianId,
                UserId = technician.UserId,
                CenterId = technician.CenterId,
                Position = technician.Position,
                IsActive = technician.IsActive,
                CreatedAt = technician.CreatedAt,
                UserFullName = technician.User?.FullName,
                UserEmail = technician.User?.Email,
                UserPhoneNumber = technician.User?.PhoneNumber,
                CenterName = technician.Center?.CenterName,
                // CenterCity = technician.Center?.City
            };
        }
    }
}
