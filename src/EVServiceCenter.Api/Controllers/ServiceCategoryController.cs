using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace EVServiceCenter.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceCategoryController : ControllerBase
{
    private readonly IServiceCategoryService _serviceCategoryService;

    public ServiceCategoryController(IServiceCategoryService serviceCategoryService)
    {
        _serviceCategoryService = serviceCategoryService;
    }

    [HttpGet]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> GetAllCategories()
    {
        try
        {
            var categories = await _serviceCategoryService.GetAllAsync();
            
            return Ok(new { 
                success = true, 
                message = "Lấy danh sách danh mục dịch vụ thành công",
                data = categories
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

    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveCategories()
    {
        try
        {
            var categories = await _serviceCategoryService.GetActiveAsync();
            
            return Ok(new { 
                success = true, 
                message = "Lấy danh sách danh mục dịch vụ đang hoạt động thành công",
                data = categories
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
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> GetCategoryById(int id)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = "ID danh mục không hợp lệ" });

            var category = await _serviceCategoryService.GetByIdAsync(id);
            
            if (category == null)
                return NotFound(new { success = false, message = "Không tìm thấy danh mục dịch vụ" });

            return Ok(new { 
                success = true, 
                message = "Lấy thông tin danh mục dịch vụ thành công",
                data = category
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

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateServiceCategoryRequest request)
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

            var result = await _serviceCategoryService.CreateAsync(request);
            
            return CreatedAtAction(nameof(GetCategoryById), new { id = result.CategoryId }, new { 
                success = true, 
                message = "Tạo danh mục dịch vụ thành công",
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
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateServiceCategoryRequest request)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = "ID danh mục không hợp lệ" });

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

            var result = await _serviceCategoryService.UpdateAsync(id, request);
            
            return Ok(new { 
                success = true, 
                message = "Cập nhật danh mục dịch vụ thành công",
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
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActiveCategory(int id, [FromBody] ToggleActiveRequest request)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = "ID danh mục không hợp lệ" });

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

            var result = await _serviceCategoryService.ToggleActiveAsync(id, request.IsActive);
            
            if (!result)
                return NotFound(new { success = false, message = "Không tìm thấy danh mục dịch vụ" });

            return Ok(new { 
                success = true, 
                message = $"{(request.IsActive ? "Kích hoạt" : "Vô hiệu hóa")} danh mục dịch vụ thành công"
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

public class ToggleActiveRequest
{
    [Required(ErrorMessage = "Trạng thái hoạt động là bắt buộc")]
    public bool IsActive { get; set; }
}