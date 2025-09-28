using System;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductReviewController : ControllerBase
{
    private readonly IProductReviewService _productReviewService;

    public ProductReviewController(IProductReviewService productReviewService)
    {
        _productReviewService = productReviewService;
    }

    /// <summary>
    /// Lấy đánh giá của sản phẩm
    /// </summary>
    [HttpGet("part/{partId}")]
    public async Task<IActionResult> GetByPartId(int partId)
    {
        try
        {
            var reviews = await _productReviewService.GetByPartIdAsync(partId);
            return Ok(new { success = true, data = reviews });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy đánh giá của khách hàng
    /// </summary>
    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> GetByCustomerId(int customerId)
    {
        try
        {
            var reviews = await _productReviewService.GetByCustomerIdAsync(customerId);
            return Ok(new { success = true, data = reviews });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy đánh giá của đơn hàng
    /// </summary>
    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetByOrderId(int orderId)
    {
        try
        {
            var reviews = await _productReviewService.GetByOrderIdAsync(orderId);
            return Ok(new { success = true, data = reviews });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết đánh giá
    /// </summary>
    [HttpGet("{reviewId}")]
    public async Task<IActionResult> GetById(int reviewId)
    {
        try
        {
            var review = await _productReviewService.GetByIdAsync(reviewId);
            if (review == null)
                return NotFound(new { success = false, message = "Đánh giá không tồn tại" });

            return Ok(new { success = true, data = review });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Tạo đánh giá mới
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateReview([FromBody] CreateProductReviewRequest request)
    {
        try
        {
            var review = await _productReviewService.CreateReviewAsync(request);
            return Ok(new { success = true, data = review, message = "Đã tạo đánh giá thành công" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi tạo đánh giá" });
        }
    }

    /// <summary>
    /// Cập nhật đánh giá
    /// </summary>
    [HttpPut("{reviewId}")]
    public async Task<IActionResult> UpdateReview(int reviewId, [FromBody] UpdateProductReviewRequest request)
    {
        try
        {
            var review = await _productReviewService.UpdateReviewAsync(reviewId, request);
            return Ok(new { success = true, data = review, message = "Đã cập nhật đánh giá" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi cập nhật đánh giá" });
        }
    }

    /// <summary>
    /// Xóa đánh giá
    /// </summary>
    [HttpDelete("{reviewId}")]
    public async Task<IActionResult> DeleteReview(int reviewId)
    {
        try
        {
            await _productReviewService.DeleteReviewAsync(reviewId);
            return Ok(new { success = true, message = "Đã xóa đánh giá" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi xóa đánh giá" });
        }
    }
}
