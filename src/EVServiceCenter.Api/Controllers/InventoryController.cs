using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace EVServiceCenter.Api.Controllers
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
        /// <param name="searchTerm">Từ khóa tìm kiếm (mã phụ tùng, tên, thương hiệu, tên trung tâm)</param>
        /// <returns>Danh sách tồn kho</returns>
        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetInventories(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? centerId = null,
            [FromQuery] string? searchTerm = null)
        {
            try
            {
                // Validate pagination parameters
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _inventoryService.GetInventoriesAsync(pageNumber, pageSize, centerId, searchTerm);
                
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
        /// Tạo tồn kho mới (chỉ MANAGER)
        /// </summary>
        /// <param name="request">Thông tin tồn kho mới</param>
        /// <returns>Thông tin tồn kho đã tạo</returns>
        [HttpPost]
        [Authorize(Roles = "MANAGER")]
        public async Task<IActionResult> CreateInventory([FromBody] CreateInventoryRequest request)
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

                var created = await _inventoryService.CreateInventoryAsync(request);
                
                return Ok(new { 
                    success = true, 
                    message = "Tạo tồn kho thành công",
                    data = created
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
        /// Lấy danh sách phụ tùng (parts) thuộc một inventory
        /// </summary>
        /// <param name="inventoryId">ID tồn kho</param>
        /// <returns>Danh sách phụ tùng của tồn kho</returns>
        [HttpGet("{inventoryId}/parts")]
        public async Task<IActionResult> GetInventoryParts(int inventoryId)
        {
            try
            {
                if (inventoryId <= 0)
                    return BadRequest(new { success = false, message = "ID tồn kho không hợp lệ" });

                var inventory = await _inventoryService.GetInventoryByIdAsync(inventoryId);
                var parts = inventory.InventoryParts;

                return Ok(new {
                    success = true,
                    message = "Lấy danh sách phụ tùng của tồn kho thành công",
                    data = parts
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Lấy thông tin tồn kho theo ID
        /// </summary>
        /// <param name="id">ID tồn kho</param>
        /// <returns>Thông tin tồn kho</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
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
        /// Lấy tồn kho theo trung tâm (1 trung tâm = 1 kho)
        /// </summary>
        /// <param name="centerId">ID trung tâm</param>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách tồn kho của trung tâm</returns>
        [HttpGet("by-center/{centerId}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<IActionResult> GetInventoryByCenter(int centerId)
        {
            try
            {
                if (centerId <= 0)
                    return BadRequest(new { success = false, message = "ID trung tâm không hợp lệ" });

                var inventory = await _inventoryService.GetInventoryByCenterIdAsync(centerId);
                return Ok(new { success = true, message = "Lấy tồn kho theo trung tâm thành công", data = inventory });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // ========== INVENTORY PART MANAGEMENT (Cấu trúc mới) ==========

        // Removed inventory-part management endpoints

        // ========== AVAILABILITY METHODS ==========

        /// <summary>
        /// Lấy tồn kho theo center và danh sách partIds
        /// </summary>
        /// <param name="centerId">ID trung tâm</param>
        /// <param name="partIds">Danh sách ID phụ tùng (cách nhau bởi dấu phẩy)</param>
        /// <returns>Danh sách tồn kho</returns>
        // Removed availability endpoint (per center + partIds)

        /// <summary>
        /// Lấy tồn kho toàn cục theo danh sách partIds
        /// </summary>
        /// <param name="partIds">Danh sách ID phụ tùng (cách nhau bởi dấu phẩy)</param>
        /// <returns>Danh sách tồn kho toàn cục</returns>
        // Removed global-availability endpoint

        /// <summary>
        /// Lấy tồn kho toàn cục cho tất cả phụ tùng
        /// </summary>
        /// <returns>Danh sách tồn kho toàn cục</returns>
        // Removed global-availability-all endpoint

        /// <summary>
        /// Lấy toàn bộ parts available theo từng center (phân trang, tìm kiếm)
        /// </summary>
        /// <param name="pageNumber">Số trang</param>
        /// <param name="pageSize">Kích thước trang</param>
        /// <param name="searchTerm">Từ khóa (tên trung tâm, mã/tên/brand)</param>
        [HttpGet("centers-availability")]
        [Authorize(Policy = "AuthenticatedUser")]
        public async Task<IActionResult> GetCentersAvailability(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? searchTerm = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 200) pageSize = 50;

                var result = await _inventoryService.GetInventoriesAsync(pageNumber, pageSize, null, searchTerm);
                return Ok(new { success = true, message = "Lấy parts theo từng center thành công", data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }

    // ========== REQUEST MODELS ==========

    public class AddPartToInventoryRequest
    {
        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "PartId phải > 0")] 
        public int PartId { get; set; }

        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "CurrentStock phải > 0")] 
        public int CurrentStock { get; set; }

        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "MinimumStock phải > 0")] 
        public int MinimumStock { get; set; }
    }

    public class UpdateInventoryPartRequest
    {
        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "CurrentStock phải > 0")] 
        public int CurrentStock { get; set; }

        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "MinimumStock phải > 0")] 
        public int MinimumStock { get; set; }
    }
}
