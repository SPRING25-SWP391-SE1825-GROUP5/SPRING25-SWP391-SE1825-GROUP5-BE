using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AuthenticatedUser")]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly IVehicleService _vehicleService;
        private readonly ICustomerServiceCreditService _customerServiceCreditService;
        private readonly ICustomerRepository _customerRepository;

        public CustomerController(ICustomerService customerService, IVehicleService vehicleService, ICustomerServiceCreditService customerServiceCreditService, ICustomerRepository customerRepository)
        {
            _customerService = customerService;
            _vehicleService = vehicleService;
            _customerServiceCreditService = customerServiceCreditService;
            _customerRepository = customerRepository;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentCustomer()
        {
            try
            {
                var userId = GetCurrentUserId();
                Console.WriteLine($"GetCurrentCustomer API called, userId: {userId}");
                
                if (userId == null)
                {
                    Console.WriteLine("UserId is null, returning Unauthorized");
                    return Unauthorized(new { success = false, message = "Không thể xác định người dùng" });
                }

                Console.WriteLine($"Calling GetCurrentCustomerAsync with userId: {userId}");
                var customer = await _customerService.GetCurrentCustomerAsync(userId.Value);
                
                Console.WriteLine($"GetCurrentCustomerAsync completed successfully");
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

        /// <summary>
        /// Lấy lịch sử booking của khách hàng
        /// </summary>
        [HttpGet("{customerId}/bookings")]
        [Authorize(Roles = "ADMIN,STAFF,TECHNICIAN,CUSTOMER")]
        public async Task<IActionResult> GetCustomerBookings(int customerId)
        {
            try
            {
                if (customerId <= 0)
                    return BadRequest(new { success = false, message = "CustomerId không hợp lệ" });

                var data = await _customerService.GetCustomerBookingsAsync(customerId);
                return Ok(new { success = true, message = "Lấy lịch sử booking thành công", data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }


        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                            ?? User.FindFirst("userId")?.Value 
                            ?? User.FindFirst("sub")?.Value 
                            ?? User.FindFirst("nameid")?.Value;
                            
            Console.WriteLine($"Looking for userId in claims. Found: {userIdClaim}");
            Console.WriteLine($"All claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
            
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return null;
        }
    }
}