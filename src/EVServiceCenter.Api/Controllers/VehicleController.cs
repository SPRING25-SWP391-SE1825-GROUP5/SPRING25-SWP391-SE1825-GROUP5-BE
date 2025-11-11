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
    [Authorize(Policy = "AuthenticatedUser")]
    public class VehicleController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;
        private readonly EVServiceCenter.Domain.Interfaces.IVehicleRepository _vehicleRepository;
        private readonly EVServiceCenter.Domain.Interfaces.ICustomerRepository _customerRepository;

        public VehicleController(IVehicleService vehicleService, EVServiceCenter.Domain.Interfaces.IVehicleRepository vehicleRepository, EVServiceCenter.Domain.Interfaces.ICustomerRepository customerRepository)
        {
            _vehicleService = vehicleService;
            _vehicleRepository = vehicleRepository;
            _customerRepository = customerRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetVehicles(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? customerId = null,
            [FromQuery] string? searchTerm = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var isAdmin = User.IsInRole("ADMIN") || User.IsInRole("MANAGER");
                var isCustomer = User.IsInRole("CUSTOMER");

                if (isCustomer)
                {
                    var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "nameid" || c.Type == "userId");
                    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Unauthorized(new { success = false, message = "Không thể xác định thông tin người dùng" });
                    }

                    var customer = await _customerRepository.GetCustomerByUserIdAsync(userId);
                    if (customer == null)
                    {
                        return NotFound(new { success = false, message = "Không tìm thấy thông tin khách hàng" });
                    }

                    customerId = customer.CustomerId;
                }
                else if (!isAdmin)
                {
                    return Forbid();
                }

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

        public class UpdateMileageRequest { public int CurrentMileage { get; set; } }

        [HttpPost("{vehicleId:int}/mileage")]
        public async Task<IActionResult> UpdateMileage(int vehicleId, [FromBody] UpdateMileageRequest req)
        {
            try
            {
                if (req == null || req.CurrentMileage < 0)
                    return BadRequest(new { success = false, message = "CurrentMileage không hợp lệ" });
                var v = await _vehicleRepository.GetVehicleByIdAsync(vehicleId);
                if (v == null) return NotFound(new { success = false, message = "Không tìm thấy xe" });
                v.CurrentMileage = req.CurrentMileage;
                await _vehicleRepository.UpdateVehicleAsync(v);
                return Ok(new { success = true, vehicleId, currentMileage = v.CurrentMileage });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

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

                var vehicle = await _vehicleService.CreateVehicleAsync(request!);
                
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

        [HttpGet("{id}/customer")]
        public async Task<IActionResult> GetCustomerByVehicleId(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID xe không hợp lệ" });

                var customer = await _vehicleService.GetCustomerByVehicleIdAsync(id);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy thông tin khách hàng thành công",
                    data = customer
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

        [HttpGet("search/{vinOrLicensePlate}")]
        public async Task<IActionResult> GetVehicleByVinOrLicensePlate(string vinOrLicensePlate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(vinOrLicensePlate))
                    return BadRequest(new { success = false, message = "VIN hoặc biển số xe không được để trống" });

                var vehicle = await _vehicleService.GetVehicleByVinOrLicensePlateAsync(vinOrLicensePlate);
                
                return Ok(new { 
                    success = true, 
                    message = "Tìm xe thành công",
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

    }
}
