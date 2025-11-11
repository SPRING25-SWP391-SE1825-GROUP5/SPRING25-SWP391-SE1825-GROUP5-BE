using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        private readonly ICloudinaryService _cloudinaryService;

        public MessageController(
            IMessageService messageService,
            ICloudinaryService cloudinaryService,
            ILogger<MessageController> logger) : base(logger)
        {
            _messageService = messageService;
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost]
        [Consumes("multipart/form-data", "application/json")]
        public async Task<IActionResult> CreateMessage()
        {
            try
            {
                CreateMessageWithFilesRequest? formRequest = null;
                CreateMessageRequest? jsonRequest = null;

                if (Request.HasFormContentType)
                {
                    formRequest = await TryBindFormDataAsync();
                }

                if (formRequest == null && Request.ContentType?.Contains("application/json") == true)
                {
                    try
                    {
                        Request.EnableBuffering();
                        Request.Body.Position = 0;
                        using var reader = new StreamReader(Request.Body, leaveOpen: true);
                        var bodyContent = await reader.ReadToEndAsync();
                        Request.Body.Position = 0;

                        jsonRequest = JsonSerializer.Deserialize<CreateMessageRequest>(bodyContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    }
                    catch
                    {
                    }
                }

                CreateMessageRequest request;
                string? attachmentUrl = null;

                if (formRequest != null || Request.ContentType?.Contains("multipart/form-data") == true)
                {
                    var attachments = formRequest?.Attachments;
                    if ((attachments == null || attachments.Count == 0) && Request.HasFormContentType)
                    {
                        var formFiles = Request.Form.Files;
                        if (formFiles != null && formFiles.Count > 0)
                        {
                            attachments = formFiles.ToList();
                        }
                    }

                    if (formRequest == null && Request.HasFormContentType)
                    {
                        var conversationIdStr = Request.Form["ConversationId"].FirstOrDefault();
                        var content = Request.Form["Content"].FirstOrDefault();
                        var senderUserIdStr = Request.Form["SenderUserId"].FirstOrDefault();
                        var senderGuestSessionId = Request.Form["SenderGuestSessionId"].FirstOrDefault();
                        var replyToMessageIdStr = Request.Form["ReplyToMessageId"].FirstOrDefault();

                        if (long.TryParse(conversationIdStr, out var conversationId))
                        {
                            formRequest = new CreateMessageWithFilesRequest
                            {
                                ConversationId = conversationId,
                                Content = content,
                                SenderUserId = int.TryParse(senderUserIdStr, out var userId) ? userId : null,
                                SenderGuestSessionId = senderGuestSessionId,
                                ReplyToMessageId = long.TryParse(replyToMessageIdStr, out var replyId) ? replyId : null,
                                Attachments = attachments
                            };
                        }
                    }

                    if (attachments != null && attachments.Count > 0)
                    {
                        var uploadedUrls = new List<string>();

                        foreach (var file in attachments)
                        {
                            if (file != null && file.Length > 0)
                            {
                                try
                                {
                                    var url = await _cloudinaryService.UploadImageAsync(file, "messages");
                                    uploadedUrls.Add(url);
                                }
                                catch (Exception ex)
                                {
                                    return BadRequest(new { success = false, message = $"Lỗi khi upload file {file.FileName}: {ex.Message}" });
                                }
                            }
                        }

                        if (uploadedUrls.Count > 0)
                        {
                            if (uploadedUrls.Count == 1)
                            {
                                attachmentUrl = uploadedUrls[0];
                            }
                            else
                            {
                                attachmentUrl = JsonSerializer.Serialize(uploadedUrls);
                            }
                        }
                    }

                    if (formRequest == null)
                    {
                        return BadRequest(new { success = false, message = "Không thể parse form data. Vui lòng kiểm tra lại request." });
                    }

                    request = new CreateMessageRequest
                    {
                        ConversationId = formRequest.ConversationId,
                        SenderUserId = formRequest.SenderUserId,
                        SenderGuestSessionId = formRequest.SenderGuestSessionId,
                        Content = formRequest.Content ?? string.Empty,
                        AttachmentUrl = attachmentUrl,
                        ReplyToMessageId = formRequest.ReplyToMessageId
                    };

                }
                else if (jsonRequest != null)
                {
                    request = jsonRequest;
                }
                else
                {
                    return BadRequest(new { success = false, message = "Request body is required" });
                }

                var validationResult = ValidateModelState();
                if (validationResult != null)
                {
                    return validationResult;
                }

                if (!request.SenderUserId.HasValue && string.IsNullOrWhiteSpace(request.SenderGuestSessionId))
                {
                    var currentUserId = GetCurrentUserId();
                    if (currentUserId.HasValue)
                    {
                        request.SenderUserId = currentUserId.Value;
                        request.SenderGuestSessionId = null;
                    }
                }
                else
                {
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

               if (!request.SenderUserId.HasValue && string.IsNullOrWhiteSpace(request.SenderGuestSessionId))
                {
                    var currentUserId = GetCurrentUserId();
                    if (currentUserId.HasValue)
                    {
                        request.SenderUserId = currentUserId.Value;
                        request.SenderGuestSessionId = null;
                    }
                }
                else
                {
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

        [HttpPost("conversations/{conversationId}/typing")]
        public async Task<IActionResult> SendTypingIndicator(long conversationId, [FromBody] TypingIndicatorRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentGuestSessionId = request?.GuestSessionId;

                if (!currentUserId.HasValue && string.IsNullOrEmpty(currentGuestSessionId))
                {
                    return BadRequest(new { success = false, message = "Phải có userId hoặc guestSessionId" });
                }

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

        private async Task<CreateMessageWithFilesRequest?> TryBindFormDataAsync()
        {
            try
            {
                var form = await Request.ReadFormAsync();
                var conversationIdStr = form["ConversationId"].FirstOrDefault();
                var content = form["Content"].FirstOrDefault();
                var senderUserIdStr = form["SenderUserId"].FirstOrDefault();
                var senderGuestSessionId = form["SenderGuestSessionId"].FirstOrDefault();
                var replyToMessageIdStr = form["ReplyToMessageId"].FirstOrDefault();
                var attachments = form.Files?.ToList();

                if (long.TryParse(conversationIdStr, out var conversationId))
                {
                    return new CreateMessageWithFilesRequest
                    {
                        ConversationId = conversationId,
                        Content = content,
                        SenderUserId = int.TryParse(senderUserIdStr, out var userId) ? userId : null,
                        SenderGuestSessionId = senderGuestSessionId,
                        ReplyToMessageId = long.TryParse(replyToMessageIdStr, out var replyId) ? replyId : null,
                        Attachments = attachments
                    };
                }
            }
            catch
            {
            }

            return null;
        }

        public class CreateMessageWithFilesRequest
        {
            [Required(ErrorMessage = "ID cuộc trò chuyện là bắt buộc")]
            [Range(1, long.MaxValue, ErrorMessage = "ID cuộc trò chuyện phải là số nguyên dương")]
            public long ConversationId { get; set; }

            public int? SenderUserId { get; set; }
            public string? SenderGuestSessionId { get; set; }

            [StringLength(4000, ErrorMessage = "Nội dung tin nhắn không được vượt quá 4000 ký tự")]
            public string? Content { get; set; }

            [Range(1, long.MaxValue, ErrorMessage = "ID tin nhắn trả lời phải là số nguyên dương")]
            public long? ReplyToMessageId { get; set; }

            public List<IFormFile>? Attachments { get; set; }
        }
    }
}
