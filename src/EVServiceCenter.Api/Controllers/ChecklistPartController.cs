using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/checklist-parts")]
    public class ChecklistPartController : ControllerBase
    {
        private readonly IChecklistPartService _checklistPartService; // May be deprecated if ServiceParts removed

        public ChecklistPartController(IChecklistPartService checklistPartService)
        {
            _checklistPartService = checklistPartService;
        }

        /// <summary>
        /// Lấy danh sách Parts theo Service ID
        /// </summary>
        [HttpGet("service/{serviceId:int}")]
        public async Task<IActionResult> GetPartsByServiceId(int serviceId)
        {
            try
            {
                var result = await _checklistPartService.GetPartsByServiceIdAsync(serviceId);
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
        /// Thêm Parts vào Checklist
        /// </summary>
        [HttpPost("add-parts")]
        public async Task<IActionResult> AddPartsToChecklist([FromBody] AddPartsToChecklistRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
                }

                var result = await _checklistPartService.AddPartsToChecklistAsync(request);
                return Ok(new { success = true, data = result });
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
        /// Xóa Parts khỏi Checklist
        /// </summary>
        [HttpPost("remove-parts")]
        public async Task<IActionResult> RemovePartsFromChecklist([FromBody] RemovePartsFromChecklistRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
                }

                var result = await _checklistPartService.RemovePartsFromChecklistAsync(request);
                return Ok(new { success = true, data = result });
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
    }
}

