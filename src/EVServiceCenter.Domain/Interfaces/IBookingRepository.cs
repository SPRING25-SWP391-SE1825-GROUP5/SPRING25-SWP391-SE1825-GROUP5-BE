using System.Collections.Generic;
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
        Task<List<BookingService>> GetBookingServicesAsync(int bookingId);
        Task<List<BookingTimeSlot>> GetBookingTimeSlotsAsync(int bookingId);
        Task AddBookingServicesAsync(List<BookingService> bookingServices);
        Task AddBookingTimeSlotsAsync(List<BookingTimeSlot> bookingTimeSlots);
        Task RemoveBookingServicesAsync(int bookingId);
        Task RemoveBookingTimeSlotsAsync(int bookingId);
    }
}
