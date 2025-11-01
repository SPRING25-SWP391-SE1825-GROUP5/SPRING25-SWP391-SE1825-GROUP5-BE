using System;
using System.Linq;
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
    private readonly IOrderService _orderService;
    private readonly PaymentService _paymentService;

    public CartController(IOrderService orderService, PaymentService paymentService)
    {
        _orderService = orderService;
        _paymentService = paymentService;
    }

    // GET /api/Cart/customer/{customerId}
    [HttpGet("customer/{customerId:int}")]
    public async Task<IActionResult> GetOrCreateCart(int customerId)
    {
        try
        {
            var cart = await _orderService.GetOrCreateCartAsync(customerId);
            return Ok(new { success = true, data = cart });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // GET /api/Cart/{cartId}/items
    [HttpGet("{cartId:int}/items")]
    public async Task<IActionResult> GetItems(int cartId)
    {
        try
        {
            var items = await _orderService.GetCartItemsAsync(cartId);
            return Ok(new { success = true, data = items });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    public class AddCartItemRequest { public int PartId { get; set; } public int Quantity { get; set; } }
    // POST /api/Cart/{cartId}/items
    [HttpPost("{cartId:int}/items")]
    public async Task<IActionResult> AddItem(int cartId, [FromBody] AddCartItemRequest request)
    {
        try
        {
            var cart = await _orderService.AddItemToCartAsync(cartId, request.PartId, request.Quantity);
            return Ok(new { success = true, data = cart, message = "Đã thêm vào giỏ" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    public class UpdateCartItemRequest { public int Quantity { get; set; } }
    // PUT /api/Cart/{cartId}/items/{orderItemId}
    [HttpPut("{cartId:int}/items/{orderItemId:int}")]
    public async Task<IActionResult> UpdateItem(int cartId, int orderItemId, [FromBody] UpdateCartItemRequest request)
    {
        try
        {
            var cart = await _orderService.UpdateCartItemQuantityAsync(cartId, orderItemId, request.Quantity);
            return Ok(new { success = true, data = cart, message = "Đã cập nhật số lượng" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // DELETE /api/Cart/{cartId}/items/{orderItemId}
    [HttpDelete("{cartId:int}/items/{orderItemId:int}")]
    public async Task<IActionResult> RemoveItem(int cartId, int orderItemId)
    {
        try
        {
            var cart = await _orderService.RemoveCartItemAsync(cartId, orderItemId);
            return Ok(new { success = true, data = cart, message = "Đã xóa sản phẩm khỏi giỏ" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // DELETE /api/Cart/{cartId}/items
    [HttpDelete("{cartId:int}/items")]
    public async Task<IActionResult> ClearItems(int cartId)
    {
        try
        {
            var cart = await _orderService.ClearCartAsync(cartId);
            return Ok(new { success = true, data = cart, message = "Đã xóa toàn bộ giỏ hàng" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // POST /api/Cart/{cartId}/checkout
    [HttpPost("{cartId:int}/checkout")]
    public async Task<IActionResult> Checkout(int cartId)
    {
        try
        {
            var order = await _orderService.CheckoutCartAsync(cartId);
            string? checkoutUrl = null;
            try
            {
                checkoutUrl = await _paymentService.CreateOrderPaymentLinkAsync(order.OrderId);
            }
            catch
            {
                // If payment link fails, still return order; frontend may retry payment later
            }
            return Ok(new { success = true, data = order, checkoutUrl });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
