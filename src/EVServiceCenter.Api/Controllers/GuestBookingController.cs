using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EVServiceCenter.Application.Service;
using EVServiceCenter.Application.Models.Requests;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/guest/bookings")]
public class GuestBookingController : ControllerBase
{
    private readonly GuestBookingService _guestBookingService;

    public GuestBookingController(GuestBookingService guestBookingService)
    {
        _guestBookingService = guestBookingService;
    }

    // Anonymous booking for guests
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] GuestBookingRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
        }

        try
        {
            var result = await _guestBookingService.CreateGuestBookingAsync(request);
            return Ok(new { success = true, message = "Tạo đặt lịch thành công. Vui lòng thanh toán để xác nhận.", data = result });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
        }
    }
}


