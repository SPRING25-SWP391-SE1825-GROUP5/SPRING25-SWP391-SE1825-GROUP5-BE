using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EVServiceCenter.Domain.Interfaces;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/part-categories")]
    public class PartCategoryController : ControllerBase
    {
        private readonly IPartCategoryRepository _partCategoryRepository;

        public PartCategoryController(IPartCategoryRepository partCategoryRepository)
        {
            _partCategoryRepository = partCategoryRepository;
        }

        /// <summary>
        /// Lấy tất cả danh mục phụ tùng (Chỉ Admin và Staff)
        /// </summary>
        /// <returns>Danh sách tất cả danh mục phụ tùng</returns>
        [HttpGet]
        [Authorize(Policy = "StaffOrAdmin")]
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                var categories = await _partCategoryRepository.GetAllAsync();

                var data = categories.Select(c => new
                {
                    c.CategoryId,
                    c.CategoryName,
                    c.Description,
                    c.ParentId,
                    parentName = c.Parent?.CategoryName,
                    c.IsActive,
                    c.CreatedAt,
                    c.UpdatedAt
                });

                return Ok(new {
                    success = true,
                    message = "Lấy danh sách danh mục phụ tùng thành công",
                    data = data
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
        /// Lấy danh mục phụ tùng đang hoạt động (Public API - Không cần đăng nhập)
        /// </summary>
        /// <returns>Danh sách danh mục phụ tùng đang hoạt động</returns>
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveCategories()
        {
            try
            {
                var categories = await _partCategoryRepository.GetActiveAsync();

                var data = categories.Select(c => new
                {
                    c.CategoryId,
                    c.CategoryName,
                    c.Description,
                    c.ParentId,
                    parentName = c.Parent?.CategoryName,
                    c.IsActive,
                    c.CreatedAt,
                    c.UpdatedAt
                });

                return Ok(new {
                    success = true,
                    message = "Lấy danh sách danh mục phụ tùng đang hoạt động thành công",
                    data = data
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
        /// Lấy thông tin danh mục phụ tùng theo ID
        /// </summary>
        /// <param name="id">ID danh mục</param>
        /// <returns>Thông tin danh mục</returns>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "ID danh mục không hợp lệ" });

                var category = await _partCategoryRepository.GetByIdAsync(id);

                if (category == null)
                    return NotFound(new { success = false, message = "Không tìm thấy danh mục phụ tùng" });

                var childrenList = new List<object>();
                if (category.Children != null && category.Children.Any())
                {
                    foreach (var ch in category.Children)
                    {
                        childrenList.Add(new
                        {
                            ch.CategoryId,
                            ch.CategoryName,
                            ch.Description,
                            ch.IsActive
                        });
                    }
                }

                var data = new
                {
                    category.CategoryId,
                    category.CategoryName,
                    category.Description,
                    category.ParentId,
                    parentName = category.Parent?.CategoryName,
                    parentDescription = category.Parent?.Description,
                    category.IsActive,
                    category.CreatedAt,
                    category.UpdatedAt,
                    children = childrenList
                };

                return Ok(new {
                    success = true,
                    message = "Lấy thông tin danh mục phụ tùng thành công",
                    data = data
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
    }
}

