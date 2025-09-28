using System;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShoppingCartController : ControllerBase
{
    private readonly IShoppingCartService _shoppingCartService;

    public ShoppingCartController(IShoppingCartService shoppingCartService)
    {
        _shoppingCartService = shoppingCartService;
    }

    /// <summary>
    /// Lấy giỏ hàng của khách hàng
    /// </summary>
    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> GetByCustomerId(int customerId)
    {
        try
        {
            var cartItems = await _shoppingCartService.GetByCustomerIdAsync(customerId);
            return Ok(new { success = true, data = cartItems });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết mục giỏ hàng
    /// </summary>
    [HttpGet("{cartId}")]
    public async Task<IActionResult> GetById(int cartId)
    {
        try
        {
            var cartItem = await _shoppingCartService.GetByIdAsync(cartId);
            if (cartItem == null)
                return NotFound(new { success = false, message = "Mục giỏ hàng không tồn tại" });

            return Ok(new { success = true, data = cartItem });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Thêm sản phẩm vào giỏ hàng
    /// </summary>
    [HttpPost("add")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        try
        {
            var cartItem = await _shoppingCartService.AddToCartAsync(request);
            return Ok(new { success = true, data = cartItem, message = "Đã thêm sản phẩm vào giỏ hàng" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi thêm sản phẩm vào giỏ hàng" });
        }
    }

    /// <summary>
    /// Cập nhật số lượng sản phẩm trong giỏ hàng
    /// </summary>
    [HttpPut("{cartId}")]
    public async Task<IActionResult> UpdateCartItem(int cartId, [FromBody] UpdateCartItemRequest request)
    {
        try
        {
            var cartItem = await _shoppingCartService.UpdateCartItemAsync(cartId, request);
            return Ok(new { success = true, data = cartItem, message = "Đã cập nhật giỏ hàng" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi cập nhật giỏ hàng" });
        }
    }

    /// <summary>
    /// Xóa sản phẩm khỏi giỏ hàng
    /// </summary>
    [HttpDelete("{cartId}")]
    public async Task<IActionResult> DeleteCartItem(int cartId)
    {
        try
        {
            await _shoppingCartService.DeleteCartItemAsync(cartId);
            return Ok(new { success = true, message = "Đã xóa sản phẩm khỏi giỏ hàng" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi xóa sản phẩm khỏi giỏ hàng" });
        }
    }

    /// <summary>
    /// Xóa toàn bộ giỏ hàng
    /// </summary>
    [HttpDelete("customer/{customerId}")]
    public async Task<IActionResult> ClearCart(int customerId)
    {
        try
        {
            await _shoppingCartService.ClearCartAsync(customerId);
            return Ok(new { success = true, message = "Đã xóa toàn bộ giỏ hàng" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi xóa giỏ hàng" });
        }
    }
}
