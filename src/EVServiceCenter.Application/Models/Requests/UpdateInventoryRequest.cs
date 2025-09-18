using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class UpdateInventoryRequest
    {
        [Required(ErrorMessage = "Số lượng tồn kho hiện tại là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải lớn hơn hoặc bằng 0")]
        public int CurrentStock { get; set; }

        [Required(ErrorMessage = "Số lượng tồn kho tối thiểu là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho tối thiểu phải lớn hơn hoặc bằng 0")]
        public int MinimumStock { get; set; }
    }
}
