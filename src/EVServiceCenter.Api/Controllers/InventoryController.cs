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
    [Authorize(Policy = "AuthenticatedUser")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

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


    }

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