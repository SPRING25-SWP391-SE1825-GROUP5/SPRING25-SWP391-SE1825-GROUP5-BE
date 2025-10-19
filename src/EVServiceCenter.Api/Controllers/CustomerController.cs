using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AuthenticatedUser")] // Tất cả user đã đăng nhập
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly IVehicleService _vehicleService;

        public CustomerController(ICustomerService customerService, IVehicleService vehicleService)
        {
            _customerService = customerService;
            _vehicleService = vehicleService;
        }

        /// <summary>
        /// Lấy thông tin khách hàng hiện tại (map từ User)
        /// </summary>
        /// <returns>Thông tin khách hàng hiện tại</returns>
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentCustomer()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định người dùng" });

                var customer = await _customerService.GetCurrentCustomerAsync(userId.Value);
                
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

        // [Removed] POST /api/Customer tạo theo phone/isGuest đã bị loại bỏ để tránh trùng chức năng với quick-create.

        /// <summary>
        /// Lấy danh sách phương tiện của khách hàng
        /// </summary>
        /// <param name="id">ID khách hàng</param>
        /// <param name="pageNumber">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 10)</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách phương tiện của khách hàng</returns>
        [HttpGet("{id}/vehicles")]
        public async Task<IActionResult> GetCustomerVehicles(
            int id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID khách hàng không hợp lệ" });

                // Validate pagination parameters
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _vehicleService.GetVehiclesAsync(pageNumber, pageSize, id, searchTerm);
                
                return Ok(new { 
                    success = true, 
                    message = $"Lấy danh sách phương tiện của khách hàng {id} thành công",
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
        /// Lấy thông tin chi tiết xe của khách hàng
        /// </summary>
        /// <param name="customerId">ID khách hàng</param>
        /// <param name="vehicleId">ID xe</param>
        /// <returns>Thông tin chi tiết xe của khách hàng</returns>
        [HttpGet("{customerId}/vehicles/{vehicleId}")]
        public async Task<IActionResult> GetCustomerVehicleDetail(int customerId, int vehicleId)
        {
            try
            {
                if (customerId <= 0)
                    return BadRequest(new { success = false, message = "ID khách hàng không hợp lệ" });
                
                if (vehicleId <= 0)
                    return BadRequest(new { success = false, message = "ID xe không hợp lệ" });

                // Lấy thông tin xe
                var vehicle = await _vehicleService.GetVehicleByIdAsync(vehicleId);
                if (vehicle == null)
                    return NotFound(new { success = false, message = "Không tìm thấy xe" });

                // Kiểm tra xe có thuộc về customer không
                if (vehicle.CustomerId != customerId)
                    return Forbid("Xe không thuộc về khách hàng này");

                return Ok(new { 
                    success = true, 
                    message = $"Lấy thông tin chi tiết xe {vehicleId} của khách hàng {customerId} thành công",
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

        // [Removed] PUT /api/Customer/{id} cập nhật nhanh theo phone/isGuest đã bị loại bỏ để đơn giản hóa API.

        /// <summary>
        /// STAFF/TECHNICIAN/ADMIN tạo nhanh tài khoản CUSTOMER với 3 trường cơ bản
        /// </summary>
        [HttpPost("quick-create")]
        [Authorize(Roles = "STAFF,TECHNICIAN,MANAGER,ADMIN")]
        public async Task<IActionResult> QuickCreateCustomer([FromBody] QuickCreateCustomerRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new {
                        success = false,
                        message = "Dữ liệu không hợp lệ",
                        errors
                    });
                }

                var customer = await _customerService.QuickCreateCustomerAsync(request);
                return StatusCode(201, new {
                    success = true,
                    message = "Tạo tài khoản khách hàng thành công",
                    data = customer
                });
            }
            catch (ArgumentException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return null;
        }
    }
}
