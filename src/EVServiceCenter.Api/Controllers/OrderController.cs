using System;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Service;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Application.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using EVServiceCenter.Api.Constants;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AuthenticatedUser")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly PaymentService _paymentService;
    private readonly IOrderHistoryService _orderHistoryService;
    private readonly IConfiguration _configuration;

    public OrderController(IOrderService orderService, PaymentService paymentService, IOrderHistoryService orderHistoryService, IConfiguration configuration)
    {
        _orderService = orderService;
        _paymentService = paymentService;
        _orderHistoryService = orderHistoryService;
        _configuration = configuration;
    }

    /// <summary>
    /// Lấy danh sách đơn hàng của khách hàng
    /// </summary>
    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> GetByCustomerId(int customerId)
    {
        try
        {
            var orders = await _orderService.GetByCustomerIdAsync(customerId);
            return Ok(new { success = true, data = orders });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // Removed order history/customer endpoints (per request)

    /// <summary>
    /// Danh sách item của đơn hàng
    /// </summary>
    [HttpGet("{orderId}/items")]
    public async Task<IActionResult> GetItems(int orderId)
    {
        try
        {
            var items = await _orderService.GetItemsAsync(orderId);
            return Ok(new { success = true, data = items });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi lấy danh sách item: " + ex.Message });
        }
    }

    // Removed order status history endpoint

    /// <summary>
    /// Checkout online cho Order (PayOS) - dùng ReturnUrl callback, không webhook
    /// </summary>
    [HttpPost("{orderId}/checkout/online")]
    public async Task<IActionResult> CheckoutOnline(int orderId)
    {
        try
        {
            // Tạo cancel URL riêng cho order để phân biệt với booking
            var frontendUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:5173";
            var orderCancelUrl = $"{frontendUrl}/api/payment/order/{orderId}/cancel";
            
            var url = await _paymentService.CreateOrderPaymentLinkAsync(orderId, orderCancelUrl);
            return Ok(new { 
                success = true, 
                checkoutUrl = url,
                orderId = orderId,
                message = "Tạo payment link thành công"
            });
        }
        catch (ArgumentException ex)
        {
            // Validation lỗi: orderId không hợp lệ
            return BadRequest(new { 
                success = false, 
                message = ex.Message,
                errorType = ApiConstants.ErrorTypes.ValidationError,
                orderId = orderId
            });
        }
        catch (InvalidOperationException ex)
        {
            // Business logic lỗi: order không tồn tại, đã hủy, đã thanh toán, v.v.
            var message = ex.Message;
            
            // Phân loại lỗi dựa trên message để trả về status code phù hợp
            if (message.Contains("không tồn tại") || message.Contains("Không tìm thấy"))
            {
                return NotFound(new { 
                    success = false, 
                    message = message,
                    errorType = ApiConstants.ErrorTypes.OrderNotFound,
                    orderId = orderId
                });
            }
            else if (message.Contains("đã bị hủy") || message.Contains("đã được thanh toán") || message.Contains("hóa đơn thanh toán"))
            {
                return BadRequest(new { 
                    success = false, 
                    message = message,
                    errorType = ApiConstants.ErrorTypes.OrderInvalidState,
                    orderId = orderId
                });
            }
            else
            {
                return BadRequest(new { 
                    success = false, 
                    message = message,
                    errorType = ApiConstants.ErrorTypes.BusinessRuleViolation,
                    orderId = orderId
                });
            }
        }
        catch (Exception ex)
        {
            // Lỗi hệ thống không mong đợi
            return StatusCode(500, new { 
                success = false, 
                message = $"Lỗi hệ thống khi tạo payment link: {ex.Message}",
                errorType = ApiConstants.ErrorTypes.SystemError,
                orderId = orderId
            });
        }
    }

    /// <summary>
    /// Lấy payment link hiện có từ PayOS cho Order (khi đã tồn tại)
    /// </summary>
    [HttpGet("{orderId}/payment/link")]
    public async Task<IActionResult> GetOrderPaymentLink(int orderId)
    {
        try
        {
            var checkoutUrl = await _paymentService.GetExistingOrderPaymentLinkAsync(orderId);
            if (string.IsNullOrEmpty(checkoutUrl))
            {
                return NotFound(new { 
                    success = false, 
                    message = $"Không tìm thấy payment link cho đơn hàng #{orderId}. Payment link có thể chưa được tạo hoặc đã hết hạn.",
                    orderId = orderId
                });
            }
            
            return Ok(new { 
                success = true, 
                checkoutUrl = checkoutUrl,
                orderCode = orderId,
                message = "Lấy payment link thành công"
            });
        }
        catch (ArgumentException ex)
        {
            // Validation lỗi: orderId không hợp lệ
            return BadRequest(new { 
                success = false, 
                message = ex.Message,
                errorType = ApiConstants.ErrorTypes.ValidationError,
                orderId = orderId
            });
        }
        catch (InvalidOperationException ex)
        {
            // Business logic lỗi: order không tồn tại, đã hủy, đã thanh toán, v.v.
            var message = ex.Message;
            
            // Phân loại lỗi dựa trên message để trả về status code phù hợp
            if (message.Contains("không tồn tại") || message.Contains("Không tìm thấy"))
            {
                return NotFound(new { 
                    success = false, 
                    message = message,
                    errorType = ApiConstants.ErrorTypes.OrderNotFound,
                    orderId = orderId
                });
            }
            else if (message.Contains("đã bị hủy") || message.Contains("đã được thanh toán") || message.Contains("hóa đơn thanh toán"))
            {
                return BadRequest(new { 
                    success = false, 
                    message = message,
                    errorType = ApiConstants.ErrorTypes.OrderInvalidState,
                    orderId = orderId
                });
            }
            else
            {
                return BadRequest(new { 
                    success = false, 
                    message = message,
                    errorType = ApiConstants.ErrorTypes.BusinessRuleViolation,
                    orderId = orderId
                });
            }
        }
        catch (Exception ex)
        {
            // Lỗi hệ thống không mong đợi
            return StatusCode(500, new { 
                success = false, 
                message = $"Lỗi hệ thống khi lấy payment link: {ex.Message}",
                errorType = ApiConstants.ErrorTypes.SystemError,
                orderId = orderId
            });
        }
    }

    /// <summary>
    /// Lấy chi tiết đơn hàng
    /// </summary>
    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetById(int orderId)
    {
        try
        {
            var order = await _orderService.GetByIdAsync(orderId);
            if (order == null)
                return NotFound(new { success = false, message = "Đơn hàng không tồn tại" });

            return Ok(new { success = true, data = order });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy tất cả đơn hàng (Admin)
    /// </summary>
    [HttpGet("admin")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var orders = await _orderService.GetAllAsync();
            return Ok(new { success = true, data = orders });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // Removed: POST /api/Order/create (dùng route có customerId)

    /// <summary>
    /// Tạo đơn hàng từ giỏ hàng theo customerId trên route (không cần truyền ID trong body)
    /// </summary>
    [HttpPost("customer/{customerId}/create")]
    public async Task<IActionResult> CreateOrderForCustomer(int customerId, [FromBody] CreateOrderRequest request)
    {
        try
        {
            if (!ModelState.IsValid || request == null)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }
            request.CustomerId = customerId;
            var order = await _orderService.CreateOrderAsync(request);
            return Ok(new { success = true, data = order, message = "Đã tạo đơn hàng thành công" });
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
    /// Mua ngay: tạo đơn trực tiếp từ danh sách sản phẩm, không dùng giỏ hàng
    /// </summary>
    [HttpPost("customers/{customerId}/orders/quick")]
    public async Task<IActionResult> CreateQuickOrder(int customerId, [FromBody] QuickOrderRequest request)
    {
        try
        {
            if (!ModelState.IsValid || request == null)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }
            request.CustomerId = customerId;
            var order = await _orderService.CreateQuickOrderAsync(request);
            return Ok(new { success = true, data = order, message = "Đã tạo đơn hàng mua ngay thành công" });
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
    /// Cập nhật trạng thái đơn hàng
    /// </summary>
    [HttpPut("{orderId}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
    {
        try
        {
            var order = await _orderService.UpdateOrderStatusAsync(orderId, request);
            return Ok(new { success = true, data = order, message = "Đã cập nhật trạng thái đơn hàng" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi cập nhật trạng thái đơn hàng: " + ex.Message });
        }
    }

    /// <summary>
    /// Xóa đơn hàng
    /// </summary>
    [HttpDelete("{orderId}")]
    public async Task<IActionResult> DeleteOrder(int orderId)
    {
        try
        {
            await _orderService.DeleteOrderAsync(orderId);
            return Ok(new { success = true, message = "Đã xóa đơn hàng" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Lỗi khi xóa đơn hàng: " + ex.Message });
        }
    }

    /// <summary>
    /// Endpoint xử lý cancel payment cho Order
    /// HOÀN TOÀN ĐỘC LẬP với PaymentController.Cancel (dành cho Booking)
    /// Redirect về frontend với orderId để phân biệt với booking
    /// </summary>
    [HttpGet("/api/payment/order/{orderId}/cancel")]
    [AllowAnonymous]
    public IActionResult CancelOrderPayment([FromRoute] int orderId, [FromQuery] string? status = null, [FromQuery] string? code = null)
    {
        // Redirect về trang hủy thanh toán trên FE với orderId để phân biệt với booking
        var frontendUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:5173";
        var frontendCancelUrl = $"{frontendUrl}/payment-cancel?orderId={orderId}&status={status}&code={code}";
        return Redirect(frontendCancelUrl);
    }
}
