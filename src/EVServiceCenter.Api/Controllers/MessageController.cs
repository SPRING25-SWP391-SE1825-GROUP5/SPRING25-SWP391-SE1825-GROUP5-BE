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
                _logger.LogInformation("=== CreateMessage ENDPOINT CALLED ===");
                _logger.LogInformation("Content-Type: {ContentType}", Request.ContentType);
                _logger.LogInformation("HasFormContentType: {HasFormContentType}", Request.HasFormContentType);
                _logger.LogInformation("ContentLength: {ContentLength}", Request.ContentLength);

                CreateMessageWithFilesRequest? formRequest = null;
                CreateMessageRequest? jsonRequest = null;

                // Try to bind form data first
                if (Request.HasFormContentType)
                {
                    _logger.LogInformation("Attempting to bind form data...");
                    formRequest = await TryBindFormDataAsync();
                    _logger.LogInformation("formRequest is null: {IsNull}", formRequest == null);
                }

                // Try to bind JSON if not form data
                if (formRequest == null && Request.ContentType?.Contains("application/json") == true)
                {
                    _logger.LogInformation("Attempting to bind JSON data...");
                    try
                    {
                        Request.EnableBuffering();
                        Request.Body.Position = 0;
                        using var reader = new StreamReader(Request.Body, leaveOpen: true);
                        var bodyContent = await reader.ReadToEndAsync();
                        Request.Body.Position = 0;

                        _logger.LogInformation("Request body content (first 500 chars): {BodyContent}",
                            bodyContent.Length > 500 ? bodyContent.Substring(0, 500) : bodyContent);

                        jsonRequest = JsonSerializer.Deserialize<CreateMessageRequest>(bodyContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        _logger.LogInformation("jsonRequest is null: {IsNull}", jsonRequest == null);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deserializing JSON request");
                    }
                }

                if (formRequest != null)
                {
                    _logger.LogInformation("formRequest details: ConversationId={ConversationId}, Content={Content}, Attachments={AttachmentsCount}",
                        formRequest.ConversationId, formRequest.Content, formRequest.Attachments?.Count ?? 0);
                }

                if (jsonRequest != null)
                {
                    _logger.LogInformation("jsonRequest details: ConversationId={ConversationId}, Content={Content}, SenderUserId={SenderUserId}, SenderGuestSessionId={SenderGuestSessionId}",
                        jsonRequest.ConversationId, jsonRequest.Content, jsonRequest.SenderUserId, jsonRequest.SenderGuestSessionId);
                }

                // Log ModelState errors if any
                if (!ModelState.IsValid)
                {
                    var modelStateErrors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value?.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}") ?? Array.Empty<string>())
                        .ToList();

                    _logger.LogWarning("ModelState is invalid. Errors: {Errors}", string.Join("; ", modelStateErrors));
                }

                CreateMessageRequest request;
                string? attachmentUrl = null;

                // Handle multipart/form-data (with files)
                if (formRequest != null || Request.ContentType?.Contains("multipart/form-data") == true)
                {
                    // Try to get files from Request.Form if not in formRequest
                    var attachments = formRequest?.Attachments;
                    if ((attachments == null || attachments.Count == 0) && Request.HasFormContentType)
                    {
                        _logger.LogInformation("Checking Request.Form.Files for attachments...");
                        var formFiles = Request.Form.Files;
                        if (formFiles != null && formFiles.Count > 0)
                        {
                            _logger.LogInformation("Found {Count} files in Request.Form.Files", formFiles.Count);
                            attachments = formFiles.ToList();
                        }
                    }

                    if (formRequest == null && Request.HasFormContentType)
                    {
                        // Try to parse form data manually
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
                            _logger.LogInformation("Created formRequest from Request.Form. ConversationId: {ConversationId}", conversationId);
                        }
                    }

                    _logger.LogInformation("Processing form request. ConversationId: {ConversationId}, Attachments count: {Count}",
                        formRequest?.ConversationId ?? 0, attachments?.Count ?? 0);

                    // Handle file uploads if provided
                    if (attachments != null && attachments.Count > 0)
                    {
                        _logger.LogInformation("Found {Count} attachment(s). Processing all files...", attachments.Count);

                        var uploadedUrls = new List<string>();

                        // Upload all files to Cloudinary
                        foreach (var file in attachments)
                        {
                            if (file != null && file.Length > 0)
                            {
                                _logger.LogInformation("Uploading file: Name={FileName}, Length={Length}, ContentType={ContentType}",
                                    file.FileName, file.Length, file.ContentType);

                                try
                                {
                                    // Upload to Cloudinary in "messages" folder
                                    var url = await _cloudinaryService.UploadImageAsync(file, "messages");
                                    uploadedUrls.Add(url);
                                    _logger.LogInformation("File uploaded successfully to Cloudinary: {AttachmentUrl}", url);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error uploading file {FileName} to Cloudinary", file.FileName);
                                    return BadRequest(new { success = false, message = $"Lỗi khi upload file {file.FileName}: {ex.Message}" });
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Skipping null or empty file. FileName: {FileName}, Length: {Length}",
                                    file?.FileName, file?.Length);
                            }
                        }

                        // Store multiple URLs as JSON array in AttachmentUrl field
                        if (uploadedUrls.Count > 0)
                        {
                            if (uploadedUrls.Count == 1)
                            {
                                // Single file: store as plain string for backward compatibility
                                attachmentUrl = uploadedUrls[0];
                            }
                            else
                            {
                                // Multiple files: store as JSON array
                                attachmentUrl = JsonSerializer.Serialize(uploadedUrls);
                            }
                            _logger.LogInformation("All files uploaded. Total: {Count}, AttachmentUrl: {AttachmentUrl}",
                                uploadedUrls.Count, attachmentUrl);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No attachments found in form request");
                    }

                    // Ensure formRequest is not null before using it
                    if (formRequest == null)
                    {
                        _logger.LogWarning("formRequest is still null after parsing. Cannot create message.");
                        return BadRequest(new { success = false, message = "Không thể parse form data. Vui lòng kiểm tra lại request." });
                    }

                    // Create CreateMessageRequest from form data
                    request = new CreateMessageRequest
                    {
                        ConversationId = formRequest.ConversationId,
                        SenderUserId = formRequest.SenderUserId,
                        SenderGuestSessionId = formRequest.SenderGuestSessionId,
                        Content = formRequest.Content ?? string.Empty,
                        AttachmentUrl = attachmentUrl,
                        ReplyToMessageId = formRequest.ReplyToMessageId
                    };

                    _logger.LogInformation("Created request from form. AttachmentUrl: {AttachmentUrl}", attachmentUrl);
                }
                // Handle application/json (without files)
                else if (jsonRequest != null)
                {
                    _logger.LogInformation("Processing JSON request. ConversationId: {ConversationId}, Content: {Content}",
                        jsonRequest.ConversationId, jsonRequest.Content);
                    request = jsonRequest;
                }
                else
                {
                    _logger.LogWarning("Neither form request nor JSON request provided. Content-Type: {ContentType}", Request.ContentType);
                    return BadRequest(new { success = false, message = "Request body is required" });
                }

                // Log request details before validation
                _logger.LogInformation("Before validation. Request: ConversationId={ConversationId}, Content={Content}, HasSenderUserId={HasSenderUserId}, HasSenderGuestSessionId={HasSenderGuestSessionId}, HasAttachmentUrl={HasAttachmentUrl}",
                    request.ConversationId, request.Content ?? "(null)", request.SenderUserId.HasValue, !string.IsNullOrEmpty(request.SenderGuestSessionId), !string.IsNullOrEmpty(request.AttachmentUrl));

                var validationResult = ValidateModelState();
                if (validationResult != null)
                {
                    var modelStateErrors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value?.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}") ?? Array.Empty<string>())
                        .ToList();

                    _logger.LogWarning("Validation failed. ModelState errors: {Errors}", string.Join("; ", modelStateErrors));
                    return validationResult;
                }

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error binding form data");
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

            // Support "attachments" array from frontend (attachments[0], attachments[1], etc.)
            // ASP.NET Core will bind attachments[0] to Attachments[0]
            public List<IFormFile>? Attachments { get; set; }
        }



    }
}


