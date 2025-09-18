using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")] // Chỉ ADMIN mới có quyền truy cập
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Lấy danh sách tất cả người dùng với phân trang và tìm kiếm (chỉ ADMIN)
        /// </summary>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm</param>
        /// <param name="role">Lọc theo vai trò</param>
        /// <returns>Danh sách người dùng</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string searchTerm = null,
            [FromQuery] string role = null)
        {
            try
            {
                // Validate pagination parameters
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _userService.GetAllUsersAsync(pageNumber, pageSize, searchTerm, role);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách người dùng thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        /// <summary>
        /// Lấy thông tin người dùng theo ID (chỉ ADMIN)
        /// </summary>
        /// <param name="id">ID người dùng</param>
        /// <returns>Thông tin người dùng</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID người dùng không hợp lệ" });

                var user = await _userService.GetUserByIdAsync(id);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy thông tin người dùng thành công",
                    data = user
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        /// <summary>
        /// Tạo người dùng mới (chỉ ADMIN)
        /// </summary>
        /// <param name="request">Thông tin người dùng mới</param>
        /// <returns>Thông tin người dùng đã tạo</returns>
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { 
                        success = false, 
                        message = "Dữ liệu không hợp lệ", 
                        errors = errors 
                    });
                }

                var user = await _userService.CreateUserAsync(request);
                
                return CreatedAtAction(nameof(GetUserById), new { id = user.UserId }, new { 
                    success = true, 
                    message = "Tạo người dùng thành công",
                    data = user
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        /// <summary>
        /// Kích hoạt người dùng (chỉ ADMIN)
        /// </summary>
        /// <param name="id">ID người dùng</param>
        /// <returns>Kết quả kích hoạt</returns>
        [HttpPatch("{id}/activate")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID người dùng không hợp lệ" });

                var result = await _userService.ActivateUserAsync(id);
                
                if (result)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Kích hoạt người dùng thành công" 
                    });
                }
                else
                {
                    return StatusCode(500, new { 
                        success = false, 
                        message = "Không thể kích hoạt người dùng" 
                    });
                }
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        /// <summary>
        /// Vô hiệu hóa người dùng (chỉ ADMIN)
        /// </summary>
        /// <param name="id">ID người dùng</param>
        /// <returns>Kết quả vô hiệu hóa</returns>
        [HttpPatch("{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID người dùng không hợp lệ" });

                // Không cho phép vô hiệu hóa chính mình
                if (IsCurrentUser(id))
                    return BadRequest(new { success = false, message = "Không thể vô hiệu hóa tài khoản của chính mình" });

                var result = await _userService.DeactivateUserAsync(id);
                
                if (result)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Vô hiệu hóa người dùng thành công" 
                    });
                }
                else
                {
                    return StatusCode(500, new { 
                        success = false, 
                        message = "Không thể vô hiệu hóa người dùng" 
                    });
                }
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi hệ thống: " + ex.Message 
                });
            }
        }

        #region Helper Methods

        private bool IsCurrentUser(int userId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int currentUserId) && currentUserId == userId;
        }

        #endregion
    }
}