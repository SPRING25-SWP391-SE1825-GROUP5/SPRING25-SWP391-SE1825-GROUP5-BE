using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service
{
    public class ServiceCategoryService : IServiceCategoryService
    {
        private readonly IServiceCategoryRepository _serviceCategoryRepository;

        public ServiceCategoryService(IServiceCategoryRepository serviceCategoryRepository)
        {
            _serviceCategoryRepository = serviceCategoryRepository;
        }

        public async Task<List<ServiceCategoryResponse>> GetAllServiceCategoriesAsync()
        {
            try
            {
                var categories = await _serviceCategoryRepository.GetAllServiceCategoriesAsync();
                return categories.Select(c => MapToServiceCategoryResponse(c)).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách danh mục dịch vụ: {ex.Message}");
            }
        }

        public async Task<List<ServiceCategoryResponse>> GetActiveServiceCategoriesAsync()
        {
            try
            {
                var categories = await _serviceCategoryRepository.GetActiveServiceCategoriesAsync();
                return categories.Select(c => MapToServiceCategoryResponse(c)).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách danh mục dịch vụ đang hoạt động: {ex.Message}");
            }
        }

        private ServiceCategoryResponse MapToServiceCategoryResponse(ServiceCategory category)
        {
            return new ServiceCategoryResponse
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description,
                IsActive = category.IsActive,
                ParentCategoryId = category.ParentCategoryId,
                ParentCategoryName = category.ParentCategory?.CategoryName
            };
        }
    }
}
