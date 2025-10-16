using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class PromotionValidationResponse
    {
        public bool IsValid { get; set; }
        public required string Message { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public required PromotionResponse Promotion { get; set; }
    }
}
