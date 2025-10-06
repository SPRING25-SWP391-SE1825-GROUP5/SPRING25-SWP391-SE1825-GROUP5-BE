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
        Task<Booking> CreateBookingAsync(Booking booking);
        Task UpdateBookingAsync(Booking booking);
        Task<bool> BookingExistsAsync(int bookingId);
        Task<List<Booking>> GetByTechnicianAndDateAsync(int technicianId, DateOnly date);
        // BookingServices removed in single-service model
        Task<List<Booking>> GetAllForAutoCancelAsync();
        Task<List<Booking>> GetBookingsByCustomerIdAsync(int customerId, int page = 1, int pageSize = 10, 
            string? status = null, DateTime? fromDate = null, DateTime? toDate = null, 
            string sortBy = "createdAt", string sortOrder = "desc");
        Task<int> CountBookingsByCustomerIdAsync(int customerId, string? status = null, 
            DateTime? fromDate = null, DateTime? toDate = null);
        Task<Booking?> GetBookingWithDetailsByIdAsync(int bookingId);
    }
}
