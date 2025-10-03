using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IPromotionRepository
    {
        Task<List<Promotion>> GetAllPromotionsAsync();
        Task<Promotion> GetPromotionByIdAsync(int promotionId);
        Task<Promotion> GetPromotionByCodeAsync(string code);
        Task<Promotion> CreatePromotionAsync(Promotion promotion);
        Task UpdatePromotionAsync(Promotion promotion);
        Task DeletePromotionAsync(int promotionId);
        Task<bool> IsPromotionCodeUniqueAsync(string code, int? excludePromotionId = null);
        Task<bool> PromotionExistsAsync(int promotionId);
        Task<List<UserPromotion>> GetUserPromotionsByCustomerAsync(int customerId);
        Task<List<UserPromotion>> GetUserPromotionsByPromotionAsync(int promotionId);
        Task<List<UserPromotion>> GetUserPromotionsByInvoiceAsync(int invoiceId);
        Task<UserPromotion> CreateUserPromotionAsync(UserPromotion userPromotion);
        Task<bool> DeleteUserPromotionByInvoiceAndCodeAsync(int invoiceId, string promotionCode);
        // Booking-based
        Task<List<UserPromotion>> GetUserPromotionsByBookingAsync(int bookingId);
        Task<bool> DeleteUserPromotionByBookingAndCodeAsync(int bookingId, string promotionCode);
        // Order-based
        Task<List<UserPromotion>> GetUserPromotionsByOrderAsync(int orderId);
        Task<bool> DeleteUserPromotionByOrderAndCodeAsync(int orderId, string promotionCode);
    }
}
