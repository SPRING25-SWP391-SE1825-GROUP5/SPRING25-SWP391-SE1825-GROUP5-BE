using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreatePromotionRequest
    {
        [Required(ErrorMessage = "Mã khuyến mãi là bắt buộc")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Mã khuyến mãi phải từ 3 đến 50 ký tự")]
        public required string Code { get; set; }

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Mô tả phải từ 10 đến 500 ký tự")]
        public required string Description { get; set; }

        [Required(ErrorMessage = "Giá trị giảm giá là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị giảm giá phải là số lớn hơn 0")]
        public decimal DiscountValue { get; set; }

        [Required(ErrorMessage = "Loại giảm giá là bắt buộc")]
        [RegularExpression("^(PERCENT|FIXED)$", ErrorMessage = "Loại giảm giá phải là PERCENT hoặc FIXED")]
        public required string DiscountType { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số tiền đơn hàng tối thiểu phải là số lớn hơn hoặc bằng 0")]
        public decimal? MinOrderAmount { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        [DataType(DataType.Date, ErrorMessage = "Ngày bắt đầu không đúng định dạng YYYY-MM-DD")]
        public DateOnly StartDate { get; set; }

        [DataType(DataType.Date, ErrorMessage = "Ngày kết thúc không đúng định dạng YYYY-MM-DD")]
        public DateOnly? EndDate { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giảm giá tối đa phải là số lớn hơn hoặc bằng 0")]
        public decimal? MaxDiscount { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [RegularExpression("^(ACTIVE|CANCELLED|EXPIRED)$", ErrorMessage = "Trạng thái phải là ACTIVE, CANCELLED hoặc EXPIRED")]
        public required string Status { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Giới hạn sử dụng phải là số nguyên lớn hơn 0")]
        public int? UsageLimit { get; set; }

        
        
        
    }
}
