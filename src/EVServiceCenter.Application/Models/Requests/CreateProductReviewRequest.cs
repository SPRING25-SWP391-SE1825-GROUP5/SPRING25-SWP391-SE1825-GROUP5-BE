using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class CreateProductReviewRequest
{
    [Required]
    public int PartId { get; set; }

    [Required]
    public int CustomerId { get; set; }

    public int? OrderId { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
    public int Rating { get; set; }

    [MaxLength(1000, ErrorMessage = "Bình luận không được quá 1000 ký tự")]
    public string? Comment { get; set; }
}
