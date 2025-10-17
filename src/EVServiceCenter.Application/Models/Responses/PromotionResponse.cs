using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class PromotionResponse
    {
        public int PromotionId { get; set; }
        public required string Code { get; set; }
        public required string Description { get; set; }
        public decimal DiscountValue { get; set; }
        public required string DiscountType { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public decimal? MaxDiscount { get; set; }
        public required string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? UsageLimit { get; set; }
        public int UsageCount { get; set; }
        
        
        
        
        // Calculated fields
        public bool IsActive { get; set; }
        public bool IsExpired { get; set; }
        public bool IsUsageLimitReached { get; set; }
        public int RemainingUsage { get; set; }
    }
}
