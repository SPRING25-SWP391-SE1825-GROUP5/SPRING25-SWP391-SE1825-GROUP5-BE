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
    public class ChecklistPartService : IChecklistPartService
    {
        // Removed: IServicePartRepository _servicePartRepository;
        private readonly IServiceService _serviceService;
        private readonly IPartService _partService;

        public ChecklistPartService(
            IServiceService serviceService,
            IPartService partService)
        {
            _serviceService = serviceService;
            _partService = partService;
        }

        public async Task<AddPartsToChecklistResponse> AddPartsToChecklistAsync(AddPartsToChecklistRequest request)
        {
            try
            {
                // Validate service exists
                var service = await _serviceService.GetServiceByIdAsync(request.ServiceId);
                if (service == null)
                    throw new ArgumentException("Dịch vụ không tồn tại.");

                var addedParts = new List<ServicePartResponse>();

                // Add each part to service (deprecated flow) – no-op since ServiceParts removed
                foreach (var partData in request.Parts)
                {
                    // Validate part exists
                    var part = await _partService.GetPartByIdAsync(partData.PartId);
                    if (part == null)
                        throw new ArgumentException($"Part với ID {partData.PartId} không tồn tại.");

                    // No persistence to ServiceParts – consider mapping via template instead

                    // Add to response
                    addedParts.Add(new ServicePartResponse
                    {
                        ServiceId = request.ServiceId,
                        PartId = part.PartId,
                        PartName = part.PartName,
                        PartNumber = part.PartNumber,
                        Brand = part.Brand,
                        Price = part.Price,
                        ImageUrl = part.ImageUrl
                    });
                }

                return new AddPartsToChecklistResponse
                {
                    ServiceId = request.ServiceId,
                    ServiceName = service.ServiceName,
                    AddedPartsCount = addedParts.Count,
                    AddedParts = addedParts
                };
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi thêm Parts vào Checklist: {ex.Message}");
            }
        }

        public async Task<RemovePartsFromChecklistResponse> RemovePartsFromChecklistAsync(RemovePartsFromChecklistRequest request)
        {
            try
            {
                // Validate service exists
                var service = await _serviceService.GetServiceByIdAsync(request.ServiceId);
                if (service == null)
                    throw new ArgumentException("Dịch vụ không tồn tại.");

                var removedPartIds = new List<int>(); // no-op

                // Remove each part from service
                foreach (var partId in request.PartIds)
                {
                    try
                    {
                        // No deletion since ServiceParts removed
                        removedPartIds.Add(partId);
                    }
                    catch (Exception ex)
                    {
                        // Log warning but continue with other parts
                        Console.WriteLine($"Warning: Could not remove part {partId}: {ex.Message}");
                    }
                }

                return new RemovePartsFromChecklistResponse
                {
                    ServiceId = request.ServiceId,
                    ServiceName = service.ServiceName,
                    RemovedPartsCount = removedPartIds.Count,
                    RemovedPartIds = removedPartIds
                };
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa Parts khỏi Checklist: {ex.Message}");
            }
        }

        public async Task<List<ServicePartResponse>> GetPartsByServiceIdAsync(int serviceId)
        {
            try
            {
                // Validate service exists
                var service = await _serviceService.GetServiceByIdAsync(serviceId);
                if (service == null)
                    throw new ArgumentException("Dịch vụ không tồn tại.");

                // ServiceParts removed: return empty list for now (template-based flow will populate elsewhere)
                return new List<ServicePartResponse>();
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy Parts theo Service: {ex.Message}");
            }
        }
    }
}
