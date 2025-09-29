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
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ICenterRepository _centerRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly ITimeSlotRepository _timeSlotRepository;
        private readonly ITechnicianRepository _technicianRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly ICenterScheduleRepository _centerScheduleRepository;
        private readonly ITechnicianTimeSlotRepository _technicianTimeSlotRepository;
        private readonly IMaintenancePolicyRepository _maintenancePolicyRepository;

        public BookingService(
            IBookingRepository bookingRepository,
            ICenterRepository centerRepository,
            IServiceRepository serviceRepository,
            ITimeSlotRepository timeSlotRepository,
            ITechnicianRepository technicianRepository,
            ICustomerRepository customerRepository,
            IVehicleRepository vehicleRepository,
            ICenterScheduleRepository centerScheduleRepository,
            ITechnicianTimeSlotRepository technicianTimeSlotRepository,
            IMaintenancePolicyRepository maintenancePolicyRepository)
        {
            _bookingRepository = bookingRepository;
            _centerRepository = centerRepository;
            _serviceRepository = serviceRepository;
            _timeSlotRepository = timeSlotRepository;
            _technicianRepository = technicianRepository;
            _customerRepository = customerRepository;
            _vehicleRepository = vehicleRepository;
            _centerScheduleRepository = centerScheduleRepository;
            _technicianTimeSlotRepository = technicianTimeSlotRepository;
            _maintenancePolicyRepository = maintenancePolicyRepository;
        }

        public async Task<AvailabilityResponse> GetAvailabilityAsync(int centerId, DateOnly date, List<int> serviceIds = null)
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
                var bookingsForDate = existingBookings
                    .Where(b => b.CenterId == centerId && b.BookingDate == date && 
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

        public async Task<AvailableTimesResponse> GetAvailableTimesAsync(int centerId, DateOnly date, int? technicianId = null, List<int> serviceIds = null)
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

                // Lấy lịch theo ngày thực tế: lịch ngày (ScheduleDate) hoặc lịch tuần (DayOfWeek)
                var activeSchedules = await _centerScheduleRepository.GetSchedulesForDateAsync(centerId, date, targetDow);

                if (activeSchedules == null || !activeSchedules.Any())
                    throw new ArgumentException("Không có lịch hoạt động cho ngày này.");

                // Note: CenterSchedule doesn't have TechnicianId, so we'll filter technicians later

                // Get technicians for the center
                var allTechnicians = await _technicianRepository.GetAllTechniciansAsync();
                var centerTechnicians = allTechnicians.Where(t => 
                    t.CenterId == centerId && t.IsActive).ToList();

                // Get existing bookings and technician time slots for the date
                var existingBookings = await _bookingRepository.GetAllBookingsAsync();
                var bookingsForDate = existingBookings
                    .Where(b => b.CenterId == centerId && b.BookingDate == date && 
                               b.Status != "CANCELLED")
                    .ToList();

                // Get technician time slots for the date
                var technicianTimeSlots = new List<TechnicianTimeSlot>();
                foreach (var tech in centerTechnicians)
                {
                    var techSlots = await GetTechnicianTimeSlotsForDate(tech.TechnicianId, date);
                    technicianTimeSlots.AddRange(techSlots);
                }

                // Generate available time slots based on center schedule
                var availableTimeSlots = new List<AvailableTimeSlot>();
                var currentTime = DateTime.Now;

                foreach (var schedule in activeSchedules)
                {
                    var startTime = schedule.StartTime;
                    var endTime = schedule.EndTime;
                    var stepMinutes = 30; // fixed slot length

                    // Generate time slots based on schedule
                    var currentSlotTime = startTime;
                    while (currentSlotTime < endTime)
                    {
                        // For each time slot, check all available technicians
                        foreach (var technician in centerTechnicians)
                        {
                            // Filter by technician if specified
                            if (technicianId.HasValue && technician.TechnicianId != technicianId.Value)
                                continue;

                            var technicianName = technician.User?.FullName ?? "N/A";

                            // Get slot ID for current time
                            var slotId = await GetSlotIdByTime(currentSlotTime);

                            // Check if slot is available
                            var isBooked = bookingsForDate.Any(b => 
                                b.SlotId == slotId);

                            // Check technician time slot availability
                            var techTimeSlot = technicianTimeSlots.FirstOrDefault(tts => 
                                tts.TechnicianId == technician.TechnicianId && 
                                tts.WorkDate.Date == date.ToDateTime(TimeOnly.MinValue).Date && 
                                tts.SlotId == slotId);

                            var isRealtimeAvailable = !isBooked && 
                                (techTimeSlot == null || (techTimeSlot.IsAvailable && techTimeSlot.BookingId == null));

                            availableTimeSlots.Add(new AvailableTimeSlot
                            {
                                SlotId = slotId,
                                SlotTime = currentSlotTime,
                                SlotLabel = $"{currentSlotTime:HH:mm}",
                                IsAvailable = !isBooked,
                                IsRealtimeAvailable = isRealtimeAvailable,
                                TechnicianId = technician.TechnicianId,
                                TechnicianName = technicianName,
                                Status = isBooked ? "BOOKED" : (isRealtimeAvailable ? "AVAILABLE" : "UNAVAILABLE"),
                                LastUpdated = currentTime
                            });
                        }

                        currentSlotTime = currentSlotTime.AddMinutes(stepMinutes);
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
                        centerTechnicians.FirstOrDefault(t => t.TechnicianId == technicianId.Value)?.User?.FullName ?? "N/A" : null,
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

                // Generate booking code
                var bookingCode = GenerateBookingCode();

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
                    BookingCode = bookingCode,
                    CustomerId = request.CustomerId,
                    VehicleId = request.VehicleId,
                    CenterId = request.CenterId,
                    BookingDate = request.BookingDate,
                    SlotId = request.SlotId,
                    Status = "PENDING",
                    TotalEstimatedCost = totalEstimatedCost,
                    ServiceId = request.ServiceId,
                    SpecialRequests = request.SpecialRequests?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    TechnicianId = selectedTechnicianId
                };

                // Determine matched CenterSchedule to persist its ID
                try
                {
                    var dotnetDowSel = (int)request.BookingDate.DayOfWeek;
                    var targetDowSel = (byte)(dotnetDowSel == 0 ? 7 : dotnetDowSel);
                    var schedulesSel = await _centerScheduleRepository.GetSchedulesForDateAsync(request.CenterId, request.BookingDate, targetDowSel);
                    if (schedulesSel != null && schedulesSel.Any())
                    {
                        var slot = await _timeSlotRepository.GetByIdAsync(request.SlotId);
                        var matched = slot != null ? schedulesSel.FirstOrDefault(sc => slot.SlotTime >= sc.StartTime && slot.SlotTime < sc.EndTime) : null;
                        if (matched != null) booking.CenterScheduleId = matched.CenterScheduleId;
                    }
                }
                catch { }

                // Save booking
                var createdBooking = await _bookingRepository.CreateBookingAsync(booking);
                Console.WriteLine($"[DEBUG] Booking {createdBooking.BookingId} created with status: {createdBooking.Status}");

                // Reserve selected technician slot
                try
                {
                    var reserved = await _technicianTimeSlotRepository.ReserveSlotAsync(selectedTechnicianId!.Value, request.BookingDate.ToDateTime(TimeOnly.MinValue), request.SlotId, createdBooking.BookingId);
                    Console.WriteLine($"[DEBUG] Reserve slot: {(reserved ? "OK" : "FAILED")}, tech={selectedTechnicianId}, slot={request.SlotId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARN] Reserve slot failed: {ex.Message}");
                }

                // Không còn thêm vào BookingServices trong mô hình 1 dịch vụ

                // Sử dụng booking vừa tạo thay vì query lại từ database để tránh race condition
                var response = await MapToBookingResponseAsync(createdBooking);
                Console.WriteLine($"[DEBUG] Booking {createdBooking.BookingId} response status: {response.Status}");
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
                ValidateStatusTransition(booking.Status, request.Status);

                // Update booking status
                booking.Status = request.Status.ToUpper();
                booking.UpdatedAt = DateTime.UtcNow;

                await _bookingRepository.UpdateBookingAsync(booking);

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
                booking = await _bookingRepository.GetBookingByIdAsync(booking.BookingId);
            }

            // Determine matched schedule for display
            DateOnly? scheduleDate = null;
            byte? scheduleDow = null;
            try
            {
                var dotnetDow = (int)booking.BookingDate.DayOfWeek;
                var targetDow = (byte)(dotnetDow == 0 ? 7 : dotnetDow);
                var schedules = await _centerScheduleRepository.GetSchedulesForDateAsync(booking.CenterId, booking.BookingDate, targetDow);
                if (schedules != null && schedules.Any())
                {
                    var slotTime = booking.Slot?.SlotTime;
                    var matched = schedules.FirstOrDefault(sc => slotTime.HasValue && slotTime.Value >= sc.StartTime && slotTime.Value < sc.EndTime);
                    if (matched != null)
                    {
                        scheduleDate = matched.ScheduleDate;
                        scheduleDow = matched.DayOfWeek;
                    }
                }
            }
            catch { }

            return new BookingResponse
            {
                BookingId = booking.BookingId,
                BookingCode = booking.BookingCode,
                CustomerId = booking.CustomerId,
                CustomerName = booking.Customer?.User?.FullName ?? "N/A",
                VehicleId = booking.VehicleId,
                VehicleInfo = $"{booking.Vehicle?.LicensePlate ?? "N/A"}",
                CenterId = booking.CenterId,
                CenterName = booking.Center?.CenterName ?? "N/A",
                BookingDate = booking.BookingDate,
                SlotId = booking.SlotId,
                SlotTime = booking.Slot?.SlotTime.ToString() ?? "N/A",
                CenterScheduleDate = scheduleDate,
                CenterScheduleDayOfWeek = scheduleDow,
                CenterScheduleId = booking.CenterScheduleId,
                TechnicianId = booking.TechnicianId,
                Status = booking.Status,
                TotalEstimatedCost = booking.TotalEstimatedCost,
                SpecialRequests = booking.SpecialRequests,
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
                        UnitPrice = booking.TotalEstimatedCost ?? 0m,
                        TotalPrice = booking.TotalEstimatedCost ?? 0m
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

            // Validate date-slot must fall into active CenterSchedule for that date
            if (errors.Count == 0) // only if earlier validations passed
            {
                // Map DayOfWeek: Monday=1..Sunday=7
                var dotnetDow = (int)request.BookingDate.DayOfWeek;
                var targetDow = (byte)(dotnetDow == 0 ? 7 : dotnetDow);

                var schedules = await _centerScheduleRepository.GetSchedulesForDateAsync(request.CenterId, request.BookingDate, targetDow);
                if (schedules == null || !schedules.Any())
                {
                    errors.Add("Trung tâm không có lịch hoạt động cho ngày đã chọn.");
                }
                else
                {
                    var slot = await _timeSlotRepository.GetByIdAsync(request.SlotId);
                    if (slot == null)
                    {
                        errors.Add("SlotId không tồn tại.");
                    }
                    else
                    {
                        // chosen slot time must be within any schedule range
                        var slotTime = slot.SlotTime;
                        var inAnySchedule = schedules.Any(sc => slotTime >= sc.StartTime && slotTime < sc.EndTime && sc.IsActive);
                        if (!inAnySchedule)
                        {
                            errors.Add("Khung giờ đã chọn không nằm trong lịch hoạt động của trung tâm cho ngày này.");
                        }
                    }
                }
            }

                // Validate service duy nhất
                if (request.ServiceId <= 0)
                    errors.Add("ServiceId không hợp lệ.");
                else if (vehicle != null)
            {
                // Validate against maintenance policies: require either mileage >= IntervalKm OR months >= IntervalMonths
                var purchaseMonths = vehicle.PurchaseDate.HasValue ? ((DateOnly.FromDateTime(DateTime.UtcNow).Year - vehicle.PurchaseDate.Value.Year) * 12 + (DateOnly.FromDateTime(DateTime.UtcNow).Month - vehicle.PurchaseDate.Value.Month)) : (int?)null;

                var svcId = request.ServiceId;
                {
                    var policies = await _maintenancePolicyRepository.GetActiveByServiceIdAsync(svcId);
                    if (policies != null && policies.Any())
                    {
                        // Choose the strictest minimal requirement (min thresholds)
                        var minKm = policies.Min(p => p.IntervalKm);
                        var minMonths = policies.Min(p => p.IntervalMonths);

                        var mileageOk = vehicle.CurrentMileage >= minKm;
                        var monthsOk = purchaseMonths.HasValue && purchaseMonths.Value >= minMonths;

                        if (!mileageOk && !monthsOk)
                        {
                            errors.Add($"Xe chưa đạt điều kiện cho dịch vụ {svcId}: yêu cầu tối thiểu {minKm} km hoặc {minMonths} tháng.");
                        }
                    }
                }
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

        private async Task<decimal> CalculateTotalEstimatedCostAsync(List<BookingServiceRequest> services)
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
