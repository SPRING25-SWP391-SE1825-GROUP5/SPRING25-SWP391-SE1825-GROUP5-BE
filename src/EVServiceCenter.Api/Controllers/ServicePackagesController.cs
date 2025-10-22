using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using System.Collections.Generic;

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicePackagesController : ControllerBase
    {
        private readonly IServicePackageService _service;

        public ServicePackagesController(IServicePackageService service)
        {
            _service = service;
        }

        /// <summary>
        /// Danh sách gói dịch vụ (công khai)
        /// </summary>
        /// <param name="serviceId">Lọc theo ServiceId - khi có serviceId thì chỉ trả gói ACTIVE của service đó</param>
        /// <param name="activeOnly">Chỉ lấy gói đang hiệu lực</param>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] int? serviceId = null, [FromQuery] bool activeOnly = false)
        {
            try
            {
                // Yêu cầu: khi có serviceId thì chỉ trả gói đang hiệu lực của service đó (public)
                if (serviceId.HasValue)
                {
                    var active = await _service.GetActivePackagesAsync();
                    var filteredPackages = active.Where(p => p.ServiceId == serviceId.Value).ToList();
                    return Ok(new { 
                        success = true, 
                        message = $"Tìm thấy {filteredPackages.Count} gói dịch vụ cho service {serviceId}",
                        data = filteredPackages 
                    });
                }

                if (activeOnly)
                {
                    var active = await _service.GetActivePackagesAsync();
                    return Ok(new { 
                        success = true, 
                        message = $"Tìm thấy {active.Count()} gói dịch vụ đang hoạt động",
                        data = active 
                    });
                }

                var all = await _service.GetAllAsync();
                return Ok(new { 
                    success = true, 
                    message = $"Tìm thấy {all.Count()} gói dịch vụ",
                    data = all 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Danh sách tất cả gói dịch vụ đang hoạt động (công khai)
        /// </summary>
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllActive()
        {
            try
            {
                var active = await _service.GetActivePackagesAsync();
                return Ok(new {
                    success = true,
                    message = $"Tìm thấy {active.Count()} gói dịch vụ đang hoạt động",
                    data = active
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Chi tiết gói dịch vụ theo ID (công khai)
        /// </summary>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                if (id <= 0) return BadRequest(new { success = false, message = "ID gói không hợp lệ" });
                var pkg = await _service.GetByIdAsync(id);
                if (pkg == null) return NotFound(new { success = false, message = "Không tìm thấy gói" });
                return Ok(new { success = true, data = pkg });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Lấy gói theo mã (công khai)
        /// </summary>
        [HttpGet("code/{code}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByCode(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code)) return BadRequest(new { success = false, message = "Mã gói không hợp lệ" });
                var pkg = await _service.GetByPackageCodeAsync(code.Trim());
                if (pkg == null) return NotFound(new { success = false, message = "Không tìm thấy gói" });
                return Ok(new { success = true, data = pkg });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Tạo gói dịch vụ (STAFF/MANAGER/ADMIN)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "STAFF,MANAGER,ADMIN")]
        public async Task<IActionResult> Create([FromBody] CreateServicePackageRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors });
                }
                var created = await _service.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = created.PackageId }, new { success = true, data = created });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật gói dịch vụ (STAFF/MANAGER/ADMIN)
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "STAFF,MANAGER,ADMIN")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateServicePackageRequest request)
        {
            try
            {
                if (id <= 0) return BadRequest(new { success = false, message = "ID gói không hợp lệ" });
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors });
                }
                var updated = await _service.UpdateAsync(id, request);
                return Ok(new { success = true, data = updated });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Xoá gói dịch vụ (MANAGER/ADMIN)
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "MANAGER,ADMIN")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0) return BadRequest(new { success = false, message = "ID gói không hợp lệ" });
                await _service.DeleteAsync(id);
                return Ok(new { success = true, message = "Đã xoá gói" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}
