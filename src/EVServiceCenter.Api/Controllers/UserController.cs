using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using EVServiceCenter.Application.Configurations;
using System.Text;
using System.IO;
#pragma warning disable CS1591

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAccountService _accountService;
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;
        private readonly IOptions<ExportOptions> _exportOptions;
        private readonly EVServiceCenter.Domain.Interfaces.ICustomerRepository _customerRepository;

        public UserController(IUserService userService, IAccountService accountService, IAuthService authService, IEmailService emailService, IOptions<ExportOptions> exportOptions, EVServiceCenter.Domain.Interfaces.ICustomerRepository customerRepository)
        {
            _userService = userService;
            _accountService = accountService;
            _authService = authService;
            _emailService = emailService;
            _exportOptions = exportOptions;
            _customerRepository = customerRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? role = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _userService.GetAllUsersAsync(pageNumber, pageSize, searchTerm, role);

                return Ok(new {
                    success = true,
                    message = "Lấy danh sách người dùng thành công",
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

        [HttpGet("export")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> ExportUsers()
        {
            try
            {
                var opts = _exportOptions.Value;
                var total = await _userService.GetUsersCountAsync();
                if (total > opts.MaxRecords)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Số bản ghi ({total}) vượt quá giới hạn cho phép ({opts.MaxRecords}). Vui lòng thu hẹp bộ lọc."
                    });
                }

                var users = await _userService.GetUsersForExportAsync(null, null, opts.MaxRecords, null, null, null, null);

                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var bytes = GenerateXlsx(users, opts.DateFormat);
                var fileName = $"users_{timestamp}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        private static byte[] GenerateXlsx(System.Collections.Generic.IList<EVServiceCenter.Application.Models.Responses.UserResponse> users, string dateFormat, object? filters = null)
        {
            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.AddWorksheet("Users");

            var headers = new[] { "UserId", "FullName", "Email", "PhoneNumber", "Role", "IsActive", "EmailVerified", "CreatedAt", "UpdatedAt" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
            }
            ws.SheetView.FreezeRows(1);

            int r = 2;
            foreach (var u in users)
            {
                ws.Cell(r, 1).Value = u.UserId;
                ws.Cell(r, 2).Value = u.FullName ?? string.Empty;
                ws.Cell(r, 3).Value = u.Email ?? string.Empty;
                ws.Cell(r, 4).Value = u.PhoneNumber ?? string.Empty;
                ws.Cell(r, 5).Value = u.Role ?? string.Empty;
                ws.Cell(r, 6).Value = u.IsActive ? "TRUE" : "FALSE";
                ws.Cell(r, 7).Value = u.EmailVerified ? "TRUE" : "FALSE";
                ws.Cell(r, 8).Value = u.CreatedAt;
                ws.Cell(r, 9).Value = u.UpdatedAt;
                r++;
            }

            int lastRow = r - 1;
            int lastCol = headers.Length;

            ws.Range(2, 8, lastRow, 9).Style.DateFormat.Format = dateFormat;

            ws.Range(2, 6, lastRow, 7).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            ws.Range(2, 1, lastRow, lastCol).Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;

            var tableRange = ws.Range(1, 1, lastRow, lastCol);
            var table = tableRange.CreateTable();
            table.Theme = ClosedXML.Excel.XLTableTheme.TableStyleMedium9;
            table.ShowAutoFilter = true;

            ws.Range(1, 1, lastRow, lastCol).Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
            ws.Range(1, 1, lastRow, lastCol).Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

            ws.Columns().AdjustToContents();

            var wsFilters = wb.AddWorksheet("Filters");
            wsFilters.Cell(1, 1).Value = "Applied Filters";
            wsFilters.Cell(1, 1).Style.Font.Bold = true;
            int fr = 3;
            void WriteFilter(string key, string? value)
            {
                wsFilters.Cell(fr, 1).Value = key;
                wsFilters.Cell(fr, 2).Value = value ?? string.Empty;
                fr++;
            }
            var dict = new System.Collections.Generic.Dictionary<string, string?>();
            if (filters != null)
            {
                foreach (var prop in filters.GetType().GetProperties())
                {
                    var val = prop.GetValue(filters);
                    dict[prop.Name] = val switch
                    {
                        DateTime dt => dt.ToString(dateFormat),
                        bool b => b ? "TRUE" : "FALSE",
                        _ => val?.ToString()
                    };
                }
            }
            foreach (var kv in dict)
            {
                WriteFilter(kv.Key, kv.Value);
            }
            wsFilters.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
        [HttpGet("find-by-email-or-phone")]
        [Authorize(Roles = "ADMIN,STAFF,MANAGER")]
        public async Task<IActionResult> FindByEmailOrPhone([FromQuery] string? email = null, [FromQuery] string? phone = null)
        {
            try
            {
                var hasEmail = !string.IsNullOrWhiteSpace(email);
                var hasPhone = !string.IsNullOrWhiteSpace(phone);

                if (!hasEmail && !hasPhone)
                    return BadRequest(new { success = false, message = "Vui lòng truyền đúng 1 trường: email hoặc phone" });
                if (hasEmail && hasPhone)
                    return BadRequest(new { success = false, message = "Chỉ được truyền 1 trường: email hoặc phone, không được đồng thời cả hai" });

                Domain.Entities.User? user = null;
                if (hasEmail)
                {
                    var trimmed = email?.Trim() ?? string.Empty;
                    if (!System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
                        return BadRequest(new { success = false, message = "Email không hợp lệ" });
                    user = await _accountService.GetAccountByEmailAsync(trimmed);
                }
                else
                {
                    var trimmed = phone?.Trim() ?? string.Empty;
                    if (!System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^0\d{9}$"))
                        return BadRequest(new { success = false, message = "Số điện thoại không hợp lệ (bắt đầu bằng 0 và đủ 10 số)" });
                    user = await _accountService.GetAccountByPhoneNumberAsync(trimmed);
                }

                if (user == null)
                    return NotFound(new { success = false, message = "Không tìm thấy người dùng" });

                int? customerId = null;
                try
                {
                    var customer = await _customerRepository.GetCustomerByUserIdAsync(user.UserId);
                    if (customer != null) customerId = customer.CustomerId;
                }
                catch { }

                var resp = new
                {
                    userId = user.UserId,
                    fullName = user.FullName,
                    email = user.Email,
                    phoneNumber = user.PhoneNumber,
                    role = user.Role,
                    isActive = user.IsActive,
                    emailVerified = user.EmailVerified,
                    customerId = customerId
                };

                return Ok(new { success = true, data = resp });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID người dùng không hợp lệ" });

                var user = await _userService.GetUserByIdAsync(id);

                return Ok(new {
                    success = true,
                    message = "Lấy thông tin người dùng thành công",
                    data = user
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
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
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

                var user = await _userService.CreateUserAsync(request);

                return CreatedAtAction(nameof(GetUserById), new { id = user.UserId }, new {
                    success = true,
                    message = "Tạo người dùng thành công. Mật khẩu tạm đã được gửi qua email.",
                    data = user
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

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusRequest request)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID người dùng không hợp lệ" });

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new {
                        success = false,
                        message = "Dữ liệu không hợp lệ",
                        errors = errors
                    });
                }

                if (!request.IsActive && IsCurrentUser(id))
                    return BadRequest(new { success = false, message = "Không thể vô hiệu hóa tài khoản của chính mình" });

                var result = await _userService.UpdateUserStatusAsync(id, request.IsActive);

                if (result)
                {
                    var action = request.IsActive ? "kích hoạt" : "vô hiệu hóa";
                    return Ok(new {
                        success = true,
                        message = $"{action} người dùng thành công"
                    });
                }
                else
                {
                    var action = request.IsActive ? "kích hoạt" : "vô hiệu hóa";
                    return StatusCode(500, new {
                        success = false,
                        message = $"Không thể {action} người dùng"
                    });
                }
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

        private bool IsCurrentUser(int userId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int currentUserId) && currentUserId == userId;
        }
    }

    public class UpdateUserStatusRequest
    {
        public bool IsActive { get; set; }
    }
}
