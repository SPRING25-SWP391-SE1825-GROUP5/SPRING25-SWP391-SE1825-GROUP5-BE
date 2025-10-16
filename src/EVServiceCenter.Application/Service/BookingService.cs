using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Application.Service
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ICenterRepository _centerRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly ITimeSlotRepository _timeSlotRepository;
        private readonly ITechnicianRepository _technicianRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IVehicleRepository _vehicleRepository;
        // CenterSchedule removed
        private readonly ITechnicianTimeSlotRepository _technicianTimeSlotRepository;
        private readonly ILogger<BookingService> _logger;
        

        public BookingService(
            IBookingRepository bookingRepository,
            ICenterRepository centerRepository,
            IServiceRepository serviceRepository,
            ITimeSlotRepository timeSlotRepository,
            ITechnicianRepository technicianRepository,
            ICustomerRepository customerRepository,
            IVehicleRepository vehicleRepository,
            ITechnicianTimeSlotRepository technicianTimeSlotRepository,
            ILogger<BookingService> logger)
        {
            _bookingRepository = bookingRepository;
            _centerRepository = centerRepository;
            _serviceRepository = serviceRepository;
            _timeSlotRepository = timeSlotRepository;
            _technicianRepository = technicianRepository;
            _customerRepository = customerRepository;
            _vehicleRepository = vehicleRepository;
            
            _technicianTimeSlotRepository = technicianTimeSlotRepository;
            _logger = logger;
        }

        public async Task<AvailabilityResponse> GetAvailabilityAsync(int centerId, DateOnly date, List<int>? serviceIds = null)
        {
            try
            {
                // Validate center exists
                var center = await _centerRepository.GetCenterByIdAsync(centerId);
                if (center == null)
                    throw new ArgumentException("Trung tâm không tồn tại.");

                // Get all time slots
                var timeSlots = await _timeSlotRepository.GetActiveTimeSlotsAsync();
                var technicians = await _technicianRepository.GetAllTechniciansAsync();
                var centerTechnicians = technicians.Where(t => t.CenterId == centerId).ToList();

                // Get existing bookings for the date
                var existingBookings = await _bookingRepository.GetAllBookingsAsync();
                var dateStart = date.ToDateTime(TimeOnly.MinValue).Date;
                var dateEnd = dateStart.AddDays(1);
                var bookingsForDate = existingBookings
                    .Where(b => b.CenterId == centerId && b.CreatedAt >= dateStart && b.CreatedAt < dateEnd &&
                               b.Status != "CANCELLED")
                    .ToList();

                var availabilityResponse = new AvailabilityResponse
                {
                    CenterId = centerId,
                    CenterName = center.CenterName,
                    Date = date,
                    TimeSlots = new List<TimeSlotAvailability>()
                };

                foreach (var timeSlot in timeSlots)
                {
                    var timeSlotAvailability = new TimeSlotAvailability
                    {
                        SlotId = timeSlot.SlotId,
                        SlotTime = timeSlot.SlotTime.ToString(),
                        SlotLabel = timeSlot.SlotLabel,
                        IsAvailable = false,
                        AvailableTechnicians = new List<TechnicianAvailability>()
                    };

                    // Check availability for each technician
                    foreach (var technician in centerTechnicians)
                    {
                        var isTechnicianAvailable = !bookingsForDate.Any(b => 
                            b.SlotId == timeSlot.SlotId);

                        timeSlotAvailability.AvailableTechnicians.Add(new TechnicianAvailability
                        {
                            TechnicianId = technician.TechnicianId,
                            TechnicianName = technician.User?.FullName ?? "N/A",
                            IsAvailable = isTechnicianAvailable,
                            Status = isTechnicianAvailable ? "AVAILABLE" : "BUSY"
                        });
                    }

                    // Time slot is available if at least one technician is available
                    timeSlotAvailability.IsAvailable = timeSlotAvailability.AvailableTechnicians.Any(t => t.IsAvailable);

                    availabilityResponse.TimeSlots.Add(timeSlotAvailability);
                }

                return availabilityResponse;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin khả dụng: {ex.Message}");
            }
        }

        public async Task<AvailableTimesResponse> GetAvailableTimesAsync(int centerId, DateOnly date, int? technicianId = null, List<int>? serviceIds = null)
        {
            try
            {
                // Validate center exists and is active
                var center = await _centerRepository.GetCenterByIdAsync(centerId);
                if (center == null)
                    throw new ArgumentException("Trung tâm không tồn tại.");
                if (!center.IsActive)
                    throw new ArgumentException("Trung tâm hiện tại không hoạt động.");

                // Validate services are active if provided
                if (serviceIds != null && serviceIds.Any())
                {
                    var services = await _serviceRepository.GetAllServicesAsync();
                    var invalidServices = serviceIds.Where(id => 
                        !services.Any(s => s.ServiceId == id && s.IsActive)).ToList();
                    if (invalidServices.Any())
                        throw new ArgumentException($"Các dịch vụ sau không tồn tại hoặc không hoạt động: {string.Join(", ", invalidServices)}");
                }

                // Chuẩn hóa DayOfWeek: Monday=1..Sunday=7
                var dotnetDow = (int)date.DayOfWeek; // Sunday=0..Saturday=6
                var targetDow = (byte)(dotnetDow == 0 ? 7 : dotnetDow);

                // CenterSchedule removed: bỏ xác thực lịch theo ngày/tuần

                // Get technicians for the center
                var allTechnicians = await _technicianRepository.GetAllTechniciansAsync();
                var centerTechnicians = allTechnicians.Where(t => 
                    t.CenterId == centerId && t.IsActive).ToList();

                // Get existing bookings and technician time slots for the date
                var existingBookings = await _bookingRepository.GetAllBookingsAsync();
                var dStart = date.ToDateTime(TimeOnly.MinValue).Date;
                var dEnd = dStart.AddDays(1);
                var bookingsForDate = existingBookings
                    .Where(b => b.CenterId == centerId && b.CreatedAt >= dStart && b.CreatedAt < dEnd &&
                               b.Status != "CANCELLED")
                    .ToList();

                // Get technician time slots for the date
                var technicianTimeSlots = new List<TechnicianTimeSlot>();
                foreach (var tech in centerTechnicians)
                {
                    var techSlots = await GetTechnicianTimeSlotsForDate(tech.TechnicianId, date);
                    technicianTimeSlots.AddRange(techSlots);
                }

                // Generate available time slots without CenterSchedule (use defined TimeSlots)
                var availableTimeSlots = new List<AvailableTimeSlot>();
                var currentTime = DateTime.Now;
                var allTimeSlots = await _timeSlotRepository.GetAllTimeSlotsAsync();

                // optional: subtract holds via DI hold store (resolved at controller level)
                foreach (var slot in allTimeSlots)
                {
                    foreach (var technician in centerTechnicians)
                    {
                            if (technicianId.HasValue && technician.TechnicianId != technicianId.Value)
                                continue;

                        var slotId = slot.SlotId;
                        var isBooked = bookingsForDate.Any(b => b.SlotId == slotId);
                            var techTimeSlot = technicianTimeSlots.FirstOrDefault(tts => 
                                tts.TechnicianId == technician.TechnicianId && 
                                tts.WorkDate.Date == date.ToDateTime(TimeOnly.MinValue).Date && 
                                tts.SlotId == slotId);

                            var isRealtimeAvailable = !isBooked && 
                                (techTimeSlot == null || (techTimeSlot.IsAvailable && techTimeSlot.BookingId == null));

                            availableTimeSlots.Add(new AvailableTimeSlot
                            {
                                SlotId = slotId,
                            SlotTime = slot.SlotTime,
                            SlotLabel = slot.SlotLabel,
                                IsAvailable = !isBooked,
                                IsRealtimeAvailable = isRealtimeAvailable,
                                TechnicianId = technician.TechnicianId,
                            TechnicianName = technician.User?.FullName ?? "N/A",
                                Status = isBooked ? "BOOKED" : (isRealtimeAvailable ? "AVAILABLE" : "UNAVAILABLE"),
                                LastUpdated = currentTime
                            });
                    }
                }

                // Get available services
                var allServices = await _serviceRepository.GetAllServicesAsync();
                var availableServices = allServices.Where(s => s.IsActive).ToList();
                if (serviceIds != null && serviceIds.Any())
                {
                    availableServices = availableServices.Where(s => serviceIds.Contains(s.ServiceId)).ToList();
                }

                var response = new AvailableTimesResponse
                {
                    CenterId = centerId,
                    CenterName = center.CenterName,
                    Date = date,
                    TechnicianId = technicianId,
                    TechnicianName = technicianId.HasValue ? 
                        centerTechnicians.FirstOrDefault(t => t.TechnicianId == technicianId.Value)?.User?.FullName ?? "N/A" : string.Empty,
                    AvailableTimeSlots = availableTimeSlots.OrderBy(ts => ts.SlotTime).ToList(),
                    AvailableServices = availableServices.Select(s => new ServiceInfo
                    {
                        ServiceId = s.ServiceId,
                        ServiceName = s.ServiceName,
                        Description = s.Description,
                        BasePrice = s.BasePrice,
                        IsActive = s.IsActive
                    }).ToList()
                };

                return response;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin khả dụng: {ex.Message}");
            }
        }

        public async Task<bool> ReserveTimeSlotAsync(int technicianId, DateOnly date, int slotId, int? bookingId = null)
        {
            try
            {
                // Validate technician exists and is active
                var technician = await _technicianRepository.GetTechnicianByIdAsync(technicianId);
                if (technician == null || !technician.IsActive)
                    return false;

                // Check if slot is currently available
                var isAvailable = await _technicianTimeSlotRepository.IsSlotAvailableAsync(technicianId, date.ToDateTime(TimeOnly.MinValue), slotId);
                if (!isAvailable)
                    return false;

                // Reserve the slot
                return await _technicianTimeSlotRepository.ReserveSlotAsync(technicianId, date.ToDateTime(TimeOnly.MinValue), slotId, bookingId ?? 0);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> ReleaseTimeSlotAsync(int technicianId, DateOnly date, int slotId)
        {
            try
            {
                return await _technicianTimeSlotRepository.ReleaseSlotAsync(technicianId, date.ToDateTime(TimeOnly.MinValue), slotId);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request)
        {
            try
            {
                // Validate request
                await ValidateCreateBookingRequestAsync(request);

                // BookingCode removed

                // Calculate total estimated cost theo service duy nhất
                var service = await _serviceRepository.GetServiceByIdAsync(request.ServiceId);
                if (service == null || !service.IsActive)
                    throw new ArgumentException("Dịch vụ không tồn tại hoặc không hoạt động.");
                var totalEstimatedCost = service.BasePrice;

                // Single-slot model: no total slots calculation

                // Determine technician: use provided if valid else auto-assign from center
                int? selectedTechnicianId = null;
                if (request.TechnicianId.HasValue)
                {
                    var tech = await _technicianRepository.GetTechnicianByIdAsync(request.TechnicianId.Value);
                    if (tech == null || !tech.IsActive || tech.CenterId != request.CenterId)
                    {
                        throw new ArgumentException("Technician không hợp lệ cho trung tâm đã chọn.");
                    }
                    // Optional: check availability
                    var available = await _technicianTimeSlotRepository.IsSlotAvailableAsync(tech.TechnicianId, request.BookingDate.ToDateTime(TimeOnly.MinValue), request.SlotId);
                    if (!available)
                        throw new ArgumentException("Kỹ thuật viên đã bận ở khung giờ này.");
                    selectedTechnicianId = tech.TechnicianId;
                }
                else
                {
                    var techniciansInCenter = await _technicianRepository.GetTechniciansByCenterIdAsync(request.CenterId) ?? new List<Technician>();
                    var activeTechs = techniciansInCenter.Where(t => t.IsActive).ToList();
                    foreach (var tech in activeTechs)
                    {
                        var available = await _technicianTimeSlotRepository.IsSlotAvailableAsync(tech.TechnicianId, request.BookingDate.ToDateTime(TimeOnly.MinValue), request.SlotId);
                        if (available) { selectedTechnicianId = tech.TechnicianId; break; }
                    }
                    if (!selectedTechnicianId.HasValue)
                        throw new ArgumentException("Không có kỹ thuật viên khả dụng cho khung giờ này.");
                }

                // Create booking entity
                var booking = new Booking
                {
                    CustomerId = request.CustomerId,
                    VehicleId = request.VehicleId,
                    CenterId = request.CenterId,
                    SlotId = request.SlotId,
                    Status = "PENDING",
                    TotalCost = totalEstimatedCost,
                    ServiceId = request.ServiceId,
                    SpecialRequests = request.SpecialRequests?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    // TechnicianId removed from Booking
                };

                // Determine matched CenterSchedule to persist its ID
                // CenterScheduleId removed

                // Save booking
                var createdBooking = await _bookingRepository.CreateBookingAsync(booking);
                _logger.LogDebug("Booking {BookingId} created with status: {Status}", createdBooking.BookingId, createdBooking.Status);

                // Reserve selected technician slot
                try
                {
                    var reserved = await _technicianTimeSlotRepository.ReserveSlotAsync(selectedTechnicianId!.Value, request.BookingDate.ToDateTime(TimeOnly.MinValue), request.SlotId, createdBooking.BookingId);
                    _logger.LogDebug("Reserve slot: {Result}, tech={TechnicianId}, slot={SlotId}", reserved ? "OK" : "FAILED", selectedTechnicianId, request.SlotId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Reserve slot failed: {Error}", ex.Message);
                }

                // Không còn thêm vào BookingServices trong mô hình 1 dịch vụ

                // Sử dụng booking vừa tạo thay vì query lại từ database để tránh race condition
                var response = await MapToBookingResponseAsync(createdBooking);
                _logger.LogDebug("Booking {BookingId} response status: {Status}", createdBooking.BookingId, response.Status);
                return response;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo đặt lịch: {ex.Message}");
            }
        }

        public async Task<BookingResponse> GetBookingByIdAsync(int bookingId)
        {
            try
            {
                var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                if (booking == null)
                    throw new ArgumentException("Đặt lịch không tồn tại.");

                return await MapToBookingResponseAsync(bookingId);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin đặt lịch: {ex.Message}");
            }
        }

        public async Task<BookingResponse> UpdateBookingStatusAsync(int bookingId, UpdateBookingStatusRequest request)
        {
            try
            {
                // Validate booking exists
                var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                if (booking == null)
                    throw new ArgumentException("Đặt lịch không tồn tại.");

                // Validate status transition
                ValidateStatusTransition(booking.Status ?? string.Empty, request.Status);

                // Update booking status
                booking.Status = request.Status.ToUpper();
                booking.UpdatedAt = DateTime.UtcNow;

                await _bookingRepository.UpdateBookingAsync(booking);

                // Auto-remove promotions when cancelled
                if (string.Equals(booking.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                {
                    // TODO: Promotion cleanup handled at controller/service layer outside to avoid service locator pattern
                }

                return await MapToBookingResponseAsync(bookingId);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật trạng thái đặt lịch: {ex.Message}");
            }
        }

        // AssignBookingServicesAsync: đã loại bỏ trong mô hình 1 service

        private async Task<BookingResponse> MapToBookingResponseAsync(int bookingId)
        {
            var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
            if (booking == null)
                throw new ArgumentException("Đặt lịch không tồn tại.");

            return await MapToBookingResponseAsync(booking);
        }

        private async Task<BookingResponse> MapToBookingResponseAsync(Booking booking)
        {
            // Load related data if not already loaded
            if (booking.Customer == null)
            {
                booking = await _bookingRepository.GetBookingByIdAsync(booking.BookingId) ?? booking;
            }

            // Determine matched schedule for display
            DateOnly? scheduleDate = null;
            byte? scheduleDow = null;
            // CenterSchedule removed: keep schedule info null

            return new BookingResponse
            {
                BookingId = booking.BookingId,
                BookingCode = null,
                CustomerId = booking.CustomerId,
                CustomerName = booking.Customer?.User?.FullName ?? "N/A",
                VehicleId = booking.VehicleId,
                VehicleInfo = $"{booking.Vehicle?.LicensePlate ?? "N/A"}",
                CenterId = booking.CenterId,
                CenterName = booking.Center?.CenterName ?? "N/A",
                BookingDate = DateOnly.FromDateTime(booking.CreatedAt),
                SlotId = booking.SlotId,
                SlotTime = booking.Slot?.SlotTime.ToString() ?? "N/A",
                CenterScheduleDate = scheduleDate,
                CenterScheduleDayOfWeek = scheduleDow,

                Status = booking.Status ?? string.Empty,
                TotalCost = booking.TotalCost,
                SpecialRequests = booking.SpecialRequests ?? string.Empty,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt,
                // Single-slot model
                Services = new List<BookingServiceResponse>
                {
                    new BookingServiceResponse
                    {
                        ServiceId = booking.ServiceId,
                        ServiceName = booking.Service?.ServiceName ?? "N/A",
                        Quantity = 1,
                        UnitPrice = booking.TotalCost ?? 0m,
                        TotalPrice = booking.TotalCost ?? 0m
                    }
                }
            };
        }

        private async Task ValidateCreateBookingRequestAsync(CreateBookingRequest request)
        {
            var errors = new List<string>();

            // Validate customer exists
            var customer = await _customerRepository.GetCustomerByIdAsync(request.CustomerId);
            if (customer == null)
                errors.Add("Khách hàng không tồn tại.");

            // Validate vehicle exists and belongs to customer
            var vehicle = await _vehicleRepository.GetVehicleByIdAsync(request.VehicleId);
            if (vehicle == null)
            {
                errors.Add("Phương tiện không tồn tại.");
            }
            else if (vehicle.CustomerId != request.CustomerId)
            {
                errors.Add($"Phương tiện ID {request.VehicleId} không thuộc khách hàng ID {request.CustomerId}.");
            }

            // Validate center exists
            var center = await _centerRepository.GetCenterByIdAsync(request.CenterId);
            if (center == null)
                errors.Add("Trung tâm không tồn tại.");
            else if (!center.IsActive)
                errors.Add("Trung tâm hiện tại không hoạt động.");

            // Validate date is not in the past
            if (request.BookingDate < DateOnly.FromDateTime(DateTime.Today))
                errors.Add("Ngày đặt lịch không được là ngày trong quá khứ.");

            // Validate slot id
            if (request.SlotId <= 0)
                errors.Add("SlotId không hợp lệ.");

            // Validate slot exists (CenterSchedule removed)
            if (errors.Count == 0)
                {
                    var slot = await _timeSlotRepository.GetByIdAsync(request.SlotId);
                    if (slot == null)
                    {
                        errors.Add("SlotId không tồn tại.");
                }
            }

                // Validate service duy nhất
                if (request.ServiceId <= 0)
                    errors.Add("ServiceId không hợp lệ.");
                else if (vehicle != null)
            {
                // Đã loại bỏ kiểm tra theo MaintenancePolicy
            }

            if (errors.Any())
                throw new ArgumentException(string.Join(" ", errors));
        }

        private async Task ValidateBookingServicesAsync(List<BookingServiceRequest> services)
        {
            var errors = new List<string>();

            foreach (var serviceRequest in services)
            {
                var service = await _serviceRepository.GetServiceByIdAsync(serviceRequest.ServiceId);
                if (service == null)
                    errors.Add($"Dịch vụ ID {serviceRequest.ServiceId} không tồn tại.");
            }

            if (errors.Any())
                throw new ArgumentException(string.Join(" ", errors));
        }

        private void ValidateStatusTransition(string currentStatus, string newStatus)
        {
            var validTransitions = new Dictionary<string, List<string>>
            {
                { "PENDING", new List<string> { "CONFIRMED", "CANCELLED" } },
                { "CONFIRMED", new List<string> { "IN_PROGRESS", "CANCELLED" } },
                { "IN_PROGRESS", new List<string> { "COMPLETED", "CANCELLED" } },
                { "COMPLETED", new List<string>() },
                { "CANCELLED", new List<string>() }
            };

            if (!validTransitions.ContainsKey(currentStatus.ToUpper()) ||
                !validTransitions[currentStatus.ToUpper()].Contains(newStatus.ToUpper()))
            {
                throw new ArgumentException($"Không thể chuyển từ trạng thái {currentStatus} sang {newStatus}.");
            }
        }

        private string GenerateBookingCode()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"BK{timestamp}{random}";
        }

        private async Task<decimal> CalculateTotalCostAsync(List<BookingServiceRequest> services)
        {
            decimal totalCost = 0;

            foreach (var serviceRequest in services)
            {
                var service = await _serviceRepository.GetServiceByIdAsync(serviceRequest.ServiceId);
                if (service != null)
                {
                    totalCost += service.BasePrice; // mỗi dịch vụ 1 lần
                }
            }

            return totalCost;
        }

        private int CalculateTotalSlots(int startSlotId, int endSlotId)
        {
            return endSlotId - startSlotId + 1;
        }

        private async Task<List<TechnicianTimeSlot>> GetTechnicianTimeSlotsForDate(int technicianId, DateOnly date)
        {
            return await _technicianTimeSlotRepository.GetTechnicianTimeSlotsByTechnicianAndDateAsync(technicianId, date.ToDateTime(TimeOnly.MinValue));
        }

        private async Task<int> GetSlotIdByTime(TimeOnly time)
        {
            // Get all time slots and find the one that matches the time
            var timeSlots = await _timeSlotRepository.GetAllTimeSlotsAsync();
            var matchingSlot = timeSlots.FirstOrDefault(ts => ts.SlotTime == time);
            
            if (matchingSlot != null)
                return matchingSlot.SlotId;
            
            // If no exact match, find the closest slot
            var closestSlot = timeSlots
                .OrderBy(ts => Math.Abs((ts.SlotTime - time).Ticks))
                .FirstOrDefault();
            
            return closestSlot?.SlotId ?? 1; // Default to slot 1 if no slots found
        }
    }
}
