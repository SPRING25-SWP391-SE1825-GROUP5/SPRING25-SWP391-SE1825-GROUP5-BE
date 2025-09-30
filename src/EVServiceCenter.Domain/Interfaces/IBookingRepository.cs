using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IBookingRepository
    {
        Task<List<Booking>> GetAllBookingsAsync();
        Task<Booking> GetBookingByIdAsync(int bookingId);
        Task<Booking> GetBookingByCodeAsync(string bookingCode);
        Task<Booking> CreateBookingAsync(Booking booking);
        Task UpdateBookingAsync(Booking booking);
        Task<bool> BookingExistsAsync(int bookingId);
        Task<bool> IsBookingCodeUniqueAsync(string bookingCode, int? excludeBookingId = null);
        Task<List<Booking>> GetByTechnicianAndDateAsync(int technicianId, DateOnly date);
        // BookingServices removed in single-service model
        Task<List<Booking>> GetAllForAutoCancelAsync();
    }
}
