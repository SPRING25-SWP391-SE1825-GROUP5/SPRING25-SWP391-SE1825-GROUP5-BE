using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreatePromotionRequest
    {
        [Required(ErrorMessage = "Mã khuyến mãi là bắt buộc")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Mã khuyến mãi phải từ 3 đến 50 ký tự")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Mô tả phải từ 10 đến 500 ký tự")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Giá trị giảm giá là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị giảm giá phải lớn hơn 0")]
        public decimal DiscountValue { get; set; }

        [Required(ErrorMessage = "Loại giảm giá là bắt buộc")]
        [RegularExpression("^(PERCENTAGE|FIXED)$", ErrorMessage = "Loại giảm giá phải là PERCENTAGE hoặc FIXED")]
        public string DiscountType { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số tiền đơn hàng tối thiểu phải lớn hơn hoặc bằng 0")]
        public decimal? MinOrderAmount { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        [DataType(DataType.Date, ErrorMessage = "Ngày bắt đầu không đúng định dạng YYYY-MM-DD")]
        public DateOnly StartDate { get; set; }

        [DataType(DataType.Date, ErrorMessage = "Ngày kết thúc không đúng định dạng YYYY-MM-DD")]
        public DateOnly? EndDate { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giảm giá tối đa phải lớn hơn hoặc bằng 0")]
        public decimal? MaxDiscount { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [RegularExpression("^(ACTIVE|INACTIVE|EXPIRED)$", ErrorMessage = "Trạng thái phải là ACTIVE, INACTIVE hoặc EXPIRED")]
        public string Status { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Giới hạn sử dụng phải lớn hơn 0")]
        public int? UsageLimit { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Giới hạn người dùng phải lớn hơn 0")]
        public int? UserLimit { get; set; }

        [Required(ErrorMessage = "Loại khuyến mãi là bắt buộc")]
        [RegularExpression("^(GENERAL|FIRST_TIME|BIRTHDAY|LOYALTY)$", ErrorMessage = "Loại khuyến mãi phải là GENERAL, FIRST_TIME, BIRTHDAY hoặc LOYALTY")]
        public string PromotionType { get; set; }

        [Required(ErrorMessage = "Áp dụng cho là bắt buộc")]
        [RegularExpression("^(ALL|SERVICE|PRODUCT|BOOKING)$", ErrorMessage = "Áp dụng cho phải là ALL, SERVICE, PRODUCT hoặc BOOKING")]
        public string ApplyFor { get; set; }
    }
}
