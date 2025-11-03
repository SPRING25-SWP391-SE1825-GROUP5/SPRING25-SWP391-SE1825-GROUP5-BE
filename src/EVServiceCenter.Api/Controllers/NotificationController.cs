using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Responses;
using System.Security.Claims;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AuthenticatedUser")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Lấy danh sách thông báo của user hiện tại (theo userId từ JWT token)
        /// </summary>
        [HttpGet("my-notifications")]
        public async Task<IActionResult> GetMyNotifications()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định người dùng" });

                var notifications = await _notificationService.GetUserNotificationsAsync(userId.Value);
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách thông báo thành công", 
                    data = notifications 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách thông báo của customer hiện tại (theo customerId từ JWT token)
        /// </summary>
        [HttpGet("customer-notifications")]
        public async Task<IActionResult> GetCustomerNotifications()
        {
            try
            {
                // Lấy customerId từ JWT token
                var customerId = GetCustomerIdFromToken();
                if (!customerId.HasValue)
                {
                    return BadRequest(new { success = false, message = "Không xác định được khách hàng. Vui lòng đăng nhập lại." });
                }

                // Lấy userId từ customerId thông qua service
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định người dùng" });

                var notifications = await _notificationService.GetUserNotificationsAsync(userId.Value);
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách thông báo thành công", 
                    data = notifications,
                    customerId = customerId.Value
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Đánh dấu thông báo đã đọc
        /// </summary>
        [HttpPut("{notificationId}/read")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            try
            {
                await _notificationService.MarkNotificationAsReadAsync(notificationId);
                return Ok(new { success = true, message = "Đánh dấu thông báo đã đọc thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Lấy số lượng thông báo chưa đọc
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định người dùng" });

                var count = await _notificationService.GetUnreadCountAsync(userId.Value);
                return Ok(new { 
                    success = true, 
                    message = "Lấy số lượng thông báo chưa đọc thành công", 
                    data = new { unreadCount = count } 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                            ?? User.FindFirst("userId")?.Value 
                            ?? User.FindFirst("sub")?.Value 
                            ?? User.FindFirst("nameid")?.Value;
                            
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return null;
        }

        private int? GetCustomerIdFromToken()
        {
            // Lấy customerId từ JWT token claim
            var customerIdClaim = User.FindFirst("customerId")?.Value;
            if (int.TryParse(customerIdClaim, out int customerId))
            {
                return customerId;
            }
            return null;
        }
    }
}
