using System;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/maintenance-checklist-items")]
    public class MaintenanceChecklistItemController : ControllerBase
    {
        private readonly IMaintenanceChecklistItemService _itemService;

        public MaintenanceChecklistItemController(IMaintenanceChecklistItemService itemService)
        {
            _itemService = itemService;
        }

        /// <summary>
        /// Lấy danh sách tất cả mục kiểm tra
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllItems(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string searchTerm = null)
        {
            try
            {
                var result = await _itemService.GetAllItemsAsync(pageNumber, pageSize, searchTerm);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thông tin mục kiểm tra theo ID
        /// </summary>
        [HttpGet("{itemId:int}")]
        public async Task<IActionResult> GetItemById(int itemId)
        {
            try
            {
                var result = await _itemService.GetItemByIdAsync(itemId);
                return Ok(new { success = true, data = result });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy template mục kiểm tra theo dịch vụ
        /// </summary>
        [HttpGet("service/{serviceId:int}")]
        public async Task<IActionResult> GetTemplateByServiceId(int serviceId)
        {
            try
            {
                var result = await _itemService.GetTemplateByServiceIdAsync(serviceId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Tạo mục kiểm tra mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateItem([FromBody] CreateMaintenanceChecklistItemRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
                }

                var result = await _itemService.CreateItemAsync(request);
                return CreatedAtAction(nameof(GetItemById), new { itemId = result.ItemId }, 
                    new { success = true, data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật mục kiểm tra
        /// </summary>
        [HttpPut("{itemId:int}")]
        public async Task<IActionResult> UpdateItem(int itemId, [FromBody] UpdateMaintenanceChecklistItemRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
                }

                var result = await _itemService.UpdateItemAsync(itemId, request);
                return Ok(new { success = true, data = result });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa mục kiểm tra
        /// </summary>
        [HttpDelete("{itemId:int}")]
        public async Task<IActionResult> DeleteItem(int itemId)
        {
            try
            {
                var result = await _itemService.DeleteItemAsync(itemId);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Mục kiểm tra không tồn tại" });
                }

                return Ok(new { success = true, message = "Xóa mục kiểm tra thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}





