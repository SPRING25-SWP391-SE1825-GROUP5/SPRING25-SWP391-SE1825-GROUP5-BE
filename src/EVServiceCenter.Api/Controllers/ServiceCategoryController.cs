using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Responses;
using System;
using Microsoft.AspNetCore.Authorization;

namespace EVServiceCenter.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AuthenticatedUser")] // Tất cả user đã đăng nhập đều có thể xem
    public class ServiceCategoryController : ControllerBase
    {
        private readonly IServiceCategoryService _serviceCategoryService;

        public ServiceCategoryController(IServiceCategoryService serviceCategoryService)
        {
            _serviceCategoryService = serviceCategoryService;
        }

        /// <summary>
        /// Lấy danh sách tất cả danh mục dịch vụ
        /// </summary>
        /// <returns>Danh sách danh mục dịch vụ</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllServiceCategories()
        {
            try
            {
                var categories = await _serviceCategoryService.GetAllServiceCategoriesAsync();
                
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

        /// <summary>
        /// Lấy danh sách danh mục dịch vụ đang hoạt động
        /// </summary>
        /// <returns>Danh sách danh mục dịch vụ đang hoạt động</returns>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveServiceCategories()
        {
            try
            {
                var categories = await _serviceCategoryService.GetActiveServiceCategoriesAsync();
                
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
    }
}
