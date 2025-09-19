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

        [Required(ErrorMessage = "ID model xe là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "ID model xe phải là số nguyên dương")]
        public int ModelId { get; set; }

        // Thông tin model xe (nếu tạo mới)
        [Required(ErrorMessage = "Thương hiệu xe là bắt buộc")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Thương hiệu xe phải từ 2 đến 100 ký tự")]
        public string ModelBrand { get; set; }

        [Required(ErrorMessage = "Tên model xe là bắt buộc")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên model xe phải từ 2 đến 100 ký tự")]
        public string ModelName { get; set; }

        [Range(1900, 2030, ErrorMessage = "Năm sản xuất phải từ 1900 đến 2030")]
        public int? ModelYear { get; set; }

        [Required(ErrorMessage = "Dung lượng pin là bắt buộc")]
        [Range(0, 1000, ErrorMessage = "Dung lượng pin phải là số từ 0 đến 1000 kWh")]
        public decimal BatteryCapacity { get; set; }

        [Required(ErrorMessage = "Tầm hoạt động là bắt buộc")]
        [Range(0, 1000, ErrorMessage = "Tầm hoạt động phải là số từ 0 đến 1000 km")]
        public int Range { get; set; }

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
    }
}
