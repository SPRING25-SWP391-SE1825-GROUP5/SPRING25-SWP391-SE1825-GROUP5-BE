using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service
{
    public class MaintenancePolicyService : IMaintenancePolicyService
    {
        private readonly IMaintenancePolicyRepository _policyRepository;
        private readonly IServiceRepository _serviceRepository;

        public MaintenancePolicyService(IMaintenancePolicyRepository policyRepository, IServiceRepository serviceRepository)
        {
            _policyRepository = policyRepository;
            _serviceRepository = serviceRepository;
        }

        public async Task<MaintenancePolicyListResponse> GetAllPoliciesAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = null, int? serviceId = null)
        {
            try
            {
                var policies = await _policyRepository.GetAllPoliciesAsync();

                // Filtering
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    policies = policies.Where(p =>
                        p.Service?.ServiceName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true
                    ).ToList();
                }

                if (serviceId.HasValue)
                {
                    policies = policies.Where(p => p.ServiceId == serviceId.Value).ToList();
                }

                // Pagination
                var totalCount = policies.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var paginatedPolicies = policies.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                var policyResponses = paginatedPolicies.Select(p => MapToPolicyResponse(p)).ToList();

                return new MaintenancePolicyListResponse
                {
                    Policies = policyResponses,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách chính sách bảo trì: {ex.Message}");
            }
        }

        public async Task<MaintenancePolicyResponse> GetPolicyByIdAsync(int policyId)
        {
            try
            {
                var policy = await _policyRepository.GetPolicyByIdAsync(policyId);
                if (policy == null)
                    throw new ArgumentException("Chính sách bảo trì không tồn tại.");

                return MapToPolicyResponse(policy);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin chính sách bảo trì: {ex.Message}");
            }
        }

        public async Task<MaintenancePolicyListResponse> GetActivePoliciesAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = null, int? serviceId = null)
        {
            try
            {
                var policies = await _policyRepository.GetActivePoliciesAsync();

                // Filtering
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    policies = policies.Where(p =>
                        p.Service?.ServiceName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true
                    ).ToList();
                }

                if (serviceId.HasValue)
                {
                    policies = policies.Where(p => p.ServiceId == serviceId.Value).ToList();
                }

                // Pagination
                var totalCount = policies.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var paginatedPolicies = policies.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                var policyResponses = paginatedPolicies.Select(p => MapToPolicyResponse(p)).ToList();

                return new MaintenancePolicyListResponse
                {
                    Policies = policyResponses,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách chính sách bảo trì đang hoạt động: {ex.Message}");
            }
        }

        public async Task<MaintenancePolicyResponse> CreatePolicyAsync(CreateMaintenancePolicyRequest request)
        {
            try
            {
                // Validate service exists
                var service = await _serviceRepository.GetServiceByIdAsync(request.ServiceId);
                if (service == null)
                    throw new ArgumentException("Dịch vụ không tồn tại.");

                // Create new policy entity
                var policy = new MaintenancePolicy
                {
                    IntervalMonths = request.IntervalMonths,
                    IntervalKm = request.IntervalKm,
                    IsActive = request.IsActive,
                    ServiceId = request.ServiceId
                };

                // Save to repository
                var createdPolicy = await _policyRepository.CreatePolicyAsync(policy);

                return MapToPolicyResponse(createdPolicy);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo chính sách bảo trì: {ex.Message}");
            }
        }

        public async Task<MaintenancePolicyResponse> UpdatePolicyAsync(int policyId, UpdateMaintenancePolicyRequest request)
        {
            try
            {
                var existingPolicy = await _policyRepository.GetPolicyByIdAsync(policyId);
                if (existingPolicy == null)
                    throw new ArgumentException("Chính sách bảo trì không tồn tại.");

                // Validate service exists
                var service = await _serviceRepository.GetServiceByIdAsync(request.ServiceId);
                if (service == null)
                    throw new ArgumentException("Dịch vụ không tồn tại.");

                existingPolicy.IntervalMonths = request.IntervalMonths;
                existingPolicy.IntervalKm = request.IntervalKm;
                existingPolicy.IsActive = request.IsActive;
                existingPolicy.ServiceId = request.ServiceId;

                await _policyRepository.UpdatePolicyAsync(existingPolicy);

                return MapToPolicyResponse(existingPolicy);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật chính sách bảo trì: {ex.Message}");
            }
        }

        public async Task<bool> ToggleActiveAsync(int policyId)
        {
            try
            {
                var policy = await _policyRepository.GetPolicyByIdAsync(policyId);
                if (policy == null)
                    return false;

                policy.IsActive = !policy.IsActive;
                await _policyRepository.UpdatePolicyAsync(policy);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi thay đổi trạng thái chính sách bảo trì: {ex.Message}");
            }
        }

        public async Task<bool> DeletePolicyAsync(int policyId)
        {
            try
            {
                var exists = await _policyRepository.PolicyExistsAsync(policyId);
                if (!exists)
                    return false;

                await _policyRepository.DeletePolicyAsync(policyId);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa chính sách bảo trì: {ex.Message}");
            }
        }

        public async Task<MaintenancePolicyListResponse> GetPoliciesByServiceIdAsync(int serviceId)
        {
            try
            {
                var policies = await _policyRepository.GetPoliciesByServiceIdAsync(serviceId);
                var policyResponses = policies.Select(p => MapToPolicyResponse(p)).ToList();

                return new MaintenancePolicyListResponse
                {
                    Policies = policyResponses,
                    PageNumber = 1,
                    PageSize = policyResponses.Count,
                    TotalPages = 1,
                    TotalCount = policyResponses.Count
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy chính sách bảo trì theo dịch vụ: {ex.Message}");
            }
        }

        private MaintenancePolicyResponse MapToPolicyResponse(MaintenancePolicy policy)
        {
            return new MaintenancePolicyResponse
            {
                PolicyId = policy.PolicyId,
                IntervalMonths = policy.IntervalMonths,
                IntervalKm = policy.IntervalKm,
                IsActive = policy.IsActive,
                ServiceId = policy.ServiceId ?? 0,
                ServiceName = policy.Service?.ServiceName ?? "N/A",
                CreatedAt = DateTime.UtcNow // Note: MaintenancePolicy entity doesn't have CreatedAt, using current time
            };
        }
    }
}
