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
        private readonly IServicePackageRepository _servicePackageRepository;
        private readonly ICustomerServiceCreditRepository _customerServiceCreditRepository;
        private readonly IMaintenanceChecklistRepository _maintenanceChecklistRepository;
        private readonly IServiceChecklistRepository _serviceChecklistRepository;
        private readonly IMaintenanceChecklistResultRepository _maintenanceChecklistResultRepository;
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
            IServicePackageRepository servicePackageRepository,
            ICustomerServiceCreditRepository customerServiceCreditRepository,
            IMaintenanceChecklistRepository maintenanceChecklistRepository,
            IServiceChecklistRepository serviceChecklistRepository,
            IMaintenanceChecklistResultRepository maintenanceChecklistResultRepository,
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
            _servicePackageRepository = servicePackageRepository;
            _customerServiceCreditRepository = customerServiceCreditRepository;
            _maintenanceChecklistRepository = maintenanceChecklistRepository;
            _serviceChecklistRepository = serviceChecklistRepository;
            _maintenanceChecklistResultRepository = maintenanceChecklistResultRepository;
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
                            b.TechnicianTimeSlot?.SlotId == timeSlot.SlotId && b.TechnicianTimeSlot?.TechnicianId == technician.TechnicianId);

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
                        var isBooked = bookingsForDate.Any(b => b.TechnicianTimeSlot?.SlotId == slotId);
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

                // Calculate total estimated cost dựa trên gói hoặc dịch vụ (XOR)
                decimal totalEstimatedCost;
                ServicePackage? selectedPackage = null;

                var hasService = request.ServiceId.HasValue && request.ServiceId.Value > 0;
                var hasPackage = !string.IsNullOrWhiteSpace(request.PackageCode);
                if (hasService == hasPackage)
                    throw new ArgumentException("Phải chọn một trong hai: dịch vụ hoặc gói dịch vụ.");

                int resolvedServiceId;
                if (hasPackage)
                {
                    selectedPackage = await _servicePackageRepository.GetByPackageCodeAsync(request.PackageCode!);
                    if (selectedPackage == null || !selectedPackage.IsActive)
                        throw new ArgumentException("Gói dịch vụ không tồn tại hoặc đã ngừng hoạt động.");
                    if (selectedPackage.ValidTo < DateTime.UtcNow)
                        throw new ArgumentException("Gói dịch vụ đã hết hạn.");

                    resolvedServiceId = selectedPackage.ServiceId;
                    
                    // Lấy giá dịch vụ gốc
                    var service = await _serviceRepository.GetServiceByIdAsync(selectedPackage.ServiceId);
                    if (service == null)
                        throw new ArgumentException("Dịch vụ trong gói không tồn tại.");
                    
                    // Tính tổng tiền = Giá dịch vụ - (Giá dịch vụ × DiscountPercent)
                    var servicePrice = service.BasePrice;
                    var discountAmount = servicePrice * ((selectedPackage.DiscountPercent ?? 0) / 100);
                    totalEstimatedCost = servicePrice - discountAmount;
                }
                else
                {
                    var service = await _serviceRepository.GetServiceByIdAsync(request.ServiceId!.Value);
                if (service == null || !service.IsActive)
                    throw new ArgumentException("Dịch vụ không tồn tại hoặc không hoạt động.");
                    resolvedServiceId = service.ServiceId;
                    totalEstimatedCost = service.BasePrice;
                }

                // Single-slot model: no total slots calculation

                // Note: TechnicianId is now derived from TechnicianSlotId
                // Validate technician time slot exists and is available
                var timeSlot = await _technicianTimeSlotRepository.GetByIdAsync(request.TechnicianSlotId);
                if (timeSlot == null)
                    throw new ArgumentException("Slot thời gian không tồn tại");

                if (!timeSlot.IsAvailable)
                    throw new ArgumentException($"Khung giờ {timeSlot.Slot?.SlotLabel} ({timeSlot.Slot?.SlotTime}) của kỹ thuật viên {timeSlot.Technician?.User?.FullName} đã được đặt. Vui lòng chọn khung giờ khác.");

                var selectedTechnicianId = timeSlot.TechnicianId;

                // Kiểm tra và sử dụng CustomerServiceCredit đã có hoặc tạo mới
                int? appliedCreditId = null;
                if (hasPackage && selectedPackage != null)
                {
                    // Tìm credit đã có cho customer và package này
                    var existingCredits = await _customerServiceCreditRepository.GetByCustomerAndPackageAsync(request.CustomerId, selectedPackage.PackageId);
                    var availableCredit = existingCredits?.FirstOrDefault(c => 
                        c.Status == "ACTIVE" && 
                        c.ExpiryDate > DateTime.UtcNow && 
                        c.UsedCredits < c.TotalCredits);

                    if (availableCredit != null)
                    {
                        // Sử dụng credit đã có
                        appliedCreditId = availableCredit.CreditId;
                        _logger.LogInformation("Using existing CustomerServiceCredit {CreditId} for customer {CustomerId} with package {PackageId}", 
                            availableCredit.CreditId, request.CustomerId, selectedPackage.PackageId);
                    }
                    else
                    {
                        // Tạo credit mới nếu không có credit khả dụng
                        var customerServiceCredit = new CustomerServiceCredit
                        {
                            CustomerId = request.CustomerId,
                            PackageId = selectedPackage.PackageId,
                            ServiceId = selectedPackage.ServiceId,
                            TotalCredits = selectedPackage.TotalCredits,
                            UsedCredits = 0,
                            PurchaseDate = DateTime.UtcNow,
                            ExpiryDate = DateTime.UtcNow.AddYears(1), // 1 năm
                            Status = "ACTIVE", // Credit được tạo và active ngay lập tức
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        
                        var createdCredit = await _customerServiceCreditRepository.CreateAsync(customerServiceCredit);
                        appliedCreditId = createdCredit.CreditId;
                        _logger.LogInformation("Created new CustomerServiceCredit {CreditId} for customer {CustomerId} with package {PackageId}", 
                            createdCredit.CreditId, request.CustomerId, selectedPackage.PackageId);
                    }
                }

                // Create booking entity
                var booking = new Booking
                {
                    CustomerId = request.CustomerId,
                    VehicleId = request.VehicleId,
                    CenterId = request.CenterId,
                    TechnicianSlotId = request.TechnicianSlotId,
                    Status = "PENDING",
                    ServiceId = resolvedServiceId,
                    SpecialRequests = request.SpecialRequests?.Trim(),
                    // Fields migrated from WorkOrder
                    CurrentMileage = null, // Will be updated when work starts
                    LicensePlate = null,   // Will be updated when work starts
                    AppliedCreditId = appliedCreditId, // Gán credit ID nếu có package
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Determine matched CenterSchedule to persist its ID
                // CenterScheduleId removed

                // Reserve the technician time slot BEFORE creating booking to avoid orphaned bookings
                var reserveOk = await _technicianTimeSlotRepository.ReserveSlotAsync(
                    selectedTechnicianId,
                    request.BookingDate.ToDateTime(TimeOnly.MinValue),
                    timeSlot.SlotId,
                    null); // Reserve without booking ID first
                // Removed availability check - allow force reserve for rebooking cancelled slots
                
                // Save booking only after successful slot reservation
                var createdBooking = await _bookingRepository.CreateBookingAsync(booking);
                
                // Update the reserved slot with the actual booking ID
                await _technicianTimeSlotRepository.UpdateSlotBookingIdAsync(
                    selectedTechnicianId,
                    request.BookingDate.ToDateTime(TimeOnly.MinValue),
                    timeSlot.SlotId,
                    createdBooking.BookingId);
                _logger.LogDebug("Booking {BookingId} created with status: {Status}", createdBooking.BookingId, createdBooking.Status);

                // Tự động tạo MaintenanceChecklist từ ServiceChecklistTemplate
                await CreateMaintenanceChecklistFromTemplateAsync(createdBooking.BookingId, resolvedServiceId);

                // Không còn thêm vào BookingServices trong mô hình 1 dịch vụ

                // Sử dụng booking vừa tạo thay vì query lại từ database để tránh race condition
                var response = await MapToBookingResponseAsync(createdBooking, selectedPackage, totalEstimatedCost);
                _logger.LogDebug("Booking {BookingId} response status: {Status}", createdBooking.BookingId, response.Status);
                return response;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx) when (dbEx.InnerException is Microsoft.Data.SqlClient.SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
            {
                // Unique index violated (e.g., UX_Bookings_TechnicianSlotId)
                throw new ArgumentException("Slot đã được đặt bởi người khác. Vui lòng chọn khung giờ khác.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo đặt lịch: {ex.Message}");
            }
        }

        /// <summary>
        /// Tự động tạo MaintenanceChecklist từ ServiceChecklistTemplate khi booking thành công
        /// </summary>
        private async Task CreateMaintenanceChecklistFromTemplateAsync(int bookingId, int serviceId)
        {
            try
            {
                // Lấy template checklist cho service này
                var templates = await _serviceChecklistRepository.GetActiveAsync(serviceId);
                var template = templates.FirstOrDefault();
                
                if (template == null)
                {
                    _logger.LogWarning("Không tìm thấy ServiceChecklistTemplate cho service {ServiceId}", serviceId);
                    return;
                }

                // Tạo MaintenanceChecklist
                var checklist = new MaintenanceChecklist
                {
                    BookingId = bookingId,
                    TemplateId = template.TemplateID,
                    Status = "PENDING", // Mặc định là PENDING
                    CreatedAt = DateTime.UtcNow,
                    Notes = $"Auto-generated from template: {template.TemplateName}"
                };

                await _maintenanceChecklistRepository.CreateAsync(checklist);
                _logger.LogInformation("Đã tạo MaintenanceChecklist {ChecklistId} cho booking {BookingId} từ template {TemplateId}", 
                    checklist.ChecklistId, bookingId, template.TemplateID);

                // Seed MaintenanceChecklistResults từ ServiceChecklistTemplateItems
                try
                {
                    var templateItems = await _serviceChecklistRepository.GetItemsByTemplateAsync(template.TemplateID);
                    if (templateItems != null && templateItems.Any())
                    {
                        var seedResults = templateItems.Select(i => new MaintenanceChecklistResult
                        {
                            ChecklistId = checklist.ChecklistId,
                            PartId = i.PartID,
                            Description = i.Part?.PartName ?? string.Empty,
                            Result = null, // chưa đánh giá
                            Status = "PENDING"
                        }).ToList();

                        await _maintenanceChecklistResultRepository.UpsertManyAsync(seedResults);
                        _logger.LogInformation("Đã seed {Count} MaintenanceChecklistResults cho checklist {ChecklistId}", seedResults.Count, checklist.ChecklistId);
                    }
                    else
                    {
                        _logger.LogWarning("Template {TemplateId} không có items để seed MaintenanceChecklistResults", template.TemplateID);
                    }
                }
                catch (Exception exSeed)
                {
                    _logger.LogError(exSeed, "Lỗi khi seed MaintenanceChecklistResults cho checklist {ChecklistId}", checklist.ChecklistId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo MaintenanceChecklist cho booking {BookingId}, service {ServiceId}", bookingId, serviceId);
                // Không throw exception để không làm fail booking process
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

                // Handle package usage based on status change
                if (booking.AppliedCreditId.HasValue)
                {
                    if (string.Equals(request.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
                    {
                        // Deduct package usage when booking is completed (không cần chờ PAID)
                        await DeductPackageUsageAsync(booking.AppliedCreditId.Value);
                    }
                    else if (string.Equals(request.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                    {
                        // Refund gói luôn khi hủy booking (xóa CustomerServiceCredit)
                        await RefundPackageCompletelyAsync(booking.AppliedCreditId.Value);
                        // Clear applied credit
                        booking.AppliedCreditId = null;
                    }
                }

                // Release reserved technician slot when booking is cancelled
                if (string.Equals(request.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase)
                    && booking.TechnicianSlotId.HasValue)
                {
                    var tts = await _technicianTimeSlotRepository.GetByIdAsync(booking.TechnicianSlotId.Value);
                    if (tts != null)
                    {
                        await _technicianTimeSlotRepository.ReleaseSlotAsync(
                            tts.TechnicianId,
                            tts.WorkDate,
                            tts.SlotId);
                    }
                    booking.TechnicianSlotId = null;
                }

                // Update MaintenanceChecklist and MaintenanceChecklistResult when booking is cancelled
                if (string.Equals(request.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleMaintenanceChecklistCancellationAsync(booking.BookingId);
                }

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

        private async Task<BookingResponse> MapToBookingResponseAsync(Booking booking, ServicePackage? selectedPackage = null, decimal? totalAmount = null)
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

            // Load package information if applied
            string? packageCode = null;
            string? packageName = null;
            decimal? packageDiscountPercent = null;
            decimal? packageDiscountAmount = null;
            decimal? originalServicePrice = null;
            string paymentType = "SERVICE";
            decimal finalTotalAmount = totalAmount ?? (booking.Service?.BasePrice ?? 0);

            if (booking.AppliedCreditId.HasValue)
            {
                // Gói đã được áp dụng (sau khi thanh toán)
                var appliedCredit = await _customerServiceCreditRepository.GetByIdAsync(booking.AppliedCreditId.Value);
                if (appliedCredit?.ServicePackage != null)
                {
                    packageCode = appliedCredit.ServicePackage.PackageCode;
                    packageName = appliedCredit.ServicePackage.PackageName;
                    packageDiscountPercent = appliedCredit.ServicePackage.DiscountPercent;
                    
                    // Calculate discount amount
                    var servicePrice = booking.Service?.BasePrice ?? 0;
                    originalServicePrice = servicePrice;
                    packageDiscountAmount = servicePrice * ((packageDiscountPercent ?? 0) / 100);
                    paymentType = "PACKAGE";
                    // Tính tổng tiền = Giá dịch vụ - Discount
                    finalTotalAmount = servicePrice - (packageDiscountAmount ?? 0);
                }
            }
            else if (selectedPackage != null)
            {
                // Gói đã chọn nhưng chưa thanh toán
                packageCode = selectedPackage.PackageCode;
                packageName = selectedPackage.PackageName;
                packageDiscountPercent = selectedPackage.DiscountPercent;
                paymentType = "PACKAGE";
                
                // Tính tổng tiền = Giá dịch vụ - (Giá dịch vụ × DiscountPercent)
                var servicePrice = booking.Service?.BasePrice ?? 0;
                originalServicePrice = servicePrice;
                var discountAmount = servicePrice * ((selectedPackage.DiscountPercent ?? 0) / 100);
                packageDiscountAmount = discountAmount;
                finalTotalAmount = servicePrice - discountAmount;
            }

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
                TechnicianSlotId = booking.TechnicianSlotId,
                SlotId = booking.TechnicianTimeSlot?.SlotId ?? 0, // Get SlotId from TechnicianTimeSlot
                SlotTime = booking.TechnicianTimeSlot?.Slot?.SlotTime.ToString() ?? "N/A",
                CenterScheduleDate = scheduleDate,
                CenterScheduleDayOfWeek = scheduleDow,

                Status = booking.Status ?? string.Empty,
                SpecialRequests = booking.SpecialRequests ?? string.Empty,
                
                // Fields migrated from WorkOrder
                TechnicianId = booking.TechnicianTimeSlot?.TechnicianId,
                TechnicianName = booking.TechnicianTimeSlot?.Technician?.User?.FullName ?? "N/A",
                CurrentMileage = booking.CurrentMileage,
                LicensePlate = booking.LicensePlate,
                
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt,
                
                // Package information
                AppliedCreditId = booking.AppliedCreditId,
                PackageCode = packageCode,
                PackageName = packageName,
                PackageDiscountPercent = packageDiscountPercent,
                PackageDiscountAmount = packageDiscountAmount,
                OriginalServicePrice = originalServicePrice,
                
                // Payment information
                TotalAmount = finalTotalAmount,
                PaymentType = paymentType,
                
                // Single-slot model
                Services = new List<BookingServiceResponse>
                {
                    new BookingServiceResponse
                    {
                        ServiceId = booking.ServiceId,
                        ServiceName = booking.Service?.ServiceName ?? "N/A",
                        Quantity = 1,
                        UnitPrice = finalTotalAmount, // Sử dụng giá cuối cùng (gói hoặc dịch vụ)
                        TotalPrice = finalTotalAmount // Sử dụng giá cuối cùng (gói hoặc dịch vụ)
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

            // Validate technician slot id
            if (request.TechnicianSlotId <= 0)
                errors.Add("TechnicianSlotId không hợp lệ.");

            // Validate technician slot exists & belongs to center & matches date
            if (errors.Count == 0)
            {
                var timeSlot = await _technicianTimeSlotRepository.GetByIdAsync(request.TechnicianSlotId);
                if (timeSlot == null)
                {
                    errors.Add("TechnicianSlotId không tồn tại.");
                }
                else
                {
                    // Ensure the technician of this slot belongs to the requested center
                    var technician = await _technicianRepository.GetTechnicianByIdAsync(timeSlot.TechnicianId);
                    if (technician == null)
                    {
                        errors.Add("Kỹ thuật viên của slot không tồn tại.");
                    }
                    else if (technician.CenterId != request.CenterId)
                    {
                        errors.Add("TechnicianSlot không thuộc trung tâm đã chọn.");
                    }

                    // Ensure the slot date matches requested booking date
                    var slotDate = DateOnly.FromDateTime(timeSlot.WorkDate);
                    if (slotDate != request.BookingDate)
                    {
                        errors.Add("Ngày đặt không khớp với khung giờ đã chọn.");
                    }

                    // If booking for today, ensure slot time is not in the past
                    var today = DateOnly.FromDateTime(DateTime.Today);
                    if (slotDate == today && timeSlot.Slot != null)
                    {
                        var nowLocal = TimeOnly.FromDateTime(DateTime.Now);
                        // Convert Slot.SlotTime to TimeOnly regardless of underlying type
                        TimeOnly slotStartTime;
                        var slotObj = (object)timeSlot.Slot.SlotTime!;
                        switch (slotObj)
                        {
                            case TimeOnly to:
                                slotStartTime = to;
                                break;
                            case TimeSpan ts:
                                slotStartTime = new TimeOnly(ts.Hours, ts.Minutes, ts.Seconds);
                                break;
                            default:
                                // Fallback: treat as 00:00 to avoid throwing; validation won't block
                                slotStartTime = new TimeOnly(0, 0);
                                break;
                        }
                        if (nowLocal > slotStartTime)
                        {
                            errors.Add("Khung giờ đã qua thời điểm hiện tại. Vui lòng chọn khung giờ khác.");
                        }
                    }
                }
                // Availability check intentionally omitted to allow rebooking cancelled slots
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
                { "COMPLETED", new List<string> { "PAID" } }, // Cho phép chuyển sang PAID nếu cần
                { "PAID", new List<string>() },
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

        /// <summary>
        /// Validate and reserve a service package for booking
        /// </summary>
        private async Task<int?> ValidateAndReservePackageAsync(int customerId, int serviceId, string packageCode)
        {
            try
            {
                // Get service package by code
                var servicePackage = await _servicePackageRepository.GetByPackageCodeAsync(packageCode);
                if (servicePackage == null)
                    throw new ArgumentException($"Gói dịch vụ với mã '{packageCode}' không tồn tại.");

                // Validate package is active and valid
                if (!servicePackage.IsActive)
                    throw new ArgumentException($"Gói dịch vụ '{packageCode}' không còn hoạt động.");

                var now = DateTime.UtcNow;
                if (servicePackage.ValidFrom > now)
                    throw new ArgumentException($"Gói dịch vụ '{packageCode}' chưa có hiệu lực.");

                if (servicePackage.ValidTo.HasValue && servicePackage.ValidTo < now)
                    throw new ArgumentException($"Gói dịch vụ '{packageCode}' đã hết hạn.");

                // Validate package applies to the service
                if (servicePackage.ServiceId != serviceId)
                    throw new ArgumentException($"Gói dịch vụ '{packageCode}' không áp dụng cho dịch vụ này.");

                // Find available customer service credit
                var customerCredits = await _customerServiceCreditRepository.GetByCustomerIdAsync(customerId);
                var availableCredit = customerCredits.FirstOrDefault(cc => 
                    cc.PackageId == servicePackage.PackageId && 
                    cc.RemainingCredits > 0 &&
                    string.Equals(cc.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase));

                if (availableCredit == null)
                    throw new ArgumentException($"Khách hàng không có gói dịch vụ '{packageCode}' khả dụng hoặc đã hết lượt sử dụng.");

                // Reserve the credit (don't deduct yet, just mark as reserved)
                // For now, we'll just return the credit ID - actual deduction happens on completion
                return availableCredit.CreditId;
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi áp dụng gói dịch vụ: {ex.Message}");
            }
        }

        /// <summary>
        /// Deduct package usage when booking is completed
        /// </summary>
        private async Task DeductPackageUsageAsync(int creditId)
        {
            try
            {
                var credit = await _customerServiceCreditRepository.GetByIdAsync(creditId);
                if (credit == null || !string.Equals(credit.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                    return;

                if (credit.RemainingCredits <= 0)
                    return;

                // Deduct one usage
                credit.UsedCredits += 1;
                credit.UpdatedAt = DateTime.UtcNow;

                await _customerServiceCreditRepository.UpdateAsync(credit);
                _logger.LogInformation("Deducted package usage for credit {CreditId}. Remaining: {Remaining}", creditId, credit.RemainingCredits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deducting package usage for credit {CreditId}", creditId);
                throw new Exception($"Lỗi khi trừ lượt sử dụng gói: {ex.Message}");
            }
        }

        /// <summary>
        /// Refund package usage when booking is cancelled
        /// </summary>
        private async Task RefundPackageUsageAsync(int creditId)
        {
            try
            {
                var credit = await _customerServiceCreditRepository.GetByIdAsync(creditId);
                if (credit == null || !string.Equals(credit.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                    return;

                if (credit.UsedCredits <= 0)
                    return;

                // Refund one usage
                credit.UsedCredits = Math.Max(0, credit.UsedCredits - 1);
                credit.UpdatedAt = DateTime.UtcNow;

                await _customerServiceCreditRepository.UpdateAsync(credit);
                _logger.LogInformation("Refunded package usage for credit {CreditId}. Remaining: {Remaining}", creditId, credit.RemainingCredits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding package usage for credit {CreditId}", creditId);
                throw new Exception($"Lỗi khi hoàn lại lượt sử dụng gói: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply package to existing booking
        /// </summary>
        public async Task<BookingResponse> ApplyPackageToBookingAsync(int bookingId, ApplyPackageRequest request)
        {
            try
            {
                var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                if (booking == null)
                    throw new ArgumentException("Đặt lịch không tồn tại.");

                if (booking.AppliedCreditId.HasValue)
                    throw new ArgumentException("Đặt lịch đã áp dụng gói dịch vụ khác.");

                if (string.Equals(booking.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(booking.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Không thể áp dụng gói cho đặt lịch đã hủy hoặc hoàn thành.");

                // Validate and reserve package
                var appliedCreditId = await ValidateAndReservePackageAsync(booking.CustomerId, booking.ServiceId, request.PackageCode);

                // Update booking with applied credit
                booking.AppliedCreditId = appliedCreditId;
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
                throw new Exception($"Lỗi khi áp dụng gói dịch vụ: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove package from existing booking
        /// </summary>
        public async Task<BookingResponse> RemovePackageFromBookingAsync(int bookingId)
        {
            try
            {
                var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                if (booking == null)
                    throw new ArgumentException("Đặt lịch không tồn tại.");

                if (!booking.AppliedCreditId.HasValue)
                    throw new ArgumentException("Đặt lịch chưa áp dụng gói dịch vụ nào.");

                if (string.Equals(booking.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(booking.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Không thể gỡ gói cho đặt lịch đã hủy hoặc hoàn thành.");

                // Clear applied credit
                booking.AppliedCreditId = null;
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
                throw new Exception($"Lỗi khi gỡ gói dịch vụ: {ex.Message}");
            }
        }

        /// <summary>
        /// Refund gói hoàn toàn (xóa CustomerServiceCredit)
        /// </summary>
        /// <param name="creditId">ID của CustomerServiceCredit</param>
        private async Task RefundPackageCompletelyAsync(int creditId)
        {
            try
            {
                var credit = await _customerServiceCreditRepository.GetByIdAsync(creditId);
                if (credit == null)
                {
                    _logger.LogWarning("CustomerServiceCredit {CreditId} không tồn tại khi refund", creditId);
                    return;
                }

                // Xóa CustomerServiceCredit hoàn toàn
                await _customerServiceCreditRepository.DeleteAsync(creditId);
                
                _logger.LogInformation("Đã refund hoàn toàn gói dịch vụ CreditId: {CreditId}", creditId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi refund hoàn toàn gói dịch vụ CreditId: {CreditId}", creditId);
                throw new Exception($"Lỗi khi refund gói dịch vụ: {ex.Message}");
            }
        }

        /// <summary>
        /// Tạo gói dịch vụ sau khi thanh toán thành công
        /// </summary>
        /// <param name="bookingId">ID booking</param>
        /// <param name="packageCode">Mã gói dịch vụ</param>
        /// <returns>Thông tin booking đã cập nhật</returns>
        public async Task<BookingResponse> CreatePackageAfterPaymentAsync(int bookingId, string packageCode)
        {
            try
            {
                // Validate booking exists
                var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                if (booking == null)
                    throw new ArgumentException("Booking không tồn tại.");

                // Validate package exists and is active
                var servicePackage = await _servicePackageRepository.GetByPackageCodeAsync(packageCode);
                if (servicePackage == null || !servicePackage.IsActive)
                {
                    throw new ArgumentException($"Gói dịch vụ '{packageCode}' không tồn tại hoặc đã ngừng hoạt động.");
                }

                // Validate package applies to the service
                if (servicePackage.ServiceId != booking.ServiceId)
                {
                    throw new ArgumentException($"Gói dịch vụ '{packageCode}' không áp dụng cho dịch vụ đã chọn.");
                }

                // Validate package is still valid
                if (servicePackage.ValidTo < DateTime.UtcNow)
                {
                    throw new ArgumentException($"Gói dịch vụ '{packageCode}' đã hết hạn.");
                }

                // Create CustomerServiceCredit
                var customerServiceCredit = new CustomerServiceCredit
                {
                    CustomerId = booking.CustomerId,
                    PackageId = servicePackage.PackageId,
                    ServiceId = booking.ServiceId,
                    TotalCredits = servicePackage.TotalCredits,
                    UsedCredits = 0,
                    PurchaseDate = DateTime.UtcNow,
                    ExpiryDate = servicePackage.ValidTo,
                    Status = "ACTIVE",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdCredit = await _customerServiceCreditRepository.CreateAsync(customerServiceCredit);

                // Update booking with applied credit
                booking.AppliedCreditId = createdCredit.CreditId;
                booking.UpdatedAt = DateTime.UtcNow;
                await _bookingRepository.UpdateBookingAsync(booking);

                _logger.LogInformation("Đã tạo gói dịch vụ {PackageCode} cho booking {BookingId}, CreditId: {CreditId}", 
                    packageCode, bookingId, createdCredit.CreditId);

                return await MapToBookingResponseAsync(booking);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo gói dịch vụ cho booking {BookingId}", bookingId);
                throw new Exception("Lỗi hệ thống khi tạo gói dịch vụ.", ex);
            }
        }

        /// <summary>
        /// Xử lý hủy MaintenanceChecklist và MaintenanceChecklistResult khi booking bị hủy
        /// </summary>
        private async Task HandleMaintenanceChecklistCancellationAsync(int bookingId)
        {
            try
            {
                // Lấy MaintenanceChecklist theo BookingId
                var checklist = await _maintenanceChecklistRepository.GetByBookingIdAsync(bookingId);
                if (checklist != null)
                {
                    // Cập nhật status của MaintenanceChecklist thành CANCELLED
                    checklist.Status = "CANCELLED";
                    await _maintenanceChecklistRepository.UpdateAsync(checklist);

                    // Lấy tất cả MaintenanceChecklistResult của checklist này
                    var results = await _maintenanceChecklistResultRepository.GetByChecklistIdAsync(checklist.ChecklistId);
                    
                    // Cập nhật status và result của tất cả MaintenanceChecklistResult thành CANCELLED
                    foreach (var result in results)
                    {
                        result.Status = "CANCELLED";
                        result.Result = "CANCELLED";
                        await _maintenanceChecklistResultRepository.UpdateAsync(result);
                    }

                    _logger.LogInformation("Đã cập nhật MaintenanceChecklist và {Count} MaintenanceChecklistResult thành CANCELLED cho booking {BookingId}", 
                        results.Count, bookingId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật MaintenanceChecklist cho booking {BookingId}", bookingId);
                // Không throw exception để không ảnh hưởng đến việc cancel booking
            }
        }
    }
}

