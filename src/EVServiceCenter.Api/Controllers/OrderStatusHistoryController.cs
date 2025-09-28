using System;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderStatusHistoryController : ControllerBase
{
    private readonly IOrderStatusHistoryRepository _orderStatusHistoryRepository;

    public OrderStatusHistoryController(IOrderStatusHistoryRepository orderStatusHistoryRepository)
    {
        _orderStatusHistoryRepository = orderStatusHistoryRepository;
    }

    /// <summary>
    /// Lấy lịch sử trạng thái đơn hàng
    /// </summary>
    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetByOrderId(int orderId)
    {
        try
        {
            var statusHistory = await _orderStatusHistoryRepository.GetByOrderIdAsync(orderId);
            return Ok(new { success = true, data = statusHistory });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
