using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateVehicleRequest
    {
        [Required(ErrorMessage = "ID khách hàng là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ID khách hàng phải là số nguyên dương")]
        public int CustomerId { get; set; }


        [Required(ErrorMessage = "VIN là bắt buộc")]
        [StringLength(17, MinimumLength = 17, ErrorMessage = "VIN phải có đúng 17 ký tự")]
        public string Vin { get; set; }

        [Required(ErrorMessage = "Biển số xe là bắt buộc")]
        [RegularExpression(@"^\d{2}-[A-Z]\d\s\d{4}$", ErrorMessage = "Biển số xe máy phải theo định dạng XX-YZ ABCD (ví dụ: 29-T8 2843, 30-A1 1234)")]
        public string LicensePlate { get; set; }

        [Required(ErrorMessage = "Màu sắc là bắt buộc")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Màu sắc phải từ 2 đến 50 ký tự")]
        public string Color { get; set; }

        [Required(ErrorMessage = "Số km hiện tại là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Số km hiện tại phải là số nguyên lớn hơn hoặc bằng 0")]
        public int CurrentMileage { get; set; }

        public DateOnly? LastServiceDate { get; set; }

        public DateOnly? PurchaseDate { get; set; }
    }
}
