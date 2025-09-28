using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class UpdateProductReviewRequest
{
    [Required]
    [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
    public int Rating { get; set; }

    [MaxLength(1000, ErrorMessage = "Bình luận không được quá 1000 ký tự")]
    public string? Comment { get; set; }
}
