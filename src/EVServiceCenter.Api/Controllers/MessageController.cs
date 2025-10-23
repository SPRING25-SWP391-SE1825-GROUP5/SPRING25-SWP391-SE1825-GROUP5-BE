using System;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AuthenticatedUser")]
    public class MessageController : BaseController
    {
        private readonly IMessageService _messageService;

        public MessageController(
            IMessageService messageService,
            ILogger<MessageController> logger) : base(logger)
        {
            _messageService = messageService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage([FromBody] CreateMessageRequest request)
        {
            try
            {
                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;


                if (!request.SenderUserId.HasValue && string.IsNullOrEmpty(request.SenderGuestSessionId))
                {
                    var currentUserId = GetCurrentUserId();
                    if (currentUserId.HasValue)
                    {
                        request.SenderUserId = currentUserId.Value;
                    }
                }

                var result = await _messageService.CreateMessageAsync(request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Tạo tin nhắn");
            }
        }

        [HttpGet("{messageId}")]
        public async Task<IActionResult> GetMessage(long messageId)
        {
            try
            {
                var result = await _messageService.GetMessageByIdAsync(messageId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Lấy tin nhắn");
            }
        }

        [HttpPut("{messageId}")]
        public async Task<IActionResult> UpdateMessage(long messageId, [FromBody] UpdateMessageRequest request)
        {
            try
            {
                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await _messageService.UpdateMessageAsync(messageId, request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Cập nhật tin nhắn");
            }
        }

        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(long messageId)
        {
            try
            {
                var result = await _messageService.DeleteMessageAsync(messageId);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy tin nhắn" });
                }

                return Ok(new { success = true, message = "Xóa tin nhắn thành công" });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Xóa tin nhắn");
            }
        }

        [HttpGet("conversations/{conversationId}")]
        public async Task<IActionResult> GetMessagesByConversation(long conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var result = await _messageService.GetMessagesByConversationIdAsync(conversationId, page, pageSize);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Lấy danh sách tin nhắn theo cuộc trò chuyện");
            }
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyMessages([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "Không xác định được người dùng" });
                }

                var result = await _messageService.GetMessagesByUserIdAsync(userId.Value, page, pageSize);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Lấy danh sách tin nhắn của tôi");
            }
        }

        [HttpGet("guest")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMessagesByGuest([FromQuery] string guestSessionId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(guestSessionId))
                {
                    return BadRequest(new { success = false, message = "GuestSessionId là bắt buộc" });
                }

                var result = await _messageService.GetMessagesByGuestSessionIdAsync(guestSessionId, page, pageSize);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Lấy danh sách tin nhắn theo phiên khách");
            }
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchMessagesRequest request)
        {
            try
            {
                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await _messageService.SearchMessagesAsync(request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Tìm kiếm tin nhắn");
            }
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
        {
            try
            {
                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                // Fallback to current user if sender not provided
                if (!request.SenderUserId.HasValue && string.IsNullOrEmpty(request.SenderGuestSessionId))
                {
                    var currentUserId = GetCurrentUserId();
                    if (currentUserId.HasValue)
                    {
                        request.SenderUserId = currentUserId.Value;
                    }
                }

                var result = await _messageService.SendMessageAsync(request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {

                return HandleException(ex, "Gửi tin nhắn");
            }
        }



    }
}


