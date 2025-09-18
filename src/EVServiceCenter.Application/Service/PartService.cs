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
    public class PartService : IPartService
    {
        private readonly IPartRepository _partRepository;

        public PartService(IPartRepository partRepository)
        {
            _partRepository = partRepository;
        }

        public async Task<PartListResponse> GetAllPartsAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = null, bool? isActive = null)
        {
            try
            {
                var parts = await _partRepository.GetAllPartsAsync();

                // Filtering
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    parts = parts.Where(p =>
                        p.PartNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        p.PartName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        p.Brand.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                if (isActive.HasValue)
                {
                    parts = parts.Where(p => p.IsActive == isActive.Value).ToList();
                }

                // Pagination
                var totalCount = parts.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var paginatedParts = parts.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                var partResponses = paginatedParts.Select(p => MapToPartResponse(p)).ToList();

                return new PartListResponse
                {
                    Parts = partResponses,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách phụ tùng: {ex.Message}");
            }
        }

        public async Task<PartResponse> GetPartByIdAsync(int partId)
        {
            try
            {
                var part = await _partRepository.GetPartByIdAsync(partId);
                if (part == null)
                    throw new ArgumentException("Phụ tùng không tồn tại.");

                return MapToPartResponse(part);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy thông tin phụ tùng: {ex.Message}");
            }
        }

        public async Task<PartResponse> CreatePartAsync(CreatePartRequest request)
        {
            try
            {
                // Validate request
                await ValidateCreatePartRequestAsync(request);

                // Create part entity
                var part = new Part
                {
                    PartNumber = request.PartNumber.Trim().ToUpper(),
                    PartName = request.PartName.Trim(),
                    Brand = request.Brand.Trim(),
                    UnitPrice = request.UnitPrice,
                    Unit = request.Unit.Trim(),
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                // Save part
                var createdPart = await _partRepository.CreatePartAsync(part);

                return MapToPartResponse(createdPart);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo phụ tùng: {ex.Message}");
            }
        }

        private PartResponse MapToPartResponse(Part part)
        {
            return new PartResponse
            {
                PartId = part.PartId,
                PartNumber = part.PartNumber,
                PartName = part.PartName,
                Brand = part.Brand,
                UnitPrice = part.UnitPrice,
                Unit = part.Unit,
                IsActive = part.IsActive,
                CreatedAt = part.CreatedAt
            };
        }

        private async Task ValidateCreatePartRequestAsync(CreatePartRequest request)
        {
            var errors = new List<string>();

            // Check for duplicate part number
            if (!await _partRepository.IsPartNumberUniqueAsync(request.PartNumber.Trim().ToUpper()))
            {
                errors.Add("Mã phụ tùng này đã tồn tại. Vui lòng chọn mã khác.");
            }

            if (errors.Any())
                throw new ArgumentException(string.Join(" ", errors));
        }
    }
}
