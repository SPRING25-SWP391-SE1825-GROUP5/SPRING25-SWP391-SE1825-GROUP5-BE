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
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Application.Service
{
    public class TechnicianService : ITechnicianService
    {
        private readonly ITechnicianRepository _technicianRepository;
        private readonly ITimeSlotRepository _timeSlotRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IMaintenanceChecklistRepository _maintenanceChecklistRepository;
        private readonly IMaintenanceChecklistResultRepository _maintenanceChecklistResultRepository;
        private readonly ILogger<TechnicianService> _logger;

        public TechnicianService(
            ITechnicianRepository technicianRepository, 
            ITimeSlotRepository timeSlotRepository, 
            IBookingRepository bookingRepository,
            IMaintenanceChecklistRepository maintenanceChecklistRepository,
            IMaintenanceChecklistResultRepository maintenanceChecklistResultRepository,
            ILogger<TechnicianService> logger)
        {
            _technicianRepository = technicianRepository;
            _timeSlotRepository = timeSlotRepository;
            _bookingRepository = bookingRepository;
            _maintenanceChecklistRepository = maintenanceChecklistRepository;
            _maintenanceChecklistResultRepository = maintenanceChecklistResultRepository;
            _logger = logger;
        }

        public async Task<TechnicianListResponse> GetAllTechniciansAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, int? centerId = null)
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
                    Success = true,
                    Message = "Lấy thông tin availability của technician thành công",
                    Data = new List<TechnicianAvailabilityData>
                    {
                        new TechnicianAvailabilityData
                        {
                            Date = date.ToString("yyyy-MM-dd"),
                    TechnicianId = technician.TechnicianId,
                            TechnicianName = technician.User?.FullName ?? "Unknown",
                            IsFullyBooked = timeSlotAvailability.All(ts => !ts.IsAvailable),
                            TotalSlots = timeSlotAvailability.Count,
                            BookedSlots = timeSlotAvailability.Count(ts => !ts.IsAvailable),
                            AvailableSlots = timeSlotAvailability.Count(ts => ts.IsAvailable),
                            TimeSlots = timeSlotAvailability.Select(ts => new TimeSlotInfo
                            {
                                Time = ts.SlotTime,
                                IsAvailable = ts.IsAvailable,
                                BookingId = null // TimeSlotAvailability doesn't have BookingId
                            }).ToList()
                        }
                    }
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
                UserFullName = technician.User?.FullName ?? string.Empty,
                UserEmail = technician.User?.Email ?? string.Empty,
                UserPhoneNumber = technician.User?.PhoneNumber ?? string.Empty,
                CenterName = technician.Center?.CenterName ?? string.Empty,
                // CenterCity = technician.Center?.City
            };
        }


        public async Task<TechnicianBookingsResponse> GetAllBookingsAsync(int technicianId)
        {
            var result = new TechnicianBookingsResponse
            {
                TechnicianId = technicianId,
                Date = DateOnly.MinValue, // Sử dụng MinValue thay vì null
                Bookings = new List<TechnicianBookingItem>()
            };

            try
            {
                // Lấy tất cả bookings của technician
                var bookings = await _bookingRepository.GetByTechnicianAsync(technicianId);
                
                foreach (var booking in bookings)
                {
                    var bookingItem = new TechnicianBookingItem
                    {
                        BookingId = booking.BookingId,
                        Status = booking.Status ?? "N/A",
                        Date = booking.TechnicianTimeSlot?.WorkDate.ToString("yyyy-MM-dd") ?? "N/A", // Thêm Date từ TechnicianTimeSlot
                        ServiceId = booking.ServiceId,
                        ServiceName = booking.Service?.ServiceName ?? "N/A",
                        CenterId = booking.CenterId,
                        CenterName = booking.Center?.CenterName ?? "N/A",
                        SlotId = booking.TechnicianTimeSlot?.SlotId ?? 0,
                        TechnicianSlotId = booking.TechnicianTimeSlot?.TechnicianSlotId ?? 0,
                        SlotTime = booking.TechnicianTimeSlot?.Slot?.SlotTime.ToString() ?? "N/A",
                        SlotLabel = booking.TechnicianTimeSlot?.Slot?.SlotLabel ?? "N/A", // Thêm SlotLabel
                        CustomerName = booking.Customer?.User?.FullName ?? "N/A",
                        CustomerPhone = booking.Customer?.User?.PhoneNumber ?? "N/A",
                        VehiclePlate = booking.Vehicle?.LicensePlate ?? "N/A",
                        WorkStartTime = null, // Booking không có WorkStartTime
                        WorkEndTime = null   // Booking không có WorkEndTime
                    };
                    
                    result.Bookings.Add(bookingItem);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tất cả booking cho technician {TechnicianId}", technicianId);
                return new TechnicianBookingsResponse
                {
                    TechnicianId = technicianId,
                    Date = DateOnly.MinValue,
                    Bookings = new List<TechnicianBookingItem>()
                };
            }
        }

        public async Task<TechnicianBookingDetailResponse> GetBookingDetailAsync(int technicianId, int bookingId)
        {
            try
            {
                // Lấy booking detail
                var booking = await _bookingRepository.GetBookingDetailAsync(bookingId);
                
                if (booking == null)
                {
                    return new TechnicianBookingDetailResponse
                    {
                        TechnicianId = technicianId,
                        BookingId = bookingId,
                        Status = "NOT_FOUND"
                    };
                }

                // Kiểm tra booking có thuộc về technician này không
                if (booking.TechnicianTimeSlot?.TechnicianId != technicianId)
                {
                    return new TechnicianBookingDetailResponse
                    {
                        TechnicianId = technicianId,
                        BookingId = bookingId,
                        Status = "UNAUTHORIZED"
                    };
                }

                var response = new TechnicianBookingDetailResponse
                {
                    TechnicianId = technicianId,
                    BookingId = bookingId,
                    Status = booking.Status ?? "N/A",
                    Date = booking.TechnicianTimeSlot?.WorkDate.ToString("yyyy-MM-dd") ?? "N/A",
                    SlotTime = booking.TechnicianTimeSlot?.Slot?.SlotTime.ToString() ?? "N/A",
                    TechnicianSlotId = booking.TechnicianTimeSlot?.TechnicianSlotId ?? 0,
                    
                    // Service Information
                    ServiceId = booking.ServiceId,
                    ServiceName = booking.Service?.ServiceName ?? "N/A",
                    ServiceDescription = booking.Service?.Description ?? "N/A",
                    ServicePrice = booking.Service?.BasePrice ?? 0,
                    
                    // Center Information
                    CenterId = booking.CenterId,
                    CenterName = booking.Center?.CenterName ?? "N/A",
                    CenterAddress = booking.Center?.Address ?? "N/A",
                    CenterPhone = booking.Center?.PhoneNumber ?? "N/A",
                    
                    // Customer Information
                    CustomerId = booking.CustomerId,
                    CustomerName = booking.Customer?.User?.FullName ?? "N/A",
                    CustomerPhone = booking.Customer?.User?.PhoneNumber ?? "N/A",
                    CustomerAddress = booking.Customer?.User?.Address ?? "N/A",
                    CustomerEmail = booking.Customer?.User?.Email ?? "N/A",
                    
                    // Vehicle Information
                    VehicleId = booking.VehicleId,
                    VehiclePlate = booking.Vehicle?.LicensePlate ?? "N/A",
                    VehicleModel = "VinFast VF8", // Hardcode for now
                    VehicleColor = booking.Vehicle?.Color ?? "N/A",
                    CurrentMileage = booking.Vehicle?.CurrentMileage ?? 0,
                    LastServiceDate = booking.Vehicle?.LastServiceDate?.ToDateTime(TimeOnly.MinValue),
                    
                    // Maintenance Checklist
                    MaintenanceChecklists = await GetMaintenanceChecklistsAsync(bookingId),
                    
                    // Additional Information
                    SpecialRequests = booking.SpecialRequests ?? "N/A",
                    CreatedAt = booking.CreatedAt,
                    UpdatedAt = booking.UpdatedAt
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết booking {BookingId} cho technician {TechnicianId}", bookingId, technicianId);
                return new TechnicianBookingDetailResponse
                {
                    TechnicianId = technicianId,
                    BookingId = bookingId,
                    Status = "ERROR"
                };
            }
        }

        private async Task<List<MaintenanceChecklistInfo>> GetMaintenanceChecklistsAsync(int bookingId)
        {
            try
            {
                // Lấy maintenance checklist từ database (single object)
                var checklist = await _maintenanceChecklistRepository.GetByBookingIdAsync(bookingId);
                var result = new List<MaintenanceChecklistInfo>();

                if (checklist != null)
                {
                    // Lấy results cho checklist
                    var results = await _maintenanceChecklistResultRepository.GetByChecklistIdAsync(checklist.ChecklistId);
                    var resultInfos = new List<MaintenanceChecklistResultInfo>();

                    foreach (var resultItem in results)
                    {
                        resultInfos.Add(new MaintenanceChecklistResultInfo
                        {
                            ResultId = resultItem.ResultId,
                            PartId = resultItem.PartId ?? 0,
                            PartName = resultItem.Part?.PartName ?? "N/A",
                            Description = resultItem.Part?.Brand ?? "N/A", // Sử dụng Brand làm Description
                            Result = resultItem.Result,
                            Status = resultItem.Status ?? "PENDING"
                        });
                    }

                    result.Add(new MaintenanceChecklistInfo
                    {
                        ChecklistId = checklist.ChecklistId,
                        Status = checklist.Status ?? "PENDING",
                        Notes = checklist.Notes ?? "Auto-generated from template",
                        Results = resultInfos
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy maintenance checklists cho booking {BookingId}", bookingId);
                return new List<MaintenanceChecklistInfo>();
            }
        }

        public async Task<int?> GetTechnicianUserIdAsync(int technicianId)
        {
            try
            {
                var technician = await _technicianRepository.GetTechnicianByIdAsync(technicianId);
                return technician?.UserId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy technician user ID cho technician {TechnicianId}", technicianId);
                return null;
            }
        }
    }
}

