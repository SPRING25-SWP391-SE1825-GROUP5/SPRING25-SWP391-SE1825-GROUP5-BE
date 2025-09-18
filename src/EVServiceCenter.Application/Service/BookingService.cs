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

        public BookingService(
            IBookingRepository bookingRepository,
            ICenterRepository centerRepository,
            IServiceRepository serviceRepository,
            ITimeSlotRepository timeSlotRepository,
            ITechnicianRepository technicianRepository,
            ICustomerRepository customerRepository,
            IVehicleRepository vehicleRepository)
        {
            _bookingRepository = bookingRepository;
            _centerRepository = centerRepository;
            _serviceRepository = serviceRepository;
            _timeSlotRepository = timeSlotRepository;
            _technicianRepository = technicianRepository;
            _customerRepository = customerRepository;
            _vehicleRepository = vehicleRepository;
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
                            b.BookingTimeSlots.Any(bts => 
                                bts.SlotId == timeSlot.SlotId && 
                                bts.TechnicianId == technician.TechnicianId));

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

        public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request)
        {
            try
            {
                // Validate request
                await ValidateCreateBookingRequestAsync(request);

                // Generate booking code
                var bookingCode = GenerateBookingCode();

                // Calculate total estimated cost
                var totalEstimatedCost = await CalculateTotalEstimatedCostAsync(request.Services);

                // Calculate total slots
                var totalSlots = CalculateTotalSlots(request.StartSlotId, request.EndSlotId);

                // Create booking entity
                var booking = new Booking
                {
                    BookingCode = bookingCode,
                    CustomerId = request.CustomerId,
                    VehicleId = request.VehicleId,
                    CenterId = request.CenterId,
                    BookingDate = request.BookingDate,
                    StartSlotId = request.StartSlotId,
                    EndSlotId = request.EndSlotId,
                    Status = "PENDING",
                    TotalEstimatedCost = totalEstimatedCost,
                    SpecialRequests = request.SpecialRequests?.Trim(),
                    TotalSlots = totalSlots,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Save booking
                var createdBooking = await _bookingRepository.CreateBookingAsync(booking);

                // Add booking services
                var bookingServices = new List<Domain.Entities.BookingService>();
                foreach (var serviceRequest in request.Services)
                {
                    var service = await _serviceRepository.GetServiceByIdAsync(serviceRequest.ServiceId);
                    if (service != null)
                    {
                        bookingServices.Add(new Domain.Entities.BookingService
                        {
                            BookingId = createdBooking.BookingId,
                            ServiceId = serviceRequest.ServiceId,
                            Quantity = serviceRequest.Quantity,
                            UnitPrice = service.BasePrice,
                            TotalPrice = service.BasePrice * serviceRequest.Quantity
                        });
                    }
                }

                if (bookingServices.Any())
                {
                    await _bookingRepository.AddBookingServicesAsync(bookingServices);
                }

                return await MapToBookingResponseAsync(createdBooking.BookingId);
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

        public async Task<BookingResponse> AssignBookingServicesAsync(int bookingId, AssignBookingServicesRequest request)
        {
            try
            {
                // Validate booking exists
                var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                if (booking == null)
                    throw new ArgumentException("Đặt lịch không tồn tại.");

                // Validate services
                await ValidateBookingServicesAsync(request.Services);

                // Remove existing booking services
                await _bookingRepository.RemoveBookingServicesAsync(bookingId);

                // Add new booking services
                var bookingServices = new List<Domain.Entities.BookingService>();
                decimal totalCost = 0;

                foreach (var serviceRequest in request.Services)
                {
                    var service = await _serviceRepository.GetServiceByIdAsync(serviceRequest.ServiceId);
                    if (service != null)
                    {
                        var totalPrice = service.BasePrice * serviceRequest.Quantity;
                        totalCost += totalPrice;

                        bookingServices.Add(new Domain.Entities.BookingService
                        {
                            BookingId = bookingId,
                            ServiceId = serviceRequest.ServiceId,
                            Quantity = serviceRequest.Quantity,
                            UnitPrice = service.BasePrice,
                            TotalPrice = totalPrice
                        });
                    }
                }

                if (bookingServices.Any())
                {
                    await _bookingRepository.AddBookingServicesAsync(bookingServices);
                }

                // Update booking total cost
                booking.TotalEstimatedCost = totalCost;
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
                throw new Exception($"Lỗi khi gán dịch vụ cho đặt lịch: {ex.Message}");
            }
        }

        public async Task<BookingResponse> AssignBookingTimeSlotsAsync(int bookingId, AssignBookingTimeSlotsRequest request)
        {
            try
            {
                // Validate booking exists
                var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                if (booking == null)
                    throw new ArgumentException("Đặt lịch không tồn tại.");

                // Validate time slots
                await ValidateBookingTimeSlotsAsync(bookingId, request.TimeSlots);

                // Remove existing booking time slots
                await _bookingRepository.RemoveBookingTimeSlotsAsync(bookingId);

                // Add new booking time slots
                var bookingTimeSlots = new List<BookingTimeSlot>();
                foreach (var timeSlotRequest in request.TimeSlots)
                {
                    bookingTimeSlots.Add(new BookingTimeSlot
                    {
                        BookingId = bookingId,
                        SlotId = timeSlotRequest.SlotId,
                        TechnicianId = timeSlotRequest.TechnicianId,
                        SlotOrder = timeSlotRequest.SlotOrder
                    });
                }

                if (bookingTimeSlots.Any())
                {
                    await _bookingRepository.AddBookingTimeSlotsAsync(bookingTimeSlots);
                }

                // Update booking
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
                throw new Exception($"Lỗi khi gán time slots cho đặt lịch: {ex.Message}");
            }
        }

        private async Task<BookingResponse> MapToBookingResponseAsync(int bookingId)
        {
            var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
            if (booking == null)
                throw new ArgumentException("Đặt lịch không tồn tại.");

            return new BookingResponse
            {
                BookingId = booking.BookingId,
                BookingCode = booking.BookingCode,
                CustomerId = booking.CustomerId,
                CustomerName = booking.Customer?.User?.FullName ?? "N/A",
                VehicleId = booking.VehicleId,
                VehicleInfo = $"{booking.Vehicle?.Model?.ModelName ?? "N/A"} - {booking.Vehicle?.LicensePlate ?? "N/A"}",
                CenterId = booking.CenterId,
                CenterName = booking.Center?.CenterName ?? "N/A",
                BookingDate = booking.BookingDate,
                StartSlotId = booking.StartSlotId,
                StartSlotTime = booking.StartSlot?.SlotTime.ToString() ?? "N/A",
                EndSlotId = booking.EndSlotId,
                EndSlotTime = booking.EndSlot?.SlotTime.ToString() ?? "N/A",
                Status = booking.Status,
                TotalEstimatedCost = booking.TotalEstimatedCost,
                SpecialRequests = booking.SpecialRequests,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt,
                TotalSlots = booking.TotalSlots,
                Services = booking.BookingServices?.Select(bs => new BookingServiceResponse
                {
                    ServiceId = bs.ServiceId,
                    ServiceName = bs.Service?.ServiceName ?? "N/A",
                    Quantity = bs.Quantity,
                    UnitPrice = bs.UnitPrice,
                    TotalPrice = bs.TotalPrice
                }).ToList() ?? new List<BookingServiceResponse>(),
                TimeSlots = booking.BookingTimeSlots?.Select(bts => new BookingTimeSlotResponse
                {
                    SlotId = bts.SlotId,
                    SlotTime = bts.Slot?.SlotTime.ToString() ?? "N/A",
                    SlotLabel = bts.Slot?.SlotLabel ?? "N/A",
                    TechnicianId = bts.TechnicianId,
                    TechnicianName = bts.Technician?.User?.FullName ?? "N/A",
                    SlotOrder = bts.SlotOrder
                }).ToList() ?? new List<BookingTimeSlotResponse>()
            };
        }

        private async Task ValidateCreateBookingRequestAsync(CreateBookingRequest request)
        {
            var errors = new List<string>();

            // Validate customer exists
            var customer = await _customerRepository.GetCustomerByIdAsync(request.CustomerId);
            if (customer == null)
                errors.Add("Khách hàng không tồn tại.");

            // Validate vehicle exists
            var vehicle = await _vehicleRepository.GetVehicleByIdAsync(request.VehicleId);
            if (vehicle == null)
                errors.Add("Phương tiện không tồn tại.");

            // Validate center exists
            var center = await _centerRepository.GetCenterByIdAsync(request.CenterId);
            if (center == null)
                errors.Add("Trung tâm không tồn tại.");

            // Validate date is not in the past
            if (request.BookingDate < DateOnly.FromDateTime(DateTime.Today))
                errors.Add("Ngày đặt lịch không được là ngày trong quá khứ.");

            // Validate slot range
            if (request.EndSlotId <= request.StartSlotId)
                errors.Add("Slot kết thúc phải sau slot bắt đầu.");

            // Validate services
            if (request.Services == null || !request.Services.Any())
                errors.Add("Phải có ít nhất 1 dịch vụ.");

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

        private async Task ValidateBookingTimeSlotsAsync(int bookingId, List<BookingTimeSlotRequest> timeSlots)
        {
            var errors = new List<string>();

            var allTimeSlots = await _timeSlotRepository.GetAllTimeSlotsAsync();
            var allTechnicians = await _technicianRepository.GetAllTechniciansAsync();

            foreach (var timeSlotRequest in timeSlots)
            {
                var timeSlot = allTimeSlots.FirstOrDefault(ts => ts.SlotId == timeSlotRequest.SlotId);
                if (timeSlot == null)
                    errors.Add($"Time slot ID {timeSlotRequest.SlotId} không tồn tại.");

                var technician = allTechnicians.FirstOrDefault(t => t.TechnicianId == timeSlotRequest.TechnicianId);
                if (technician == null)
                    errors.Add($"Kỹ thuật viên ID {timeSlotRequest.TechnicianId} không tồn tại.");
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
                    totalCost += service.BasePrice * serviceRequest.Quantity;
                }
            }

            return totalCost;
        }

        private int CalculateTotalSlots(int startSlotId, int endSlotId)
        {
            return endSlotId - startSlotId + 1;
        }
    }
}
