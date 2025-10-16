using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class UpdateVehicleRequest
    {
        [Required(ErrorMessage = "Màu sắc là bắt buộc")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Màu sắc phải từ 2 đến 50 ký tự")]
        public required string Color { get; set; }

        [Required(ErrorMessage = "Số km hiện tại là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Số km hiện tại phải là số nguyên lớn hơn hoặc bằng 0")]
        public int CurrentMileage { get; set; }

        public DateOnly? LastServiceDate { get; set; }

        [Range(typeof(DateOnly), "1900-01-01", "2100-12-31", ErrorMessage = "Ngày mua xe phải từ năm 1900 đến năm 2100")]
        public DateOnly? PurchaseDate { get; set; }
    }
}
