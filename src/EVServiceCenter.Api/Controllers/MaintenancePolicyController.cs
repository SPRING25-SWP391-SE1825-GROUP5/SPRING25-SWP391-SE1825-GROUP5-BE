using System;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/maintenance-policies")]
    public class MaintenancePolicyController : ControllerBase
    {
        private readonly IMaintenancePolicyService _policyService;

        public MaintenancePolicyController(IMaintenancePolicyService policyService)
        {
            _policyService = policyService;
        }

        /// <summary>
        /// Lấy danh sách tất cả chính sách bảo trì
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllPolicies(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string searchTerm = null,
            [FromQuery] int? serviceId = null)
        {
            try
            {
                var result = await _policyService.GetAllPoliciesAsync(pageNumber, pageSize, searchTerm, serviceId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thông tin chính sách bảo trì theo ID
        /// </summary>
        [HttpGet("{policyId:int}")]
        public async Task<IActionResult> GetPolicyById(int policyId)
        {
            try
            {
                var result = await _policyService.GetPolicyByIdAsync(policyId);
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
        /// Lấy danh sách chính sách bảo trì đang hoạt động
        /// </summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActivePolicies(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string searchTerm = null,
            [FromQuery] int? serviceId = null)
        {
            try
            {
                var result = await _policyService.GetActivePoliciesAsync(pageNumber, pageSize, searchTerm, serviceId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy chính sách bảo trì theo dịch vụ
        /// </summary>
        [HttpGet("service/{serviceId:int}")]
        public async Task<IActionResult> GetPoliciesByServiceId(int serviceId)
        {
            try
            {
                var result = await _policyService.GetPoliciesByServiceIdAsync(serviceId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Tạo chính sách bảo trì mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreatePolicy([FromBody] CreateMaintenancePolicyRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
                }

                var result = await _policyService.CreatePolicyAsync(request);
                return CreatedAtAction(nameof(GetPolicyById), new { policyId = result.PolicyId }, 
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
        /// Cập nhật chính sách bảo trì
        /// </summary>
        [HttpPut("{policyId:int}")]
        public async Task<IActionResult> UpdatePolicy(int policyId, [FromBody] UpdateMaintenancePolicyRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
                }

                var result = await _policyService.UpdatePolicyAsync(policyId, request);
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
        /// Thay đổi trạng thái hoạt động của chính sách bảo trì
        /// </summary>
        [HttpPatch("{policyId:int}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int policyId)
        {
            try
            {
                var result = await _policyService.ToggleActiveAsync(policyId);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Chính sách bảo trì không tồn tại" });
                }

                return Ok(new { success = true, message = "Thay đổi trạng thái thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa chính sách bảo trì
        /// </summary>
        [HttpDelete("{policyId:int}")]
        public async Task<IActionResult> DeletePolicy(int policyId)
        {
            try
            {
                var result = await _policyService.DeletePolicyAsync(policyId);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Chính sách bảo trì không tồn tại" });
                }

                return Ok(new { success = true, message = "Xóa chính sách bảo trì thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}

