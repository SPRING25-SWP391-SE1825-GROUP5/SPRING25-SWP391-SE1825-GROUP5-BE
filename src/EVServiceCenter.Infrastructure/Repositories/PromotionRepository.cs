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
            // Không Include UserPromotions để tránh sinh cột InvoiceId từ shadow FK
            return await _context.Promotions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PromotionId == promotionId);
        }

        public async Task<Promotion> GetPromotionByCodeAsync(string code)
        {
            return await _context.Promotions
                .AsNoTracking()
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
            // Chỉ trả về các khuyến mãi còn hợp lệ để hiển thị cho customer
            var today = System.DateOnly.FromDateTime(System.DateTime.Today);
            var query = from up in _context.UserPromotions.AsNoTracking()
                        join p in _context.Promotions.AsNoTracking() on up.PromotionId equals p.PromotionId
                        where up.CustomerId == customerId
                              && p.Status == "ACTIVE"
                              && p.StartDate <= today
                              && (p.EndDate == null || p.EndDate >= today)
                              && (p.UsageLimit == null || p.UsageCount < p.UsageLimit)
                        orderby up.UsedAt descending
                        select new UserPromotion
                        {
                            UserPromotionId = up.UserPromotionId,
                            CustomerId = up.CustomerId,
                            PromotionId = up.PromotionId,
                            BookingId = up.BookingId,
                            OrderId = up.OrderId,
                            ServiceId = up.ServiceId,
                            UsedAt = up.UsedAt,
                            DiscountAmount = up.DiscountAmount,
                            Status = up.Status,
                            Promotion = new Promotion
                            {
                                PromotionId = p.PromotionId,
                                Code = p.Code,
                                Description = p.Description,
                                StartDate = p.StartDate,
                                EndDate = p.EndDate,
                                Status = p.Status
                            }
                        };

            return await query.ToListAsync();
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

        public async Task<UserPromotion> CreateUserPromotionAsync(UserPromotion userPromotion)
        {
            // Quy tắc mới: Mỗi (PromotionId, CustomerId) chỉ có 1 bản ghi duy nhất
            var existing = await _context.UserPromotions
                .FirstOrDefaultAsync(up => up.PromotionId == userPromotion.PromotionId && up.CustomerId == userPromotion.CustomerId);

            if (existing != null)
            {
                // Nếu đã USED thì không cho ghi đè/lưu thêm
                if (string.Equals(existing.Status, "USED", System.StringComparison.OrdinalIgnoreCase))
                {
                    return existing; // hoặc ném lỗi tùy chính sách
                }

                // Nếu gọi lưu đơn thuần (SAVED) mà đã có (SAVED/APPLIED) thì trả về hiện có, không tạo mới
                var isIncomingApply = userPromotion.OrderId != null || userPromotion.BookingId != null || userPromotion.ServiceId != null;
                if (!isIncomingApply)
                {
                    return existing; // idempotent save
                }

                // Nếu là áp dụng cho Order/Booking/Service → cập nhật chính bản ghi hiện có
                if (userPromotion.OrderId != null)
                {
                    existing.OrderId = userPromotion.OrderId;
                }
                if (userPromotion.BookingId != null)
                {
                    existing.BookingId = userPromotion.BookingId;
                }
                if (userPromotion.ServiceId != null)
                {
                    existing.ServiceId = userPromotion.ServiceId;
                }

                existing.DiscountAmount = userPromotion.DiscountAmount;
                existing.Status = "APPLIED";
                existing.UsedAt = System.DateTime.UtcNow;

                _context.UserPromotions.Update(existing);
                await _context.SaveChangesAsync();
                return existing;
            }

            // Chưa có: tạo mới theo trạng thái phù hợp
            if (userPromotion.OrderId == null && userPromotion.BookingId == null && userPromotion.ServiceId == null)
            {
                userPromotion.Status = string.IsNullOrWhiteSpace(userPromotion.Status) ? "SAVED" : userPromotion.Status;
                userPromotion.DiscountAmount = userPromotion.DiscountAmount;
                userPromotion.UsedAt = userPromotion.UsedAt == null ? System.DateTime.UtcNow : userPromotion.UsedAt;
            }
            else
            {
                userPromotion.Status = "APPLIED";
                if (userPromotion.UsedAt == null) userPromotion.UsedAt = System.DateTime.UtcNow;
            }

            _context.UserPromotions.Add(userPromotion);
            await _context.SaveChangesAsync();
            return userPromotion;
        }

        public async Task UpdateUserPromotionAsync(UserPromotion userPromotion)
        {
            _context.UserPromotions.Update(userPromotion);
            await _context.SaveChangesAsync();
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
            // Tránh để EF tự sinh truy vấn dư thừa bằng Include; join rõ ràng và project
            var query = from up in _context.UserPromotions.AsNoTracking()
                        join p in _context.Promotions.AsNoTracking() on up.PromotionId equals p.PromotionId
                        where up.OrderId == orderId
                        orderby up.UsedAt descending
                        select new UserPromotion
                        {
                            UserPromotionId = up.UserPromotionId,
                            CustomerId = up.CustomerId,
                            PromotionId = up.PromotionId,
                            BookingId = up.BookingId,
                            OrderId = up.OrderId,
                            ServiceId = up.ServiceId,
                            UsedAt = up.UsedAt,
                            DiscountAmount = up.DiscountAmount,
                            Status = up.Status,
                            Promotion = new Promotion
                            {
                                PromotionId = p.PromotionId,
                                Code = p.Code,
                                Description = p.Description
                            }
                        };

            return await query.ToListAsync();
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
