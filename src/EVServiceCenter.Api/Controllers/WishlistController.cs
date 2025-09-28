using System;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlistService;

    public WishlistController(IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    /// <summary>
    /// Lấy danh sách yêu thích của khách hàng
    /// </summary>
    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> GetByCustomerId(int customerId)
    {
        try
        {
            var wishlists = await _wishlistService.GetByCustomerIdAsync(customerId);
            return Ok(new { success = true, data = wishlists });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết mục yêu thích
    /// </summary>
    [HttpGet("{wishlistId}")]
    public async Task<IActionResult> GetById(int wishlistId)
    {
        try
        {
            var wishlist = await _wishlistService.GetByIdAsync(wishlistId);
            if (wishlist == null)
                return NotFound(new { success = false, message = "Mục yêu thích không tồn tại" });

            return Ok(new { success = true, data = wishlist });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Thêm sản phẩm vào danh sách yêu thích
    /// </summary>
    [HttpPost("add")]
    public async Task<IActionResult> AddToWishlist([FromBody] AddToWishlistRequest request)
    {
        try
        {
            var wishlist = await _wishlistService.AddToWishlistAsync(request);
            return Ok(new { success = true, data = wishlist, message = "Đã thêm sản phẩm vào danh sách yêu thích" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi thêm sản phẩm vào danh sách yêu thích" });
        }
    }

    /// <summary>
    /// Xóa sản phẩm khỏi danh sách yêu thích
    /// </summary>
    [HttpDelete("{wishlistId}")]
    public async Task<IActionResult> DeleteFromWishlist(int wishlistId)
    {
        try
        {
            await _wishlistService.DeleteFromWishlistAsync(wishlistId);
            return Ok(new { success = true, message = "Đã xóa sản phẩm khỏi danh sách yêu thích" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi xóa sản phẩm khỏi danh sách yêu thích" });
        }
    }

    /// <summary>
    /// Xóa sản phẩm khỏi danh sách yêu thích theo khách hàng và sản phẩm
    /// </summary>
    [HttpDelete("customer/{customerId}/part/{partId}")]
    public async Task<IActionResult> DeleteByCustomerAndPart(int customerId, int partId)
    {
        try
        {
            await _wishlistService.DeleteByCustomerAndPartAsync(customerId, partId);
            return Ok(new { success = true, message = "Đã xóa sản phẩm khỏi danh sách yêu thích" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi xóa sản phẩm khỏi danh sách yêu thích" });
        }
    }
}
