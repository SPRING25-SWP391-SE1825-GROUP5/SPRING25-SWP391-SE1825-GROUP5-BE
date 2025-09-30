using System;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AuthenticatedUser")]
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
    [HttpPost("customer/{customerId}/cart/items")]
    public async Task<IActionResult> AddToCart([FromRoute] int customerId, [FromBody] AddToCartRequest request)
    {
        try
        {
            if (!ModelState.IsValid || request == null)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }
            request.CustomerId = customerId;
            var cartItem = await _shoppingCartService.AddToCartAsync(request);
            return Ok(new { success = true, data = cartItem, message = "Đã thêm sản phẩm vào giỏ hàng" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật số lượng sản phẩm trong giỏ hàng
    /// </summary>
    [HttpPut("customer/{customerId}/cart/items/{partId}")]
    public async Task<IActionResult> UpdateCartItem([FromRoute] int customerId, [FromRoute] int partId, [FromBody] UpdateCartItemRequest request)
    {
        try
        {
            var cartItem = await _shoppingCartService.UpdateCartItemByCustomerAndPartAsync(customerId, partId, request.Quantity);
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
    [HttpDelete("customer/{customerId}/cart/items/{partId}")]
    public async Task<IActionResult> DeleteCartItem([FromRoute] int customerId, [FromRoute] int partId)
    {
        try
        {
            await _shoppingCartService.DeleteCartItemByCustomerAndPartAsync(customerId, partId);
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
    [HttpDelete("customer/{customerId}/cart")]
    public async Task<IActionResult> ClearCart([FromRoute] int customerId)
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
