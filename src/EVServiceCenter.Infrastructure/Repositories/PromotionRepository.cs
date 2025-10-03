using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class PromotionRepository : IPromotionRepository
    {
        private readonly EVDbContext _context;

        public PromotionRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<Promotion>> GetAllPromotionsAsync()
        {
            return await _context.Promotions
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Promotion> GetPromotionByIdAsync(int promotionId)
        {
            return await _context.Promotions
                .Include(p => p.UserPromotions)
                .FirstOrDefaultAsync(p => p.PromotionId == promotionId);
        }

        public async Task<Promotion> GetPromotionByCodeAsync(string code)
        {
            return await _context.Promotions
                .Include(p => p.UserPromotions)
                .FirstOrDefaultAsync(p => p.Code == code);
        }

        public async Task<Promotion> CreatePromotionAsync(Promotion promotion)
        {
            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();
            return promotion;
        }

        public async Task UpdatePromotionAsync(Promotion promotion)
        {
            _context.Promotions.Update(promotion);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePromotionAsync(int promotionId)
        {
            var promotion = await _context.Promotions.FindAsync(promotionId);
            if (promotion != null)
            {
                _context.Promotions.Remove(promotion);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsPromotionCodeUniqueAsync(string code, int? excludePromotionId = null)
        {
            var query = _context.Promotions.Where(p => p.Code == code);
            
            if (excludePromotionId.HasValue)
            {
                query = query.Where(p => p.PromotionId != excludePromotionId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<bool> PromotionExistsAsync(int promotionId)
        {
            return await _context.Promotions.AnyAsync(p => p.PromotionId == promotionId);
        }

        public async Task<List<UserPromotion>> GetUserPromotionsByCustomerAsync(int customerId)
        {
            return await _context.UserPromotions
                .Include(up => up.Promotion)
                .Where(up => up.CustomerId == customerId)
                .OrderByDescending(up => up.UsedAt)
                .ToListAsync();
        }

        public async Task<List<UserPromotion>> GetUserPromotionsByPromotionAsync(int promotionId)
        {
            return await _context.UserPromotions
                .Include(up => up.Customer)
                .Include(up => up.Customer.User)
                .Where(up => up.PromotionId == promotionId)
                .OrderByDescending(up => up.UsedAt)
                .ToListAsync();
        }

        public async Task<List<UserPromotion>> GetUserPromotionsByInvoiceAsync(int invoiceId)
        {
            return await _context.UserPromotions
                .Include(up => up.Promotion)
                .Where(up => false)
                .OrderByDescending(up => up.UsedAt)
                .ToListAsync();
        }

        public async Task<UserPromotion> CreateUserPromotionAsync(UserPromotion userPromotion)
        {
            _context.UserPromotions.Add(userPromotion);
            await _context.SaveChangesAsync();
            return userPromotion;
        }

        public async Task<bool> DeleteUserPromotionByInvoiceAndCodeAsync(int invoiceId, string promotionCode)
        {
            var up = await _context.UserPromotions
                .Include(x => x.Promotion)
                .FirstOrDefaultAsync(x => false);
            if (up == null) return false;
            _context.UserPromotions.Remove(up);
            await _context.SaveChangesAsync();
            return true;
        }

        // Booking-based versions
        public async Task<List<UserPromotion>> GetUserPromotionsByBookingAsync(int bookingId)
        {
            return await _context.UserPromotions
                .Include(up => up.Promotion)
                .Where(up => up.BookingId == bookingId)
                .OrderByDescending(up => up.UsedAt)
                .ToListAsync();
        }

        public async Task<bool> DeleteUserPromotionByBookingAndCodeAsync(int bookingId, string promotionCode)
        {
            var up = await _context.UserPromotions
                .Include(x => x.Promotion)
                .FirstOrDefaultAsync(x => x.BookingId == bookingId && x.Promotion.Code == promotionCode);
            if (up == null) return false;
            _context.UserPromotions.Remove(up);
            await _context.SaveChangesAsync();
            return true;
        }

        // Order-based versions
        public async Task<List<UserPromotion>> GetUserPromotionsByOrderAsync(int orderId)
        {
            return await _context.UserPromotions
                .Include(up => up.Promotion)
                .Where(up => up.OrderId == orderId)
                .OrderByDescending(up => up.UsedAt)
                .ToListAsync();
        }

        public async Task<bool> DeleteUserPromotionByOrderAndCodeAsync(int orderId, string promotionCode)
        {
            var up = await _context.UserPromotions
                .Include(x => x.Promotion)
                .FirstOrDefaultAsync(x => x.OrderId == orderId && x.Promotion.Code == promotionCode);
            if (up == null) return false;
            _context.UserPromotions.Remove(up);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
