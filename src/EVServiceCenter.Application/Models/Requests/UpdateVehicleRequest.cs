using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EVServiceCenter.Application.Models.Requests
{
    public class UpdateVehicleRequest
    {
        [Required(ErrorMessage = "ID model xe là bắt buộc")]
        public int ModelId { get; set; }

        [Required(ErrorMessage = "VIN là bắt buộc")]
        [StringLength(17, MinimumLength = 17, ErrorMessage = "VIN phải có đúng 17 ký tự")]
        public string Vin { get; set; }

        [Required(ErrorMessage = "Biển số xe là bắt buộc")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "Biển số xe phải từ 5 đến 20 ký tự")]
        public string LicensePlate { get; set; }

        [Required(ErrorMessage = "Màu sắc là bắt buộc")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Màu sắc phải từ 2 đến 50 ký tự")]
        public string Color { get; set; }

        [Required(ErrorMessage = "Số km hiện tại là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Số km hiện tại phải lớn hơn hoặc bằng 0")]
        public int CurrentMileage { get; set; }

        public DateOnly? LastServiceDate { get; set; }

        public DateOnly? NextServiceDue { get; set; }
    }
}
