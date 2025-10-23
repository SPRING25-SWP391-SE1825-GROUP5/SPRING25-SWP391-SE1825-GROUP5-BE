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
        private readonly ICustomerServiceCreditService _customerServiceCreditService;

        public CustomerController(ICustomerService customerService, IVehicleService vehicleService, ICustomerServiceCreditService customerServiceCreditService)
        {
            _customerService = customerService;
            _vehicleService = vehicleService;
            _customerServiceCreditService = customerServiceCreditService;
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
        /// Liệt kê các gói dịch vụ khách đã mua (credits)
        /// </summary>
        /// <param name="customerId">ID khách hàng</param>
        [HttpGet("{customerId}/credits")]
        public async Task<IActionResult> GetCustomerCredits(int customerId)
        {
            try
            {
                if (customerId <= 0)
                    return BadRequest(new { success = false, message = "ID khách hàng không hợp lệ" });

                var credits = await _customerServiceCreditService.GetByCustomerIdAsync(customerId);
                var creditsList = credits.ToList();
                
                return Ok(new { 
                    success = true, 
                    message = $"Tìm thấy {creditsList.Count} gói dịch vụ đã mua",
                    data = creditsList 
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

        /// <summary>
        /// Lấy danh sách service package của khách hàng hiện tại
        /// </summary>
        /// <returns>Danh sách service package đã mua</returns>
        [HttpGet("service-packages")]
        public async Task<IActionResult> GetCustomerServicePackages()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định người dùng" });

                var servicePackages = await _customerServiceCreditService.GetCustomerServicePackagesAsync(userId.Value);
                
                return Ok(new { 
                    success = true, 
                    message = $"Tìm thấy {servicePackages.Count()} service package",
                    data = servicePackages
                });
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
        /// Lấy chi tiết service package theo ID
        /// </summary>
        /// <param name="packageId">ID của service package</param>
        /// <returns>Chi tiết service package</returns>
        [HttpGet("service-packages/{packageId:int}")]
        public async Task<IActionResult> GetServicePackageDetail(int packageId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định người dùng" });

                var servicePackage = await _customerServiceCreditService.GetServicePackageDetailAsync(packageId, userId.Value);
                
                if (servicePackage == null)
                    return NotFound(new { success = false, message = "Không tìm thấy service package hoặc bạn không có quyền truy cập" });

                return Ok(new { 
                    success = true, 
                    message = "Lấy chi tiết service package thành công",
                    data = servicePackage
                });
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
        /// Lấy lịch sử sử dụng service package
        /// </summary>
        /// <param name="packageId">ID của service package</param>
        /// <returns>Lịch sử sử dụng</returns>
        [HttpGet("service-packages/{packageId:int}/usage-history")]
        public async Task<IActionResult> GetServicePackageUsageHistory(int packageId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định người dùng" });

                var usageHistory = await _customerServiceCreditService.GetServicePackageUsageHistoryAsync(packageId, userId.Value);
                
                return Ok(new { 
                    success = true, 
                    message = $"Tìm thấy {usageHistory.Count()} lần sử dụng",
                    data = usageHistory
                });
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
        /// Lấy thống kê service package của khách hàng
        /// </summary>
        /// <returns>Thống kê tổng quan</returns>
        [HttpGet("service-packages/statistics")]
        public async Task<IActionResult> GetServicePackageStatistics()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định người dùng" });

                var statistics = await _customerServiceCreditService.GetCustomerServicePackageStatisticsAsync(userId.Value);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy thống kê service package thành công",
                    data = statistics
                });
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

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return null;
        }
    }
}
