using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service;

public class GuestBookingService : IGuestBookingService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly ICenterRepository _centerRepository;
    private readonly ITimeSlotRepository _timeSlotRepository;
    private readonly ITechnicianRepository _technicianRepository;
    private readonly ITechnicianTimeSlotRepository _technicianTimeSlotRepository;
    private readonly PaymentService _paymentService;

    public GuestBookingService(
        ICustomerRepository customerRepository,
        IVehicleRepository vehicleRepository,
        IBookingRepository bookingRepository,
        IServiceRepository serviceRepository,
        ICenterRepository centerRepository,
        ITimeSlotRepository timeSlotRepository,
        ITechnicianRepository technicianRepository,
        ITechnicianTimeSlotRepository technicianTimeSlotRepository,
        PaymentService paymentService)
    {
        _customerRepository = customerRepository;
        _vehicleRepository = vehicleRepository;
        _bookingRepository = bookingRepository;
        _serviceRepository = serviceRepository;
        _centerRepository = centerRepository;
        _timeSlotRepository = timeSlotRepository;
        _technicianRepository = technicianRepository;
        _technicianTimeSlotRepository = technicianTimeSlotRepository;
        _paymentService = paymentService;
    }

    public async Task<GuestBookingResponse> CreateGuestBookingAsync(GuestBookingRequest request)
    {
        // Normalize
        var email = request.Email.Trim().ToLowerInvariant();
        var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);

        // Validate center, service, slot, date
        var center = await _centerRepository.GetCenterByIdAsync(request.CenterId) ?? throw new ArgumentException("Trung tâm không tồn tại.");
        if (!center.IsActive) throw new ArgumentException("Trung tâm hiện tại không hoạt động.");
        var service = await _serviceRepository.GetServiceByIdAsync(request.ServiceId) ?? throw new ArgumentException("Dịch vụ không tồn tại.");
        if (!service.IsActive) throw new ArgumentException("Dịch vụ không hoạt động.");
        if (request.BookingDate < DateOnly.FromDateTime(DateTime.Today)) throw new ArgumentException("Ngày đặt lịch không được là ngày trong quá khứ.");
        var slot = await _timeSlotRepository.GetByIdAsync(request.SlotId) ?? throw new ArgumentException("SlotId không tồn tại.");

        // CenterSchedule removed: chỉ kiểm tra Slot tồn tại & trung tâm active

        // Find or create guest customer
        var customer = await _customerRepository.GetGuestByEmailOrPhoneAsync(email, normalizedPhone);
        if (customer == null)
        {
            customer = new Customer
            {
                UserId = null,
                IsGuest = true,
                
            };
            customer = await _customerRepository.CreateCustomerAsync(customer);
        }

        // Upsert vehicle (prefer by license plate, else VIN)
        Vehicle? vehicle = null;
        if (!string.IsNullOrWhiteSpace(request.LicensePlate))
        {
            var all = await _vehicleRepository.GetAllVehiclesAsync();
            vehicle = all.FirstOrDefault(v => v.CustomerId == customer.CustomerId && v.LicensePlate == request.LicensePlate);
        }
        if (vehicle == null && !string.IsNullOrWhiteSpace(request.Vin))
        {
            var all = await _vehicleRepository.GetAllVehiclesAsync();
            vehicle = all.FirstOrDefault(v => v.CustomerId == customer.CustomerId && v.Vin == request.Vin);
        }
        if (vehicle == null)
        {
            vehicle = new Vehicle
            {
                CustomerId = customer.CustomerId,
                Vin = request.Vin,
                LicensePlate = request.LicensePlate,
                Color = request.Color,
                CurrentMileage = request.CurrentMileage,
                PurchaseDate = request.PurchaseDate,
                CreatedAt = DateTime.UtcNow
            };
            vehicle = await _vehicleRepository.CreateVehicleAsync(vehicle);
        }
        else
        {
            vehicle.Color = request.Color ?? vehicle.Color;
            vehicle.CurrentMileage = request.CurrentMileage > 0 ? request.CurrentMileage : vehicle.CurrentMileage;
            vehicle.PurchaseDate = request.PurchaseDate ?? vehicle.PurchaseDate;
            await _vehicleRepository.UpdateVehicleAsync(vehicle);
        }

        // Pick technician
        int? technicianId = null;
        if (request.TechnicianId.HasValue)
        {
            var tech = await _technicianRepository.GetTechnicianByIdAsync(request.TechnicianId.Value);
            if (tech == null || !tech.IsActive || tech.CenterId != request.CenterId)
                throw new ArgumentException("Technician không hợp lệ cho trung tâm đã chọn.");
            var available = await _technicianTimeSlotRepository.IsSlotAvailableAsync(tech.TechnicianId, request.BookingDate.ToDateTime(TimeOnly.MinValue), request.SlotId);
            if (!available) throw new ArgumentException("Kỹ thuật viên đã bận ở khung giờ này.");
            technicianId = tech.TechnicianId;
        }
        else
        {
            var techs = await _technicianRepository.GetTechniciansByCenterIdAsync(request.CenterId) ?? new System.Collections.Generic.List<Technician>();
            foreach (var t in techs.Where(t => t.IsActive))
            {
                var available = await _technicianTimeSlotRepository.IsSlotAvailableAsync(t.TechnicianId, request.BookingDate.ToDateTime(TimeOnly.MinValue), request.SlotId);
                if (available) { technicianId = t.TechnicianId; break; }
            }
            if (!technicianId.HasValue) throw new ArgumentException("Không có kỹ thuật viên khả dụng cho khung giờ này.");
        }

        // Create booking PENDING
        var booking = new Booking
        {
            CustomerId = customer.CustomerId,
            VehicleId = vehicle.VehicleId,
            CenterId = request.CenterId,
            SlotId = request.SlotId,
            Status = "PENDING",
            TotalCost = service.BasePrice,
            ServiceId = request.ServiceId,
            SpecialRequests = request.SpecialRequests?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            // TechnicianId removed from Booking
        };

        // centerScheduleId
        // CenterScheduleId removed

        booking = await _bookingRepository.CreateBookingAsync(booking);

        // Reserve slot
        try { await _technicianTimeSlotRepository.ReserveSlotAsync(technicianId!.Value, request.BookingDate.ToDateTime(TimeOnly.MinValue), request.SlotId, booking.BookingId); } catch { }

        // Create PayOS checkout link
        var checkoutUrl = await _paymentService.CreateBookingPaymentLinkAsync(booking.BookingId);

        return new GuestBookingResponse
        {
            BookingId = booking.BookingId,
            BookingCode = string.Empty,
            CheckoutUrl = checkoutUrl ?? string.Empty
        };
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("84")) return "0" + digits.Substring(2);
        return digits.StartsWith("0") ? digits : ("0" + digits);
    }

    private static string GenerateCustomerCode()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(100, 999);
        return $"CUS{timestamp}{random}";
    }

    private static string GenerateBookingCode()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"BK{timestamp}{random}";
    }
}


