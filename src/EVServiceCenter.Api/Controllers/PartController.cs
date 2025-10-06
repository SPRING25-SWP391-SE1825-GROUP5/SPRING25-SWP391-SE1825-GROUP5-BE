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
    public class PartController : ControllerBase
    {
        private readonly IPartService _partService;
        private readonly IInventoryService _inventoryService;

        public PartController(IPartService partService, IInventoryService inventoryService)
        {
            _partService = partService;
            _inventoryService = inventoryService;
        }

        /// <summary>
        /// Lấy danh sách tất cả phụ tùng với phân trang và tìm kiếm
        /// </summary>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm (mã, tên, thương hiệu)</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động (true/false/null = all)</param>
        /// <returns>Danh sách phụ tùng</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllParts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string searchTerm = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                // Validate pagination parameters
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _partService.GetAllPartsAsync(pageNumber, pageSize, searchTerm, isActive);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách phụ tùng thành công",
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
        /// Tồn kho tổng hợp toàn hệ thống cho danh sách phụ tùng
        /// </summary>
        [HttpGet("availability")]
        public async Task<IActionResult> GetGlobalAvailability([FromQuery] string partIds)
        {
            // Nếu không truyền partIds => trả toàn bộ các phụ tùng còn hàng (Get All)
            if (string.IsNullOrWhiteSpace(partIds))
            {
                var all = await _inventoryService.GetGlobalAvailabilityAllAsync();
                return Ok(new { success = true, data = all });
            }

            var ids = partIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => int.TryParse(s, out var x) ? x : 0)
                              .Where(x => x > 0)
                              .ToList();
            if (ids.Count == 0)
                return BadRequest(new { success = false, message = "partIds không hợp lệ" });

            var result = await _inventoryService.GetGlobalAvailabilityAsync(ids);
            var inStock = result.Where(r => r.TotalStock > 0).ToList();
            return Ok(new { success = true, data = inStock });
        }

        /// <summary>
        /// Cập nhật phụ tùng
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePartRequest request)
        {
            try
            {
                var updated = await _partService.UpdatePartAsync(id, request);
                return Ok(new { success = true, data = updated });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception)
            {
                return BadRequest(new { success = false, message = "Lỗi khi cập nhật phụ tùng" });
            }
        }

        /// <summary>
        /// Lấy thông tin phụ tùng theo ID
        /// </summary>
        /// <param name="id">ID phụ tùng</param>
        /// <returns>Thông tin phụ tùng</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPartById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID phụ tùng không hợp lệ" });

                var part = await _partService.GetPartByIdAsync(id);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy thông tin phụ tùng thành công",
                    data = part
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
        /// Tạo phụ tùng mới (chỉ ADMIN)
        /// </summary>
        /// <param name="request">Thông tin phụ tùng mới</param>
        /// <returns>Thông tin phụ tùng đã tạo</returns>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreatePart([FromBody] CreatePartRequest request)
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

                var part = await _partService.CreatePartAsync(request);
                
                return CreatedAtAction(nameof(GetPartById), new { id = part.PartId }, new { 
                    success = true, 
                    message = "Tạo phụ tùng thành công",
                    data = part
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
        /// Lấy danh sách dịch vụ tương thích với phụ tùng
        /// </summary>
        /// <param name="id">ID phụ tùng</param>
        /// <returns>Danh sách dịch vụ tương thích</returns>
        [HttpGet("{id}/services")]
        public async Task<IActionResult> GetServicesByPartId(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID phụ tùng không hợp lệ" });

                var services = await _partService.GetServicesByPartIdAsync(id);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách dịch vụ tương thích thành công",
                    data = services
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
    }
}
