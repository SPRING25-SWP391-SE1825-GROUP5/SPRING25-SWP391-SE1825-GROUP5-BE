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

        [HttpPost("{conversationId}/members")]
        public async Task<IActionResult> AddMember(long conversationId, [FromBody] AddMemberToConversationRequest request)
        {
            try
            {
                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await _conversationService.AddMemberToConversationAsync(conversationId, request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Thêm thành viên vào cuộc trò chuyện");
            }
        }

        [HttpDelete("{conversationId}/members")]
        public async Task<IActionResult> RemoveMember(long conversationId, [FromQuery] int? userId = null, [FromQuery] string? guestSessionId = null)
        {
            try
            {
                var result = await _conversationService.RemoveMemberFromConversationAsync(conversationId, userId, guestSessionId);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy thành viên" });
                }

                return Ok(new { success = true, message = "Xóa thành viên thành công" });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Xóa thành viên khỏi cuộc trò chuyện");
            }
        }

        [HttpGet("{conversationId}/members")]
        public async Task<IActionResult> GetMembers(long conversationId)
        {
            try
            {
                var result = await _conversationService.GetConversationMembersAsync(conversationId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Lấy danh sách thành viên");
            }
        }

        [HttpPut("{conversationId}/last-read")]
        public async Task<IActionResult> UpdateLastReadTime(long conversationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _conversationService.UpdateLastReadTimeAsync(conversationId, userId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Cập nhật thời gian đọc cuối cùng");
            }
        }

        [HttpPut("{conversationId}/reassign-center")]
        [Authorize(Policy = "AdminOrStaff")]
        public async Task<IActionResult> ReassignCenter(long conversationId, [FromBody] ReassignCenterRequest request)
        {
            try
            {
                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await _conversationService.ReassignCenterAsync(conversationId, request.NewCenterId, request.Reason);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Chuyển trung tâm");
            }
        }

        [HttpGet("staff/{staffId}")]
        [Authorize(Policy = "AdminOrStaff")]
        public async Task<IActionResult> GetConversationsByStaff(int staffId, [FromQuery] int page = 0, [FromQuery] int pageSize = 0)
        {
            try
            {
                var result = await _conversationService.GetConversationsByStaffIdAsync(staffId, page, pageSize);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Lấy danh sách cuộc trò chuyện của staff");
            }
        }

        [HttpGet("unassigned")]
        [Authorize(Policy = "AdminOrStaff")]
        public async Task<IActionResult> GetUnassignedConversations([FromQuery] int page = 0, [FromQuery] int pageSize = 0)
        {
            try
            {
                var result = await _conversationService.GetUnassignedConversationsAsync(page, pageSize);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Lấy danh sách cuộc trò chuyện chưa được assign");
            }
        }
    }

    public class ReassignCenterRequest
    {
        public int NewCenterId { get; set; }
        public string? Reason { get; set; }
    }
}
