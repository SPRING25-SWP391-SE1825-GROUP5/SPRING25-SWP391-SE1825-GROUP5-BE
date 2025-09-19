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
    public class ServiceService : IServiceService
    {
        private readonly IServiceRepository _serviceRepository;

        public ServiceService(IServiceRepository serviceRepository)
        {
            _serviceRepository = serviceRepository;
        }

        public async Task<ServiceListResponse> GetAllServicesAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = null, int? categoryId = null)
        {
            try
            {
                var services = await _serviceRepository.GetAllServicesAsync();

                // Filtering
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    services = services.Where(s =>
                        s.ServiceName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        s.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        s.RequiredSkills.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                if (categoryId.HasValue)
                {
                    services = services.Where(s => s.CategoryId == categoryId.Value).ToList();
                }

                // Pagination
                var totalCount = services.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var paginatedServices = services.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                var serviceResponses = paginatedServices.Select(s => MapToServiceResponse(s)).ToList();

                return new ServiceListResponse
                {
                    Services = serviceResponses,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách dịch vụ: {ex.Message}");
            }
        }

        public async Task<ServiceResponse> GetServiceByIdAsync(int serviceId)
        {
            try
            {
                var service = await _serviceRepository.GetServiceByIdAsync(serviceId);
                if (service == null)
                    throw new ArgumentException("Dịch vụ không tồn tại.");

                return MapToServiceResponse(service);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin dịch vụ: {ex.Message}");
            }
        }

        public async Task<ServiceListResponse> GetActiveServicesAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = null, int? categoryId = null)
        {
            try
            {
                var services = await _serviceRepository.GetActiveServicesAsync();

                // Filtering
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    services = services.Where(s =>
                        s.ServiceName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        s.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        s.RequiredSkills.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                if (categoryId.HasValue)
                {
                    services = services.Where(s => s.CategoryId == categoryId.Value).ToList();
                }

                // Pagination
                var totalCount = services.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var paginatedServices = services.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                var serviceResponses = paginatedServices.Select(s => MapToServiceResponse(s)).ToList();

                return new ServiceListResponse
                {
                    Services = serviceResponses,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách dịch vụ đang hoạt động: {ex.Message}");
            }
        }

        private ServiceResponse MapToServiceResponse(Domain.Entities.Service service)
        {
            return new ServiceResponse
            {
                ServiceId = service.ServiceId,
                CategoryId = service.CategoryId,
                CategoryName = service.Category?.CategoryName,
                ServiceName = service.ServiceName,
                Description = service.Description,
                EstimatedDuration = service.EstimatedDuration,
                RequiredSlots = service.RequiredSlots,
                BasePrice = service.BasePrice,
                RequiredSkills = service.RequiredSkills,
                IsActive = service.IsActive,
                CreatedAt = service.CreatedAt
            };
        }
    }
}
