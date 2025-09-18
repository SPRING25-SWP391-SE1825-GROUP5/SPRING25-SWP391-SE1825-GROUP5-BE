using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AuthenticatedUser")] // Tất cả user đã đăng nhập
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionService _promotionService;

        public PromotionController(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        /// <summary>
        /// Lấy danh sách tất cả khuyến mãi với phân trang và tìm kiếm
        /// </summary>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm (mã, mô tả)</param>
        /// <param name="status">Lọc theo trạng thái (ACTIVE, INACTIVE, EXPIRED)</param>
        /// <param name="promotionType">Lọc theo loại khuyến mãi (GENERAL, FIRST_TIME, BIRTHDAY, LOYALTY)</param>
        /// <returns>Danh sách khuyến mãi</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllPromotions(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string searchTerm = null,
            [FromQuery] string status = null,
            [FromQuery] string promotionType = null)
        {
            try
            {
                // Validate pagination parameters
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _promotionService.GetAllPromotionsAsync(pageNumber, pageSize, searchTerm, status, promotionType);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách khuyến mãi thành công",
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
        /// Lấy thông tin khuyến mãi theo ID
        /// </summary>
        /// <param name="id">ID khuyến mãi</param>
        /// <returns>Thông tin khuyến mãi</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPromotionById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID khuyến mãi không hợp lệ" });

                var promotion = await _promotionService.GetPromotionByIdAsync(id);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy thông tin khuyến mãi thành công",
                    data = promotion
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
        /// Lấy thông tin khuyến mãi theo mã
        /// </summary>
        /// <param name="code">Mã khuyến mãi</param>
        /// <returns>Thông tin khuyến mãi</returns>
        [HttpGet("code/{code}")]
        public async Task<IActionResult> GetPromotionByCode(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                    return BadRequest(new { success = false, message = "Mã khuyến mãi không được để trống" });

                var promotion = await _promotionService.GetPromotionByCodeAsync(code);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy thông tin khuyến mãi thành công",
                    data = promotion
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
        /// Tạo khuyến mãi mới (chỉ ADMIN)
        /// </summary>
        /// <param name="request">Thông tin khuyến mãi mới</param>
        /// <returns>Thông tin khuyến mãi đã tạo</returns>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreatePromotion([FromBody] CreatePromotionRequest request)
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

                var promotion = await _promotionService.CreatePromotionAsync(request);
                
                return CreatedAtAction(nameof(GetPromotionById), new { id = promotion.PromotionId }, new { 
                    success = true, 
                    message = "Tạo khuyến mãi thành công",
                    data = promotion
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
        /// Cập nhật thông tin khuyến mãi (chỉ ADMIN)
        /// </summary>
        /// <param name="id">ID khuyến mãi</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Kết quả cập nhật</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdatePromotion(int id, [FromBody] UpdatePromotionRequest request)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID khuyến mãi không hợp lệ" });

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { 
                        success = false, 
                        message = "Dữ liệu không hợp lệ", 
                        errors = errors 
                    });
                }

                var promotion = await _promotionService.UpdatePromotionAsync(id, request);
                
                return Ok(new { 
                    success = true, 
                    message = "Cập nhật khuyến mãi thành công",
                    data = promotion
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
        /// Xác thực mã khuyến mãi
        /// </summary>
        /// <param name="request">Thông tin xác thực</param>
        /// <returns>Kết quả xác thực</returns>
        [HttpPost("validate")]
        public async Task<IActionResult> ValidatePromotion([FromBody] ValidatePromotionRequest request)
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

                var result = await _promotionService.ValidatePromotionAsync(request);
                
                return Ok(new { 
                    success = true, 
                    message = result.IsValid ? "Xác thực mã khuyến mãi thành công" : "Mã khuyến mãi không hợp lệ",
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
        /// Kích hoạt khuyến mãi (chỉ ADMIN)
        /// </summary>
        /// <param name="id">ID khuyến mãi</param>
        /// <returns>Kết quả kích hoạt</returns>
        [HttpPut("{id}/activate")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ActivatePromotion(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID khuyến mãi không hợp lệ" });

                var result = await _promotionService.ActivatePromotionAsync(id);
                
                if (result)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Kích hoạt khuyến mãi thành công" 
                    });
                }
                else
                {
                    return StatusCode(500, new { 
                        success = false, 
                        message = "Không thể kích hoạt khuyến mãi" 
                    });
                }
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
        /// Vô hiệu hóa khuyến mãi (chỉ ADMIN)
        /// </summary>
        /// <param name="id">ID khuyến mãi</param>
        /// <returns>Kết quả vô hiệu hóa</returns>
        [HttpPut("{id}/deactivate")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeactivatePromotion(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID khuyến mãi không hợp lệ" });

                var result = await _promotionService.DeactivatePromotionAsync(id);
                
                if (result)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Vô hiệu hóa khuyến mãi thành công" 
                    });
                }
                else
                {
                    return StatusCode(500, new { 
                        success = false, 
                        message = "Không thể vô hiệu hóa khuyến mãi" 
                    });
                }
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
        /// Lấy danh sách khuyến mãi đang hoạt động cho user
        /// </summary>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm (mã, mô tả)</param>
        /// <param name="promotionType">Lọc theo loại khuyến mãi (GENERAL, FIRST_TIME, BIRTHDAY, LOYALTY)</param>
        /// <returns>Danh sách khuyến mãi đang hoạt động</returns>
        [HttpGet("active")]
        public async Task<IActionResult> GetActivePromotions(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string searchTerm = null,
            [FromQuery] string promotionType = null)
        {
            try
            {
                // Validate pagination parameters
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _promotionService.GetAllPromotionsAsync(pageNumber, pageSize, searchTerm, "ACTIVE", promotionType);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách khuyến mãi đang hoạt động thành công",
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
    }
}
