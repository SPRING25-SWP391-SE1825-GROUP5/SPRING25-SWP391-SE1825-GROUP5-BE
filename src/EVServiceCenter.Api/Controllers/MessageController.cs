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



    }
}


