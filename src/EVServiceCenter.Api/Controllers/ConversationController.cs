using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AuthenticatedUser")]
    public class ConversationController : BaseController
    {
        private readonly IConversationService _conversationService;

        public ConversationController(
            IConversationService conversationService,
            ILogger<ConversationController> logger) : base(logger)
        {
            _conversationService = conversationService;
        }

        
        [HttpPost]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
        {
            try
            {
                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await _conversationService.CreateConversationAsync(request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Tạo cuộc trò chuyện");
            }
        }

        
        [HttpGet("{conversationId}")]
        public async Task<IActionResult> GetConversation(long conversationId)
        {
            try
            {
                var result = await _conversationService.GetConversationByIdAsync(conversationId);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy cuộc trò chuyện" });
                }

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Lấy cuộc trò chuyện");
            }
        }

        
        [HttpPut("{conversationId}")]
        public async Task<IActionResult> UpdateConversation(long conversationId, [FromBody] UpdateConversationRequest request)
        {
            try
            {
                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await _conversationService.UpdateConversationAsync(conversationId, request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Cập nhật cuộc trò chuyện");
            }
        }

        
        [HttpDelete("{conversationId}")]
        public async Task<IActionResult> DeleteConversation(long conversationId)
        {
            try
            {
                var result = await _conversationService.DeleteConversationAsync(conversationId);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy cuộc trò chuyện" });
                }

                return Ok(new { success = true, message = "Xóa cuộc trò chuyện thành công" });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Xóa cuộc trò chuyện");
            }
        }

        
        [HttpPost("get-or-create")]
        public async Task<IActionResult> GetOrCreateConversation([FromBody] GetOrCreateConversationRequest request)
        {
            try
            {
                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await _conversationService.GetOrCreateConversationAsync(request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Lấy hoặc tạo cuộc trò chuyện");
            }
        }

        
        [HttpGet("my-conversations")]
        public async Task<IActionResult> GetMyConversations([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "Không xác định được người dùng" });
                }

                var result = await _conversationService.GetConversationsByUserIdAsync(userId.Value, page, pageSize);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Lấy danh sách cuộc trò chuyện");
            }
        }

   

       
        [HttpGet("all")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetAllConversations([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null)
        {
            try
            {
                var result = await _conversationService.GetAllConversationsAsync(page, pageSize, searchTerm);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Lấy tất cả cuộc trò chuyện");
            }
        }

        /// <summary>
        /// Test endpoint để debug JWT claims
        /// </summary>
        [HttpGet("debug-claims")]
        public IActionResult DebugClaims()
        {
            try
            {
                var allClaims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList();
                var userId = GetCurrentUserId();
                
                return Ok(new { 
                    success = true, 
                    data = new {
                        allClaims,
                        userId,
                        isAuthenticated = User.Identity?.IsAuthenticated,
                        name = User.Identity?.Name
                    }
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Debug claims");
            }
        }
        
        
    }
}

