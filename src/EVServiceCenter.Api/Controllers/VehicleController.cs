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
    public class VehicleController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;

        public VehicleController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        /// <summary>
        /// Lấy danh sách xe với phân trang và tìm kiếm
        /// </summary>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="customerId">Lọc theo khách hàng</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách xe</returns>
        [HttpGet]
        public async Task<IActionResult> GetVehicles(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? customerId = null,
            [FromQuery] string searchTerm = null)
        {
            try
            {
                // Validate pagination parameters
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _vehicleService.GetVehiclesAsync(pageNumber, pageSize, customerId, searchTerm);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách xe thành công",
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
        /// Lấy thông tin xe theo ID
        /// </summary>
        /// <param name="id">ID xe</param>
        /// <returns>Thông tin xe</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVehicleById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID xe không hợp lệ" });

                var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy thông tin xe thành công",
                    data = vehicle
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
        /// Tạo xe mới
        /// </summary>
        /// <param name="request">Thông tin xe mới</param>
        /// <returns>Thông tin xe đã tạo</returns>
        [HttpPost]
        public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleRequest request)
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

                var vehicle = await _vehicleService.CreateVehicleAsync(request);
                
                return CreatedAtAction(nameof(GetVehicleById), new { id = vehicle.VehicleId }, new { 
                    success = true, 
                    message = "Tạo xe thành công",
                    data = vehicle
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
        /// Cập nhật thông tin xe
        /// </summary>
        /// <param name="id">ID xe</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Kết quả cập nhật</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVehicle(int id, [FromBody] UpdateVehicleRequest request)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID xe không hợp lệ" });

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { 
                        success = false, 
                        message = "Dữ liệu không hợp lệ", 
                        errors = errors 
                    });
                }

                var vehicle = await _vehicleService.UpdateVehicleAsync(id, request);
                
                return Ok(new { 
                    success = true, 
                    message = "Cập nhật xe thành công",
                    data = vehicle
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
