using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Service;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Xunit;

namespace EVServiceCenter.Tests.Unit.Application.Services;

public class PromotionServiceTests
{
    private class InMemoryPromotionRepository : IPromotionRepository
    {
        public readonly List<UserPromotion> UserPromotions = new();
        public readonly List<Promotion> Promotions = new();

        public Task<Promotion> CreatePromotionAsync(Promotion promotion)
        {
            promotion.PromotionId = Promotions.Count == 0 ? 1 : Promotions.Max(x => x.PromotionId) + 1;
            Promotions.Add(promotion);
            return Task.FromResult(promotion);
        }

        public Task<UserPromotion> CreateUserPromotionAsync(UserPromotion userPromotion)
        {
            userPromotion.UserPromotionId = UserPromotions.Count == 0 ? 1 : UserPromotions.Max(x => x.UserPromotionId) + 1;
            UserPromotions.Add(userPromotion);
            return Task.FromResult(userPromotion);
        }

        public Task DeletePromotionAsync(int promotionId) => Task.CompletedTask;

        public Task<bool> DeleteUserPromotionByBookingAndCodeAsync(int bookingId, string promotionCode)
        {
            var up = UserPromotions.FirstOrDefault(x => x.BookingId == bookingId && x.Promotion?.Code == promotionCode);
            if (up == null) return Task.FromResult(false);
            UserPromotions.Remove(up);
            return Task.FromResult(true);
        }

        public Task<bool> DeleteUserPromotionByOrderAndCodeAsync(int orderId, string promotionCode) => Task.FromResult(false);

        public Task<List<Promotion>> GetAllPromotionsAsync() => Task.FromResult(Promotions.ToList());

        public Task<Promotion> GetPromotionByCodeAsync(string code) => Task.FromResult(Promotions.FirstOrDefault(p => p.Code == code));

        public Task<Promotion> GetPromotionByIdAsync(int promotionId) => Task.FromResult(Promotions.FirstOrDefault(p => p.PromotionId == promotionId));

        public Task<List<UserPromotion>> GetUserPromotionsByBookingAsync(int bookingId)
        {
            return Task.FromResult(UserPromotions.Where(x => x.BookingId == bookingId).ToList());
        }

        public Task<List<UserPromotion>> GetUserPromotionsByCustomerAsync(int customerId)
        {
            return Task.FromResult(UserPromotions.Where(x => x.CustomerId == customerId).ToList());
        }

        public Task<List<UserPromotion>> GetUserPromotionsByOrderAsync(int orderId)
        {
            return Task.FromResult(UserPromotions.Where(x => x.OrderId == orderId).ToList());
        }

        public Task<bool> IsPromotionCodeUniqueAsync(string code, int? excludePromotionId = null) => Task.FromResult(true);

        public Task UpdatePromotionAsync(Promotion promotion)
        {
            var idx = Promotions.FindIndex(x => x.PromotionId == promotion.PromotionId);
            if (idx >= 0) Promotions[idx] = promotion;
            return Task.CompletedTask;
        }

        public Task UpdateUserPromotionAsync(UserPromotion userPromotion)
        {
            var idx = UserPromotions.FindIndex(x => x.UserPromotionId == userPromotion.UserPromotionId);
            if (idx >= 0) UserPromotions[idx] = userPromotion;
            return Task.CompletedTask;
        }

        public Task<List<UserPromotion>> GetUserPromotionsByPromotionAsync(int promotionId) => Task.FromResult(new List<UserPromotion>());
        public Task<UserPromotion> GetUserPromotionAsync(int userPromotionId) => Task.FromResult<UserPromotion>(null);
        public Task<List<UserPromotion>> GetUserPromotionsAsync() => Task.FromResult(UserPromotions.ToList());
        public Task<bool> PromotionExistsAsync(int promotionId) => Task.FromResult(Promotions.Any(p => p.PromotionId == promotionId));
        public Task<Promotion> CreateOrUpdatePromotionAsync(Promotion promotion) => Task.FromResult(promotion);
        public Task<bool> DeleteUserPromotionByOrderAndCodeAsync(int orderId, string promotionCode, int? customerId = null) => Task.FromResult(false);
        public Task<bool> DeleteUserPromotionByBookingAndCodeAsync(int bookingId, string promotionCode, int? customerId = null) => Task.FromResult(false);
    }

    [Fact]
    public async Task MarkUsedByBooking_Should_Update_Status_And_Increment_UsageCount()
    {
        var repo = new InMemoryPromotionRepository();
        var promo = new Promotion { PromotionId = 1, Code = "WELCOME", Status = "ACTIVE", UsageCount = 0 };
        repo.Promotions.Add(promo);
        repo.UserPromotions.Add(new UserPromotion { UserPromotionId = 1, BookingId = 100, PromotionId = 1, CustomerId = 7, Status = "APPLIED", UsedAt = DateTime.UtcNow.AddMinutes(-5) });

        var svc = new PromotionService(repo, null);
        var updated = await svc.MarkUsedByBookingAsync(100);

        Assert.Equal(1, updated);
        Assert.Equal("USED", repo.UserPromotions.Single().Status);
        Assert.Equal(1, repo.Promotions.Single().UsageCount);
    }
}


