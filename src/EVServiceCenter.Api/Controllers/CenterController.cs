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
    [Authorize(Policy = "StaffOrAdmin")] // Chỉ STAFF và ADMIN mới có quyền truy cập
    public class CenterController : ControllerBase
    {
        private readonly ICenterService _centerService;

        public CenterController(ICenterService centerService)
        {
            _centerService = centerService;
        }

        /// <summary>
        /// Lấy danh sách tất cả trung tâm với phân trang và tìm kiếm
        /// </summary>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm</param>
        /// <param name="city">Lọc theo thành phố</param>
        /// <returns>Danh sách trung tâm</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllCenters(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string searchTerm = null,
            [FromQuery] string city = null)
        {
            try
            {
                // Validate pagination parameters
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _centerService.GetAllCentersAsync(pageNumber, pageSize, searchTerm, city);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách trung tâm thành công",
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
        /// Lấy danh sách trung tâm đang hoạt động với phân trang và tìm kiếm
        /// </summary>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm</param>
        /// <param name="city">Lọc theo thành phố</param>
        /// <returns>Danh sách trung tâm đang hoạt động</returns>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveCenters(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string searchTerm = null,
            [FromQuery] string city = null)
        {
            try
            {
                // Validate pagination parameters
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _centerService.GetActiveCentersAsync(pageNumber, pageSize, searchTerm, city);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách trung tâm đang hoạt động thành công",
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
        /// Lấy thông tin trung tâm theo ID
        /// </summary>
        /// <param name="id">ID trung tâm</param>
        /// <returns>Thông tin trung tâm</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCenterById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID trung tâm không hợp lệ" });

                var center = await _centerService.GetCenterByIdAsync(id);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy thông tin trung tâm thành công",
                    data = center
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
        /// Tạo trung tâm mới
        /// </summary>
        /// <param name="request">Thông tin trung tâm mới</param>
        /// <returns>Thông tin trung tâm đã tạo</returns>
        [HttpPost]
        public async Task<IActionResult> CreateCenter([FromBody] CreateCenterRequest request)
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

                var center = await _centerService.CreateCenterAsync(request);
                
                return CreatedAtAction(nameof(GetCenterById), new { id = center.CenterId }, new { 
                    success = true, 
                    message = "Tạo trung tâm thành công",
                    data = center
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
        /// Cập nhật thông tin trung tâm
        /// </summary>
        /// <param name="id">ID trung tâm</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Thông tin trung tâm đã cập nhật</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCenter(int id, [FromBody] UpdateCenterRequest request)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID trung tâm không hợp lệ" });

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { 
                        success = false, 
                        message = "Dữ liệu không hợp lệ", 
                        errors = errors 
                    });
                }

                var center = await _centerService.UpdateCenterAsync(id, request);
                
                return Ok(new { 
                    success = true, 
                    message = "Cập nhật thông tin trung tâm thành công",
                    data = center
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
    }
}
