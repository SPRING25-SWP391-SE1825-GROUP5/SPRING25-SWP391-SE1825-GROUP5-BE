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

        // GetBookingByCodeAsync removed: BookingCode dropped

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

        // IsBookingCodeUniqueAsync removed

        public async Task<List<Booking>> GetByTechnicianAndDateAsync(int technicianId, DateOnly date)
        {
            return await _context.Bookings
                .Include(b => b.Customer).ThenInclude(c => c.User)
                .Include(b => b.Vehicle)
                .Include(b => b.Center)
                .Include(b => b.Slot)
                .Include(b => b.Service)
                .Include(b => b.WorkOrders)
                .Where(b => b.CreatedAt.Date == date.ToDateTime(TimeOnly.MinValue).Date)
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
                    Status = b.Status,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                })
                .ToListAsync();
        }

        // BookingServices removed in single-service model

        public async Task<List<Booking>> GetBookingsByCustomerIdAsync(int customerId, int page = 1, int pageSize = 10, 
            string? status = null, DateTime? fromDate = null, DateTime? toDate = null, 
            string sortBy = "createdAt", string sortOrder = "desc")
        {
            var query = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Vehicle)
                .ThenInclude(v => v.VehicleModel)
                .Include(b => b.Center)
                .Include(b => b.Slot)
                .Include(b => b.Service)

                .Where(b => b.CustomerId == customerId);

            // Apply filters
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(b => b.Status == status);
            }

            if (fromDate.HasValue)
                query = query.Where(b => b.CreatedAt >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(b => b.CreatedAt <= toDate.Value);

            // Apply sorting
            switch (sortBy.ToLower())
            {
                case "bookingdate":
                case "createdat":
                    query = sortOrder.ToLower() == "asc" 
                        ? query.OrderBy(b => b.CreatedAt)
                        : query.OrderByDescending(b => b.CreatedAt);
                    break;
                case "totalcost":
                    query = sortOrder.ToLower() == "asc" 
                        ? query.OrderBy(b => b.TotalCost)
                        : query.OrderByDescending(b => b.TotalCost);
                    break;
                default:
                    query = query.OrderByDescending(b => b.CreatedAt);
                    break;
            }

            // Apply pagination
            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountBookingsByCustomerIdAsync(int customerId, string? status = null, 
            DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Bookings.Where(b => b.CustomerId == customerId);

            // Apply filters
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(b => b.Status == status);
            }

            if (fromDate.HasValue)
                query = query.Where(b => b.CreatedAt >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(b => b.CreatedAt <= toDate.Value);

            return await query.CountAsync();
        }

        public async Task<Booking?> GetBookingWithDetailsByIdAsync(int bookingId)
        {
            return await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Vehicle)
                .ThenInclude(v => v.VehicleModel)
                .Include(b => b.Center)
                .Include(b => b.Slot)
                .Include(b => b.Service)

                .Include(b => b.WorkOrders)
                .ThenInclude(wo => wo.WorkOrderParts)
                .ThenInclude(wop => wop.Part)
                .Include(b => b.WorkOrders)
                .ThenInclude(wo => wo.Invoices)
                .ThenInclude(i => i.Payments)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }
    }
}
