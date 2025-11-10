using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IBookingService
    {
        Task<AvailabilityResponse> GetAvailabilityAsync(int centerId, DateOnly date, List<int>? serviceIds = null);
        Task<AvailableTimesResponse> GetAvailableTimesAsync(int centerId, DateOnly date, int? technicianId = null, List<int>? serviceIds = null);
        Task<bool> ReserveTimeSlotAsync(int technicianId, DateOnly date, int slotId, int? bookingId = null);
        Task<bool> ReleaseTimeSlotAsync(int technicianId, DateOnly date, int slotId);
        Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request);
        Task<BookingResponse> GetBookingByIdAsync(int bookingId);
        Task<BookingResponse> UpdateBookingStatusAsync(int bookingId, UpdateBookingStatusRequest request);
        Task<BookingResponse> ApplyPackageToBookingAsync(int bookingId, ApplyPackageRequest request);
        Task<BookingResponse> RemovePackageFromBookingAsync(int bookingId);
        Task<BookingResponse> CreatePackageAfterPaymentAsync(int bookingId, string packageCode);
        Task<BookingResponse> UpdateBookingCustomerPartsAsync(int bookingId, UpdateBookingCustomerPartsRequest request);
        // AssignBookingServicesAsync removed in single-service model
    }
}
