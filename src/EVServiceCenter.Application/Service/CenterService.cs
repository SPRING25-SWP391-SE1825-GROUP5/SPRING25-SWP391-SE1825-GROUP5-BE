using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using System.Text.RegularExpressions;

namespace EVServiceCenter.Application.Service
{
    public class CenterService : ICenterService
    {
        private readonly ICenterRepository _centerRepository;

        public CenterService(ICenterRepository centerRepository)
        {
            _centerRepository = centerRepository;
        }

        public async Task<CenterListResponse> GetAllCentersAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = null, string city = null)
        {
            try
            {
                var centers = await _centerRepository.GetAllCentersAsync();

                // Filtering
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    centers = centers.Where(c =>
                        c.CenterName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.Address.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.PhoneNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                if (!string.IsNullOrWhiteSpace(city))
                {
                    centers = centers.Where(c => c.City.Equals(city, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Pagination
                var totalCount = centers.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var paginatedCenters = centers.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                var centerResponses = paginatedCenters.Select(c => MapToCenterResponse(c)).ToList();

                return new CenterListResponse
                {
                    Centers = centerResponses,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách trung tâm: {ex.Message}");
            }
        }

        public async Task<CenterListResponse> GetActiveCentersAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = null, string city = null)
        {
            try
            {
                var centers = await _centerRepository.GetActiveCentersAsync();

                // Filtering
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    centers = centers.Where(c =>
                        c.CenterName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.Address.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.PhoneNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                if (!string.IsNullOrWhiteSpace(city))
                {
                    centers = centers.Where(c => c.City.Equals(city, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Pagination
                var totalCount = centers.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var paginatedCenters = centers.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                var centerResponses = paginatedCenters.Select(c => MapToCenterResponse(c)).ToList();

                return new CenterListResponse
                {
                    Centers = centerResponses,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách trung tâm đang hoạt động: {ex.Message}");
            }
        }

        public async Task<CenterResponse> GetCenterByIdAsync(int centerId)
        {
            try
            {
                var center = await _centerRepository.GetCenterByIdAsync(centerId);
                if (center == null)
                    throw new ArgumentException("Trung tâm không tồn tại.");

                return MapToCenterResponse(center);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin trung tâm: {ex.Message}");
            }
        }

        public async Task<CenterResponse> CreateCenterAsync(CreateCenterRequest request)
        {
            try
            {
                // Validate request
                await ValidateCreateCenterRequestAsync(request);

                // Create center entity
                var center = new ServiceCenter
                {
                    CenterName = request.CenterName.Trim(),
                    Address = request.Address.Trim(),
                    City = request.City.Trim(),
                    PhoneNumber = request.PhoneNumber.Trim(),
                    Email = request.Email.ToLower().Trim(),
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                // Save center
                var createdCenter = await _centerRepository.CreateCenterAsync(center);

                return MapToCenterResponse(createdCenter);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo trung tâm: {ex.Message}");
            }
        }

        public async Task<CenterResponse> UpdateCenterAsync(int centerId, UpdateCenterRequest request)
        {
            try
            {
                // Validate request
                await ValidateUpdateCenterRequestAsync(request);

                // Get existing center
                var center = await _centerRepository.GetCenterByIdAsync(centerId);
                if (center == null)
                    throw new ArgumentException("Trung tâm không tồn tại.");

                // Update center properties
                center.CenterName = request.CenterName.Trim();
                center.Address = request.Address.Trim();
                center.City = request.City.Trim();
                center.PhoneNumber = request.PhoneNumber.Trim();
                center.Email = request.Email.ToLower().Trim();
                center.IsActive = request.IsActive;

                // Save changes
                await _centerRepository.UpdateCenterAsync(center);

                return MapToCenterResponse(center);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật trung tâm: {ex.Message}");
            }
        }

        private CenterResponse MapToCenterResponse(ServiceCenter center)
        {
            return new CenterResponse
            {
                CenterId = center.CenterId,
                CenterName = center.CenterName,
                Address = center.Address,
                City = center.City,
                PhoneNumber = center.PhoneNumber,
                Email = center.Email,
                IsActive = center.IsActive,
                CreatedAt = center.CreatedAt
            };
        }

        private async Task ValidateCreateCenterRequestAsync(CreateCenterRequest request)
        {
            var errors = new List<string>();

            if (!IsValidPhoneNumber(request.PhoneNumber)) 
                errors.Add("Số điện thoại phải bắt đầu bằng số 0 và có đúng 10 chữ số.");
            
            if (!IsValidEmail(request.Email)) 
                errors.Add("Email không đúng định dạng.");

            if (errors.Any()) 
                throw new ArgumentException(string.Join(" ", errors));
        }

        private async Task ValidateUpdateCenterRequestAsync(UpdateCenterRequest request)
        {
            var errors = new List<string>();

            if (!IsValidPhoneNumber(request.PhoneNumber)) 
                errors.Add("Số điện thoại phải bắt đầu bằng số 0 và có đúng 10 chữ số.");
            
            if (!IsValidEmail(request.Email)) 
                errors.Add("Email không đúng định dạng.");

            if (errors.Any()) 
                throw new ArgumentException(string.Join(" ", errors));
        }

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return false;
            phoneNumber = phoneNumber.Replace(" ", "");
            var phoneRegex = new Regex(@"^0\d{9}$");
            return phoneRegex.IsMatch(phoneNumber);
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
