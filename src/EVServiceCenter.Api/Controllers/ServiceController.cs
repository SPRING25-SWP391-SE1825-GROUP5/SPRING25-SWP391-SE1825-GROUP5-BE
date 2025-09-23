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
    [Authorize(Policy = "AuthenticatedUser")] // Tất cả user đã đăng nhập đều có thể xem
    public class ServiceController : ControllerBase
    {
        private readonly IServiceService _serviceService;

        public ServiceController(IServiceService serviceService)
        {
            _serviceService = serviceService;
        }

        /// <summary>
        /// Lấy danh sách tất cả dịch vụ với phân trang và tìm kiếm
        /// </summary>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm</param>
        /// <param name="categoryId">Lọc theo danh mục dịch vụ</param>
        /// <returns>Danh sách dịch vụ</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllServices(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string searchTerm = null,
            [FromQuery] int? categoryId = null)
        {
            try
            {
                // Validate pagination parameters
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _serviceService.GetAllServicesAsync(pageNumber, pageSize, searchTerm, categoryId);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách dịch vụ thành công",
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
        /// Lấy danh sách các dịch vụ đang hoạt động (Services.IsActive = 1 AND ServiceCategories.IsActive = 1)
        /// </summary>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm</param>
        /// <param name="categoryId">Lọc theo danh mục dịch vụ</param>
        /// <returns>Danh sách dịch vụ đang hoạt động</returns>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveServices(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string searchTerm = null,
            [FromQuery] int? categoryId = null)
        {
            try
            {
                // Validate pagination parameters
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _serviceService.GetActiveServicesAsync(pageNumber, pageSize, searchTerm, categoryId);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách dịch vụ đang hoạt động thành công",
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
        /// Lấy thông tin dịch vụ theo ID
        /// </summary>
        /// <param name="id">ID dịch vụ</param>
        /// <returns>Thông tin dịch vụ</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetServiceById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID dịch vụ không hợp lệ" });

                var service = await _serviceService.GetServiceByIdAsync(id);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy thông tin dịch vụ thành công",
                    data = service
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
        /// Tạo dịch vụ mới
        /// </summary>
        /// <param name="request">Thông tin dịch vụ mới</param>
        /// <returns>Thông tin dịch vụ đã tạo</returns>
        [HttpPost]
        [Authorize(Policy = "StaffOrAdmin")] // Chỉ Staff và Admin mới được tạo dịch vụ
        public async Task<IActionResult> CreateService([FromBody] CreateServiceRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return BadRequest(new { 
                        success = false, 
                        message = "Dữ liệu không hợp lệ", 
                        errors = errors 
                    });
                }

                var result = await _serviceService.CreateServiceAsync(request);
                
                return CreatedAtAction(nameof(GetServiceById), new { id = result.ServiceId }, new { 
                    success = true, 
                    message = "Tạo dịch vụ thành công",
                    data = result
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
        /// Cập nhật thông tin dịch vụ
        /// </summary>
        /// <param name="id">ID dịch vụ cần cập nhật</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Thông tin dịch vụ đã cập nhật</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "StaffOrAdmin")] // Chỉ Staff và Admin mới được cập nhật dịch vụ
        public async Task<IActionResult> UpdateService(int id, [FromBody] UpdateServiceRequest request)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID dịch vụ không hợp lệ" });

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return BadRequest(new { 
                        success = false, 
                        message = "Dữ liệu không hợp lệ", 
                        errors = errors 
                    });
                }

                var result = await _serviceService.UpdateServiceAsync(id, request);
                
                return Ok(new { 
                    success = true, 
                    message = "Cập nhật dịch vụ thành công",
                    data = result
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
