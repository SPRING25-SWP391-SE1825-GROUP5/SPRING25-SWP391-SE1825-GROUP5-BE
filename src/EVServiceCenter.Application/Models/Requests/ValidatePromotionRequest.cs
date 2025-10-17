using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class ValidatePromotionRequest
    {
        [Required(ErrorMessage = "Mã khuyến mãi là bắt buộc")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Mã khuyến mãi phải từ 3 đến 50 ký tự")]
        public required string Code { get; set; }

        [Required(ErrorMessage = "ID khách hàng là bắt buộc")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Số tiền đơn hàng là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền đơn hàng phải lớn hơn 0")]
        public decimal OrderAmount { get; set; }

        [StringLength(50, ErrorMessage = "Loại đơn hàng không được vượt quá 50 ký tự")]
        public required string OrderType { get; set; } = "ALL";
    }
}
