using System;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AuthenticatedUser")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private readonly PaymentService _paymentService;

    public CartController(ICartService cartService, IOrderService orderService, PaymentService paymentService)
    {
        _cartService = cartService;
        _orderService = orderService;
        _paymentService = paymentService;
    }

    [HttpGet("customer/{customerId:int}")]
    public async Task<IActionResult> GetOrCreateCart(int customerId)
    {
        try
        {
            var cart = await _cartService.GetOrCreateCartAsync(customerId);
            return Ok(new { success = true, data = cart });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("customer/{customerId:int}/items")]
    public async Task<IActionResult> GetItems(int customerId)
    {
        try
        {
            var items = await _cartService.GetCartItemsAsync(customerId);
            return Ok(new { success = true, data = items });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    public class AddCartItemRequest { public int PartId { get; set; } public int Quantity { get; set; } }

    [HttpPost("customer/{customerId:int}/items")]
    public async Task<IActionResult> AddItem(int customerId, [FromBody] AddCartItemRequest request)
    {
        try
        {
            var cart = await _cartService.AddItemToCartAsync(customerId, request.PartId, request.Quantity);
            return Ok(new { success = true, data = cart, message = "Đã thêm vào giỏ" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    public class UpdateCartItemRequest { public int Quantity { get; set; } }

    [HttpPut("customer/{customerId:int}/items/{partId:int}")]
    public async Task<IActionResult> UpdateItem(int customerId, int partId, [FromBody] UpdateCartItemRequest request)
    {
        try
        {
            var cart = await _cartService.UpdateCartItemQuantityAsync(customerId, partId, request.Quantity);
            return Ok(new { success = true, data = cart, message = "Đã cập nhật số lượng" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("customer/{customerId:int}/items/{partId:int}")]
    public async Task<IActionResult> RemoveItem(int customerId, int partId)
    {
        try
        {
            var cart = await _cartService.RemoveCartItemAsync(customerId, partId);
            return Ok(new { success = true, data = cart, message = "Đã xóa sản phẩm khỏi giỏ" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("customer/{customerId:int}/items")]
    public async Task<IActionResult> ClearItems(int customerId)
    {
        try
        {
            var cart = await _cartService.ClearCartAsync(customerId);
            return Ok(new { success = true, data = cart, message = "Đã xóa toàn bộ giỏ hàng" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("customer/{customerId:int}/checkout")]
    public async Task<IActionResult> Checkout(int customerId)
    {
        try
        {
            var order = await _orderService.CheckoutCartFromRedisAsync(customerId);

            string? checkoutUrl = null;
            try
            {
                checkoutUrl = await _paymentService.CreateOrderPaymentLinkAsync(order.OrderId);
            }
            catch
            {
            }
            return Ok(new { success = true, data = order, checkoutUrl });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
