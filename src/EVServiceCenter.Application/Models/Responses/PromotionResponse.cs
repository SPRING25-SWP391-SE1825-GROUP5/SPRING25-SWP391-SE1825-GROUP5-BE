using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class PromotionResponse
    {
        public int PromotionId { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public decimal DiscountValue { get; set; }
        public string DiscountType { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public decimal? MaxDiscount { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? UsageLimit { get; set; }
        public int UsageCount { get; set; }
        public int? UserLimit { get; set; }
        public string PromotionType { get; set; }
        public string ApplyFor { get; set; }
        
        // Calculated fields
        public bool IsActive { get; set; }
        public bool IsExpired { get; set; }
        public bool IsUsageLimitReached { get; set; }
        public int RemainingUsage { get; set; }
    }
}
