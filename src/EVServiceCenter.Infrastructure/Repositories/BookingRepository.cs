using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

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
            return await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Customer.User)
                .Include(b => b.Vehicle)
                .Include(b => b.Center)
                .Include(b => b.StartSlot)
                .Include(b => b.EndSlot)
                .Include(b => b.BookingServices)
                .ThenInclude(bs => bs.Service)
                .Include(b => b.BookingTimeSlots)
                .ThenInclude(bts => bts.Slot)
                .Include(b => b.BookingTimeSlots)
                .ThenInclude(bts => bts.Technician)
                .ThenInclude(t => t.User)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<Booking> GetBookingByIdAsync(int bookingId)
        {
            return await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Customer.User)
                .Include(b => b.Vehicle)
                .Include(b => b.Center)
                .Include(b => b.StartSlot)
                .Include(b => b.EndSlot)
                .Include(b => b.BookingServices)
                .ThenInclude(bs => bs.Service)
                .Include(b => b.BookingTimeSlots)
                .ThenInclude(bts => bts.Slot)
                .Include(b => b.BookingTimeSlots)
                .ThenInclude(bts => bts.Technician)
                .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }

        public async Task<Booking> GetBookingByCodeAsync(string bookingCode)
        {
            return await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Customer.User)
                .Include(b => b.Vehicle)
                .Include(b => b.Center)
                .Include(b => b.StartSlot)
                .Include(b => b.EndSlot)
                .Include(b => b.BookingServices)
                .ThenInclude(bs => bs.Service)
                .Include(b => b.BookingTimeSlots)
                .ThenInclude(bts => bts.Slot)
                .Include(b => b.BookingTimeSlots)
                .ThenInclude(bts => bts.Technician)
                .ThenInclude(t => t.User)
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

        public async Task<List<BookingService>> GetBookingServicesAsync(int bookingId)
        {
            return await _context.BookingServices
                .Include(bs => bs.Service)
                .Where(bs => bs.BookingId == bookingId)
                .ToListAsync();
        }

        public async Task<List<BookingTimeSlot>> GetBookingTimeSlotsAsync(int bookingId)
        {
            return await _context.BookingTimeSlots
                .Include(bts => bts.Slot)
                .Include(bts => bts.Technician)
                .ThenInclude(t => t.User)
                .Where(bts => bts.BookingId == bookingId)
                .OrderBy(bts => bts.SlotOrder)
                .ToListAsync();
        }

        public async Task AddBookingServicesAsync(List<BookingService> bookingServices)
        {
            _context.BookingServices.AddRange(bookingServices);
            await _context.SaveChangesAsync();
        }

        public async Task AddBookingTimeSlotsAsync(List<BookingTimeSlot> bookingTimeSlots)
        {
            _context.BookingTimeSlots.AddRange(bookingTimeSlots);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveBookingServicesAsync(int bookingId)
        {
            var bookingServices = await _context.BookingServices
                .Where(bs => bs.BookingId == bookingId)
                .ToListAsync();
            
            if (bookingServices.Any())
            {
                _context.BookingServices.RemoveRange(bookingServices);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveBookingTimeSlotsAsync(int bookingId)
        {
            var bookingTimeSlots = await _context.BookingTimeSlots
                .Where(bts => bts.BookingId == bookingId)
                .ToListAsync();
            
            if (bookingTimeSlots.Any())
            {
                _context.BookingTimeSlots.RemoveRange(bookingTimeSlots);
                await _context.SaveChangesAsync();
            }
        }
    }
}
