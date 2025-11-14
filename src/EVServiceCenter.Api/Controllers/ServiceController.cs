using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Options;
using EVServiceCenter.Application.Configurations;
using System.Text;
using System.IO;

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AuthenticatedUser")]
    public class ServiceController : ControllerBase
    {
        private readonly IServiceService _serviceService;
        private readonly IOptions<ExportOptions> _exportOptions;

        public ServiceController(IServiceService serviceService, IOptions<ExportOptions> exportOptions)
        {
            _serviceService = serviceService;
            _exportOptions = exportOptions;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllServices(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int? categoryId = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _serviceService.GetAllServicesAsync(pageNumber, pageSize, searchTerm, categoryId);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách dịch vụ thành công",
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
        public async Task<IActionResult> ExportServices()
        {
            try
            {
                var opts = _exportOptions.Value;
                var total = await _serviceService.GetServicesCountAsync();
                if (total > opts.MaxRecords)
                {
                    return BadRequest(new { success = false, message = $"Số bản ghi ({total}) vượt quá giới hạn cho phép ({opts.MaxRecords}). Vui lòng thu hẹp bộ lọc." });
                }

                var services = await _serviceService.GetServicesForExportAsync(opts.MaxRecords);
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var bytes = GenerateServicesXlsx(services, opts.DateFormat);
                var fileName = $"services_{timestamp}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        private static byte[] GenerateServicesXlsx(System.Collections.Generic.IList<ServiceResponse> services, string dateFormat)
        {
            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.AddWorksheet("Services");

            var headers = new[] { "ServiceId", "ServiceName", "Description", "BasePrice", "IsActive", "CreatedAt" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
            }
            ws.SheetView.FreezeRows(1);

            int r = 2;
            foreach (var s in services)
            {
                ws.Cell(r, 1).Value = s.ServiceId;
                ws.Cell(r, 2).Value = s.ServiceName ?? string.Empty;
                ws.Cell(r, 3).Value = s.Description ?? string.Empty;
                ws.Cell(r, 4).Value = s.BasePrice;
                ws.Cell(r, 5).Value = s.IsActive ? "TRUE" : "FALSE";
                ws.Cell(r, 6).Value = s.CreatedAt;
                r++;
            }

            int lastRow = r - 1;
            int lastCol = headers.Length;

            ws.Range(2, 4, lastRow, 4).Style.NumberFormat.Format = "#,##0.00";
            ws.Range(2, 6, lastRow, 6).Style.DateFormat.Format = dateFormat;
            ws.Range(2, 5, lastRow, 5).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            ws.Range(1, 1, lastRow, lastCol).Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;

            var tableRange = ws.Range(1, 1, lastRow, lastCol);
            var table = tableRange.CreateTable();
            table.Theme = ClosedXML.Excel.XLTableTheme.TableStyleMedium9;
            table.ShowAutoFilter = true;
            ws.Range(1, 1, lastRow, lastCol).Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
            ws.Range(1, 1, lastRow, lastCol).Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveServices(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int? categoryId = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _serviceService.GetActiveServicesAsync(pageNumber, pageSize, searchTerm, categoryId);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy danh sách dịch vụ đang hoạt động thành công",
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

        [HttpGet("by-category/{categoryId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetServicesByCategory(int categoryId)
        {
            try
            {
                if (categoryId <= 0)
                    return BadRequest(new { success = false, message = "ID danh mục không hợp lệ" });

                var result = await _serviceService.GetActiveServicesAsync(1, 100, null, categoryId);
                
                return Ok(new { 
                    success = true, 
                    message = $"Lấy danh sách dịch vụ trong danh mục thành công",
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetServiceById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID dịch vụ không hợp lệ" });

                var service = await _serviceService.GetServiceByIdAsync(id);
                
                return Ok(new { 
                    success = true, 
                    message = "Lấy thông tin dịch vụ thành công",
                    data = service
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
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> CreateService([FromBody] CreateServiceRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return BadRequest(new { 
                        success = false, 
                        message = "Dữ liệu không hợp lệ", 
                        errors = errors 
                    });
                }

                var result = await _serviceService.CreateServiceAsync(request);
                
                return CreatedAtAction(nameof(GetServiceById), new { id = result.ServiceId }, new { 
                    success = true, 
                    message = "Tạo dịch vụ thành công",
                    data = result
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
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> UpdateService(int id, [FromBody] UpdateServiceRequest request)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID dịch vụ không hợp lệ" });

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return BadRequest(new { 
                        success = false, 
                        message = "Dữ liệu không hợp lệ", 
                        errors = errors 
                    });
                }

                var result = await _serviceService.UpdateServiceAsync(id, request);
                
                return Ok(new { 
                    success = true, 
                    message = "Cập nhật dịch vụ thành công",
                    data = result
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

        [HttpPatch("{id}/toggle-active")]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> ToggleActiveService(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID dịch vụ không hợp lệ" });

                var result = await _serviceService.ToggleActiveAsync(id);
                
                if (!result)
                    return NotFound(new { success = false, message = "Không tìm thấy dịch vụ" });

                return Ok(new { 
                    success = true, 
                    message = "Thay đổi trạng thái dịch vụ thành công"
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