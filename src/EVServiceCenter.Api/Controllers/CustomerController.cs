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
        [Authorize(Roles = "CUSTOMER")]
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

        /// <summary>
        /// Search customers by name, email, or phone (partial match)
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>List of matching customers</returns>
        [HttpGet("search")]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> SearchCustomers(
            [FromQuery] string query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Query không được rỗng"
                    });
                }

                if (query.Length < 2)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Query phải có ít nhất 2 ký tự"
                    });
                }

                var normalizedQuery = query.Trim().ToLower();

                // Get all customers with user info
                var allCustomers = await _customerRepository.GetAllCustomersAsync();
                
                // Filter by partial match
                var matchingCustomers = allCustomers
                    .Where(c => c.User != null && (
                        (c.User.FullName != null && c.User.FullName.ToLower().Contains(normalizedQuery)) ||
                        (c.User.Email != null && c.User.Email.ToLower().Contains(normalizedQuery)) ||
                        (c.User.PhoneNumber != null && c.User.PhoneNumber.Contains(query.Trim()))
                    ))
                    .OrderBy(c => c.User!.FullName)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new
                    {
                        customerId = c.CustomerId,
                        userId = c.UserId,
                        userFullName = c.User!.FullName,
                        userEmail = c.User!.Email,
                        userPhoneNumber = c.User!.PhoneNumber,
                        isGuest = c.IsGuest,
                        vehicleCount = c.Vehicles?.Count ?? 0
                    })
                    .ToList();

                var totalCount = allCustomers
                    .Count(c => c.User != null && (
                        (c.User.FullName != null && c.User.FullName.ToLower().Contains(normalizedQuery)) ||
                        (c.User.Email != null && c.User.Email.ToLower().Contains(normalizedQuery)) ||
                        (c.User.PhoneNumber != null && c.User.PhoneNumber.Contains(query.Trim()))
                    ));

                return Ok(new
                {
                    success = true,
                    data = matchingCustomers,
                    total = totalCount,
                    message = matchingCustomers.Any()
                        ? $"Tìm thấy {totalCount} khách hàng"
                        : "Không tìm thấy khách hàng"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching customers: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi tìm kiếm khách hàng: " + ex.Message
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
        public async Task<IActionResult> GetCustomerBookings(int customerId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (customerId <= 0)
                    return BadRequest(new { success = false, message = "CustomerId không hợp lệ" });

                // Validate pagination
                pageNumber = Math.Max(1, pageNumber);
                pageSize = Math.Min(Math.Max(1, pageSize), 1000);

                var data = await _customerService.GetCustomerBookingsAsync(customerId, pageNumber, pageSize);
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
