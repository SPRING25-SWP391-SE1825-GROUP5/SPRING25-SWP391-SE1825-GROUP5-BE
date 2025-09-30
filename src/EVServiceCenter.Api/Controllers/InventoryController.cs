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
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        /// <summary>
        /// Lấy danh sách tồn kho với phân trang và tìm kiếm
        /// </summary>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="centerId">Lọc theo trung tâm</param>
        /// <param name="partId">Lọc theo phụ tùng</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm (mã phụ tùng, tên, thương hiệu, tên trung tâm)</param>
        /// <returns>Danh sách tồn kho</returns>
        [HttpGet]
        public async Task<IActionResult> GetInventories(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? centerId = null,
            [FromQuery] int? partId = null,
            [FromQuery] string searchTerm = null)
        {
            try
            {
                // Validate pagination parameters
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _inventoryService.GetInventoriesAsync(pageNumber, pageSize, centerId, partId, searchTerm);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách tồn kho thành công",
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
        /// Tạo tồn kho mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateInventoryRequest request)
        {
            try
            {
                var created = await _inventoryService.CreateInventoryAsync(request);
                return Ok(new { success = true, data = created });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Lỗi khi tạo tồn kho" });
            }
        }

        /// <summary>
        /// Lấy tồn kho theo center và danh sách partIds
        /// </summary>
        [HttpGet("availability")]
        public async Task<IActionResult> GetAvailability([FromQuery] int centerId, [FromQuery] string partIds)
        {
            if (centerId <= 0 || string.IsNullOrWhiteSpace(partIds))
                return BadRequest(new { success = false, message = "centerId và partIds là bắt buộc" });

            var ids = partIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => int.TryParse(s, out var x) ? x : 0)
                              .Where(x => x > 0)
                              .ToList();
            if (ids.Count == 0) return BadRequest(new { success = false, message = "partIds không hợp lệ" });

            var result = await _inventoryService.GetAvailabilityAsync(centerId, ids);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin tồn kho theo ID
        /// </summary>
        /// <param name="id">ID tồn kho</param>
        /// <returns>Thông tin tồn kho</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInventoryById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID tồn kho không hợp lệ" });

                var inventory = await _inventoryService.GetInventoryByIdAsync(id);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy thông tin tồn kho thành công",
                    data = inventory
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
        /// Cập nhật tồn kho (chỉ ADMIN)
        /// </summary>
        /// <param name="id">ID tồn kho</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Kết quả cập nhật</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateInventory(int id, [FromBody] UpdateInventoryRequest request)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID tồn kho không hợp lệ" });

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { 
                        success = false, 
                        message = "Dữ liệu không hợp lệ", 
                        errors = errors 
                    });
                }

                var inventory = await _inventoryService.UpdateInventoryAsync(id, request);
                
                return Ok(new { 
                    success = true, 
                    message = "Cập nhật tồn kho thành công",
                    data = inventory
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
