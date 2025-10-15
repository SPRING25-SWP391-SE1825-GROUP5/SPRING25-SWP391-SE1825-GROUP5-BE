using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service;

public class VehicleModelPartService : IVehicleModelPartService
{
    private readonly IVehicleModelPartRepository _vehicleModelPartRepository;
    private readonly IVehicleModelRepository _vehicleModelRepository;
    private readonly IPartRepository _partRepository;

    public VehicleModelPartService(
        IVehicleModelPartRepository vehicleModelPartRepository,
        IVehicleModelRepository vehicleModelRepository,
        IPartRepository partRepository)
    {
        _vehicleModelPartRepository = vehicleModelPartRepository;
        _vehicleModelRepository = vehicleModelRepository;
        _partRepository = partRepository;
    }

    public async Task<IEnumerable<VehicleModelPartResponse>> GetPartsByModelIdAsync(int modelId)
    {
        var modelParts = await _vehicleModelPartRepository.GetByModelIdAsync(modelId);
        var responses = new List<VehicleModelPartResponse>();

        foreach (var modelPart in modelParts)
        {
            var model = await _vehicleModelRepository.GetByIdAsync(modelPart.ModelId);
            var part = await _partRepository.GetPartByIdAsync(modelPart.PartId);

            responses.Add(new VehicleModelPartResponse
            {
                Id = modelPart.Id,
                ModelId = modelPart.ModelId,
                PartId = modelPart.PartId,
                IsCompatible = modelPart.IsCompatible,
                // CompatibilityNotes removed - using IsCompatible boolean instead
                CreatedAt = modelPart.CreatedAt,
                ModelName = model?.ModelName,
                Brand = model?.Brand,
                PartName = part?.PartName,
                PartNumber = part?.PartNumber,
                PartBrand = part?.Brand,
                PartUnitPrice = part?.Price
            });
        }

        return responses;
    }

    public async Task<IEnumerable<VehicleModelPartResponse>> GetModelsByPartIdAsync(int partId)
    {
        var modelParts = await _vehicleModelPartRepository.GetByPartIdAsync(partId);
        var responses = new List<VehicleModelPartResponse>();

        foreach (var modelPart in modelParts)
        {
            var model = await _vehicleModelRepository.GetByIdAsync(modelPart.ModelId);
            var part = await _partRepository.GetPartByIdAsync(modelPart.PartId);

            responses.Add(new VehicleModelPartResponse
            {
                Id = modelPart.Id,
                ModelId = modelPart.ModelId,
                PartId = modelPart.PartId,
                IsCompatible = modelPart.IsCompatible,
                // CompatibilityNotes removed - using IsCompatible boolean instead
                CreatedAt = modelPart.CreatedAt,
                ModelName = model?.ModelName,
                Brand = model?.Brand,
                PartName = part?.PartName,
                PartNumber = part?.PartNumber,
                PartBrand = part?.Brand,
                PartUnitPrice = part?.Price
            });
        }

        return responses;
    }

    public async Task<VehicleModelPartResponse> CreateAsync(CreateVehicleModelPartRequest request)
    {
        // Check if model exists
        var model = await _vehicleModelRepository.GetByIdAsync(request.ModelId);
        if (model == null)
            throw new KeyNotFoundException($"Vehicle model with ID {request.ModelId} not found.");

        // Check if part exists
            var part = await _partRepository.GetPartByIdAsync(request.PartId);
        if (part == null)
            throw new KeyNotFoundException($"Part with ID {request.PartId} not found.");

        // Check if relationship already exists
        var existingRelation = await _vehicleModelPartRepository.GetByModelAndPartIdAsync(request.ModelId, request.PartId);
        if (existingRelation != null)
            throw new InvalidOperationException("Relationship between this model and part already exists.");

        var modelPart = new VehicleModelPart
        {
            ModelId = request.ModelId,
            PartId = request.PartId,
            IsCompatible = request.IsCompatible,
            // CompatibilityNotes removed - using IsCompatible boolean instead
            CreatedAt = DateTime.Now
        };

        var createdModelPart = await _vehicleModelPartRepository.CreateAsync(modelPart);

        return new VehicleModelPartResponse
        {
            Id = createdModelPart.Id,
            ModelId = createdModelPart.ModelId,
            PartId = createdModelPart.PartId,
            IsCompatible = createdModelPart.IsCompatible,
            // CompatibilityNotes removed - using IsCompatible boolean instead
            CreatedAt = createdModelPart.CreatedAt,
            ModelName = model.ModelName,
            Brand = model.Brand,
            PartName = part.PartName,
            PartNumber = part.PartNumber,
            PartBrand = part.Brand,
            PartUnitPrice = part.Price
        };
    }

    public async Task<VehicleModelPartResponse> UpdateAsync(int id, UpdateVehicleModelPartRequest request)
    {
        var modelPart = await _vehicleModelPartRepository.GetByIdAsync(id);
        if (modelPart == null)
            throw new KeyNotFoundException($"Vehicle model part relationship with ID {id} not found.");

        if (request.IsCompatible.HasValue)
            modelPart.IsCompatible = request.IsCompatible.Value;
        // CompatibilityNotes removed - using IsCompatible boolean instead

        var updatedModelPart = await _vehicleModelPartRepository.UpdateAsync(modelPart);

        var model = await _vehicleModelRepository.GetByIdAsync(updatedModelPart.ModelId);
        var part = await _partRepository.GetPartByIdAsync(updatedModelPart.PartId);

        return new VehicleModelPartResponse
        {
            Id = updatedModelPart.Id,
            ModelId = updatedModelPart.ModelId,
            PartId = updatedModelPart.PartId,
            IsCompatible = updatedModelPart.IsCompatible,
            // CompatibilityNotes removed - using IsCompatible boolean instead
            CreatedAt = updatedModelPart.CreatedAt,
            ModelName = model?.ModelName,
            Brand = model?.Brand,
            PartName = part?.PartName,
            PartNumber = part?.PartNumber,
            PartBrand = part?.Brand,
            PartUnitPrice = part?.Price
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var modelPart = await _vehicleModelPartRepository.GetByIdAsync(id);
        if (modelPart == null)
            return false;

        return await _vehicleModelPartRepository.DeleteAsync(id);
    }

    public async Task<bool> ToggleCompatibilityAsync(int id)
    {
        var modelPart = await _vehicleModelPartRepository.GetByIdAsync(id);
        if (modelPart == null)
            return false;

        modelPart.IsCompatible = !modelPart.IsCompatible;
        await _vehicleModelPartRepository.UpdateAsync(modelPart);
        return true;
    }

    public async Task<IEnumerable<VehicleModelPartResponse>> GetCompatiblePartsAsync(int modelId)
    {
        var modelParts = await _vehicleModelPartRepository.GetCompatiblePartsByModelIdAsync(modelId);
        var responses = new List<VehicleModelPartResponse>();

        foreach (var modelPart in modelParts)
        {
            var model = await _vehicleModelRepository.GetByIdAsync(modelPart.ModelId);
            var part = await _partRepository.GetPartByIdAsync(modelPart.PartId);

            responses.Add(new VehicleModelPartResponse
            {
                Id = modelPart.Id,
                ModelId = modelPart.ModelId,
                PartId = modelPart.PartId,
                IsCompatible = modelPart.IsCompatible,
                // CompatibilityNotes removed - using IsCompatible boolean instead
                CreatedAt = modelPart.CreatedAt,
                ModelName = model?.ModelName,
                Brand = model?.Brand,
                PartName = part?.PartName,
                PartNumber = part?.PartNumber,
                PartBrand = part?.Brand,
                PartUnitPrice = part?.Price
            });
        }

        return responses;
    }

    public async Task<IEnumerable<VehicleModelPartResponse>> GetIncompatiblePartsAsync(int modelId)
    {
        var modelParts = await _vehicleModelPartRepository.GetIncompatiblePartsByModelIdAsync(modelId);
        var responses = new List<VehicleModelPartResponse>();

        foreach (var modelPart in modelParts)
        {
            var model = await _vehicleModelRepository.GetByIdAsync(modelPart.ModelId);
            var part = await _partRepository.GetPartByIdAsync(modelPart.PartId);

            responses.Add(new VehicleModelPartResponse
            {
                Id = modelPart.Id,
                ModelId = modelPart.ModelId,
                PartId = modelPart.PartId,
                IsCompatible = modelPart.IsCompatible,
                // CompatibilityNotes removed - using IsCompatible boolean instead
                CreatedAt = modelPart.CreatedAt,
                ModelName = model?.ModelName,
                Brand = model?.Brand,
                PartName = part?.PartName,
                PartNumber = part?.PartNumber,
                PartBrand = part?.Brand,
                PartUnitPrice = part?.Price
            });
        }

        return responses;
    }
}
