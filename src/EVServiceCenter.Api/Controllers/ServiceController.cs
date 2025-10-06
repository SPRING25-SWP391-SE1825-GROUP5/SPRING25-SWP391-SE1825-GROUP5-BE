using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AuthenticatedUser")] // Tất cả user đã đăng nhập đều có thể xem
    public class ServiceController : ControllerBase
    {
        private readonly IServiceService _serviceService;
        private readonly IServicePartRepository _servicePartRepo;

        public ServiceController(IServiceService serviceService, IServicePartRepository servicePartRepo)
        {
            _serviceService = serviceService;
            _servicePartRepo = servicePartRepo;
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

        /// <summary>
        /// Kích hoạt/Vô hiệu hóa dịch vụ
        /// </summary>
        /// <param name="id">ID dịch vụ</param>
        /// <returns>Kết quả thay đổi trạng thái</returns>
        [HttpPatch("{id}/toggle-active")]
        [Authorize(Policy = "StaffOrAdmin")] // Chỉ Staff và Admin mới được thay đổi trạng thái
        public async Task<IActionResult> ToggleActiveService(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID dịch vụ không hợp lệ" });

                var result = await _serviceService.ToggleActiveAsync(id);
                
                if (!result)
                    return NotFound(new { success = false, message = "Không tìm thấy dịch vụ" });

                return Ok(new { 
                    success = true, 
                    message = "Thay đổi trạng thái dịch vụ thành công"
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

        // ========== SERVICE PARTS MANAGEMENT ==========

        /// <summary>
        /// Lấy danh sách phụ tùng của dịch vụ
        /// </summary>
        /// <param name="serviceId">ID dịch vụ</param>
        /// <returns>Danh sách phụ tùng</returns>
        [HttpGet("{serviceId}/parts")]
        public async Task<IActionResult> GetServiceParts(int serviceId)
        {
            try
            {
                if (serviceId <= 0)
                    return BadRequest(new { success = false, message = "ID dịch vụ không hợp lệ" });

                var items = await _servicePartRepo.GetByServiceIdAsync(serviceId);
                var result = items.Select(x => new
                {
                    partId = x.PartId,
                    partName = x.Part?.PartName,
                    notes = x.Notes
                });
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách phụ tùng dịch vụ thành công",
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
        /// Thay thế toàn bộ phụ tùng của dịch vụ
        /// </summary>
        /// <param name="serviceId">ID dịch vụ</param>
        /// <param name="request">Danh sách phụ tùng mới</param>
        /// <returns>Kết quả thay thế</returns>
        [HttpPut("{serviceId}/parts")]
        [Authorize(Policy = "StaffOrAdmin")] // Chỉ Staff và Admin mới được quản lý phụ tùng
        public async Task<IActionResult> ReplaceServiceParts(int serviceId, [FromBody] ServicePartsReplaceRequest request)
        {
            try
            {
                if (serviceId <= 0)
                    return BadRequest(new { success = false, message = "ID dịch vụ không hợp lệ" });

                var toSave = (request.Parts ?? new List<ServicePartsReplaceRequest.Item>())
                    .DistinctBy(p => p.PartId)
                    .Select(p => new ServicePart { ServiceId = serviceId, PartId = p.PartId, Notes = p.Notes });

                await _servicePartRepo.ReplaceForServiceAsync(serviceId, toSave);
                
                return Ok(new { 
                    success = true, 
                    message = "Thay thế phụ tùng dịch vụ thành công" 
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
        /// Thêm phụ tùng vào dịch vụ
        /// </summary>
        /// <param name="serviceId">ID dịch vụ</param>
        /// <param name="request">Thông tin phụ tùng</param>
        /// <returns>Kết quả thêm</returns>
        [HttpPost("{serviceId}/parts")]
        [Authorize(Policy = "StaffOrAdmin")] // Chỉ Staff và Admin mới được quản lý phụ tùng
        public async Task<IActionResult> AddServicePart(int serviceId, [FromBody] ServicePartAddRequest request)
        {
            try
            {
                if (serviceId <= 0)
                    return BadRequest(new { success = false, message = "ID dịch vụ không hợp lệ" });

                if (request.PartId <= 0)
                    return BadRequest(new { success = false, message = "ID phụ tùng không hợp lệ" });

                await _servicePartRepo.AddAsync(new ServicePart { 
                    ServiceId = serviceId, 
                    PartId = request.PartId, 
                    Notes = request.Notes 
                });
                
                return Ok(new { 
                    success = true, 
                    message = "Thêm phụ tùng vào dịch vụ thành công" 
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
        /// Xóa phụ tùng khỏi dịch vụ
        /// </summary>
        /// <param name="serviceId">ID dịch vụ</param>
        /// <param name="partId">ID phụ tùng</param>
        /// <returns>Kết quả xóa</returns>
        [HttpDelete("{serviceId}/parts/{partId}")]
        [Authorize(Policy = "StaffOrAdmin")] // Chỉ Staff và Admin mới được quản lý phụ tùng
        public async Task<IActionResult> DeleteServicePart(int serviceId, int partId)
        {
            try
            {
                if (serviceId <= 0)
                    return BadRequest(new { success = false, message = "ID dịch vụ không hợp lệ" });

                if (partId <= 0)
                    return BadRequest(new { success = false, message = "ID phụ tùng không hợp lệ" });

                await _servicePartRepo.DeleteAsync(serviceId, partId);
                
                return Ok(new { 
                    success = true, 
                    message = "Xóa phụ tùng khỏi dịch vụ thành công" 
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

        // ========== REQUEST MODELS ==========

        public class ServicePartsReplaceRequest
        {
            public List<Item> Parts { get; set; }
            public class Item
            {
                public int PartId { get; set; }
                public string Notes { get; set; }
            }
        }

        public class ServicePartAddRequest 
        { 
            public int PartId { get; set; } 
            public string Notes { get; set; } 
        }
    }
}
