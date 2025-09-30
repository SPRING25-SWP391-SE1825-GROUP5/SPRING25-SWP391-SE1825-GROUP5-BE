using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly EVDbContext _context;

        public BookingRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<Booking>> GetAllBookingsAsync()
        {
            try
            {
                return await _context.Bookings
                    .Include(b => b.Customer)
                    .ThenInclude(c => c.User)
                    .Include(b => b.Vehicle)
                    .Include(b => b.Center)
                    .Include(b => b.Slot)
                    .Include(b => b.Service)
                    .Include(b => b.Technician)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Log the detailed error for debugging
                Console.WriteLine($"Error in GetAllBookingsAsync: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                throw;
            }
        }

        public async Task<Booking> GetBookingByIdAsync(int bookingId)
        {
            return await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Customer.User)
                .Include(b => b.Vehicle)
                .Include(b => b.Center)
                .Include(b => b.Slot)
                .Include(b => b.Service)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }

        public async Task<Booking> GetBookingByCodeAsync(string bookingCode)
        {
            return await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Customer.User)
                .Include(b => b.Vehicle)
                .Include(b => b.Center)
                .Include(b => b.Slot)
                .Include(b => b.Service)
                .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);
        }

        public async Task<Booking> CreateBookingAsync(Booking booking)
        {
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            return booking;
        }

        public async Task UpdateBookingAsync(Booking booking)
        {
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> BookingExistsAsync(int bookingId)
        {
            return await _context.Bookings.AnyAsync(b => b.BookingId == bookingId);
        }

        public async Task<bool> IsBookingCodeUniqueAsync(string bookingCode, int? excludeBookingId = null)
        {
            var query = _context.Bookings.Where(b => b.BookingCode == bookingCode);
            
            if (excludeBookingId.HasValue)
            {
                query = query.Where(b => b.BookingId != excludeBookingId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<List<Booking>> GetByTechnicianAndDateAsync(int technicianId, DateOnly date)
        {
            return await _context.Bookings
                .Include(b => b.Customer).ThenInclude(c => c.User)
                .Include(b => b.Vehicle)
                .Include(b => b.Center)
                .Include(b => b.Slot)
                .Include(b => b.Service)
                .Include(b => b.WorkOrders)
                .Where(b => b.TechnicianId == technicianId && b.BookingDate == date)
                .OrderBy(b => b.Slot.SlotTime)
                .ToListAsync();
        }

        public async Task<List<Booking>> GetAllForAutoCancelAsync()
        {
            // Giảm include để tránh lỗi đọc giá trị NULL ở chuỗi bắt buộc từ các bảng liên quan
            return await _context.Bookings
                .Select(b => new Booking
                {
                    BookingId = b.BookingId,
                    BookingCode = b.BookingCode,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                })
                .ToListAsync();
        }

        // BookingServices removed in single-service model
    }
}
