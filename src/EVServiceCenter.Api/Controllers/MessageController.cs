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


                // Normalize: Constraint CK_Messages_SenderXor requires either SenderUserId OR SenderGuestSessionId, not both
                // If both are missing, try to get current user ID
                if (!request.SenderUserId.HasValue && string.IsNullOrWhiteSpace(request.SenderGuestSessionId))
                {
                    var currentUserId = GetCurrentUserId();
                    if (currentUserId.HasValue)
                    {
                        request.SenderUserId = currentUserId.Value;
                        request.SenderGuestSessionId = null; // Explicitly set to null
                    }
                }
                else
                {
                    // Normalize: If SenderUserId is provided, ensure SenderGuestSessionId is null
                    // If SenderGuestSessionId is provided, ensure SenderUserId is null
                    if (request.SenderUserId.HasValue)
                    {
                        request.SenderGuestSessionId = null;
                    }
                    else if (!string.IsNullOrWhiteSpace(request.SenderGuestSessionId))
                    {
                        request.SenderUserId = null;
                        request.SenderGuestSessionId = request.SenderGuestSessionId.Trim();
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

               // Normalize: Constraint CK_Messages_SenderXor requires either SenderUserId OR SenderGuestSessionId, not both
               // If both are missing, try to get current user ID
               if (!request.SenderUserId.HasValue && string.IsNullOrWhiteSpace(request.SenderGuestSessionId))
                {
                    var currentUserId = GetCurrentUserId();
                    if (currentUserId.HasValue)
                    {
                        request.SenderUserId = currentUserId.Value;
                        request.SenderGuestSessionId = null; // Explicitly set to null
                    }
                }
                else
                {
                    // Normalize: If SenderUserId is provided, ensure SenderGuestSessionId is null
                    // If SenderGuestSessionId is provided, ensure SenderUserId is null
                    if (request.SenderUserId.HasValue)
                    {
                        request.SenderGuestSessionId = null;
                    }
                    else if (!string.IsNullOrWhiteSpace(request.SenderGuestSessionId))
                    {
                        request.SenderUserId = null;
                        request.SenderGuestSessionId = request.SenderGuestSessionId.Trim();
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

        /// <summary>
        /// Gửi typing indicator cho conversation
        /// </summary>
        [HttpPost("conversations/{conversationId}/typing")]
        public async Task<IActionResult> SendTypingIndicator(long conversationId, [FromBody] TypingIndicatorRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentGuestSessionId = request?.GuestSessionId;

                // Validate: Phải có userId hoặc guestSessionId
                if (!currentUserId.HasValue && string.IsNullOrEmpty(currentGuestSessionId))
                {
                    return BadRequest(new { success = false, message = "Phải có userId hoặc guestSessionId" });
                }

                // Gửi typing indicator qua ChatHubService
                await _messageService.NotifyTypingAsync(conversationId, currentUserId, currentGuestSessionId, request?.IsTyping ?? true);

                return Ok(new {
                    success = true,
                    message = "Đã gửi typing indicator",
                    data = new {
                        conversationId,
                        userId = currentUserId,
                        guestSessionId = currentGuestSessionId,
                        isTyping = request?.IsTyping ?? true
                    }
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Gửi typing indicator");
            }
        }

        public class TypingIndicatorRequest
        {
            public bool? IsTyping { get; set; } = true;
            public string? GuestSessionId { get; set; }
        }



    }
}


