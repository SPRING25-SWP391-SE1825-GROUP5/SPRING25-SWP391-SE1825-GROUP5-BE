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

public class VehicleModelService : IVehicleModelService
{
    private readonly IVehicleModelRepository _vehicleModelRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IVehicleModelPartRepository _vehicleModelPartRepository;

    public VehicleModelService(
        IVehicleModelRepository vehicleModelRepository,
        IVehicleRepository vehicleRepository,
        IVehicleModelPartRepository vehicleModelPartRepository)
    {
        _vehicleModelRepository = vehicleModelRepository;
        _vehicleRepository = vehicleRepository;
        _vehicleModelPartRepository = vehicleModelPartRepository;
    }

    public async Task<VehicleModelResponse> GetByIdAsync(int id)
    {
        var model = await _vehicleModelRepository.GetByIdAsync(id);
        if (model == null)
            throw new KeyNotFoundException($"Vehicle model with ID {id} not found.");

        var vehicleCount = await _vehicleRepository.CountByModelIdAsync(id);
        var compatiblePartsCount = await _vehicleModelPartRepository.CountCompatiblePartsByModelIdAsync(id);

        return new VehicleModelResponse
        {
            ModelId = model.ModelId,
            ModelName = model.ModelName,
            Brand = model.Brand,
            IsActive = model.IsActive,
            CreatedAt = model.CreatedAt,
            VehicleCount = vehicleCount,
            CompatiblePartsCount = compatiblePartsCount
        };
    }

    public async Task<IEnumerable<VehicleModelResponse>> GetAllAsync()
    {
        var models = await _vehicleModelRepository.GetAllAsync();
        var responses = new List<VehicleModelResponse>();

        foreach (var model in models)
        {
            var vehicleCount = await _vehicleRepository.CountByModelIdAsync(model.ModelId);
            var compatiblePartsCount = await _vehicleModelPartRepository.CountCompatiblePartsByModelIdAsync(model.ModelId);

            responses.Add(new VehicleModelResponse
            {
                ModelId = model.ModelId,
                ModelName = model.ModelName,
                Brand = model.Brand,
                IsActive = model.IsActive,
                CreatedAt = model.CreatedAt,
                VehicleCount = vehicleCount,
                CompatiblePartsCount = compatiblePartsCount
            });
        }

        return responses;
    }

    public async Task<IEnumerable<VehicleModelResponse>> GetByBrandAsync(string brand)
    {
        var models = await _vehicleModelRepository.GetByBrandAsync(brand);
        var responses = new List<VehicleModelResponse>();

        foreach (var model in models)
        {
            var vehicleCount = await _vehicleRepository.CountByModelIdAsync(model.ModelId);
            var compatiblePartsCount = await _vehicleModelPartRepository.CountCompatiblePartsByModelIdAsync(model.ModelId);

            responses.Add(new VehicleModelResponse
            {
                ModelId = model.ModelId,
                ModelName = model.ModelName,
                Brand = model.Brand,
                IsActive = model.IsActive,
                CreatedAt = model.CreatedAt,
                VehicleCount = vehicleCount,
                CompatiblePartsCount = compatiblePartsCount
            });
        }

        return responses;
    }

    public async Task<IEnumerable<VehicleModelResponse>> GetActiveModelsAsync()
    {
        var models = await _vehicleModelRepository.GetActiveModelsAsync();
        var responses = new List<VehicleModelResponse>();

        foreach (var model in models)
        {
            var vehicleCount = await _vehicleRepository.CountByModelIdAsync(model.ModelId);
            var compatiblePartsCount = await _vehicleModelPartRepository.CountCompatiblePartsByModelIdAsync(model.ModelId);

            responses.Add(new VehicleModelResponse
            {
                ModelId = model.ModelId,
                ModelName = model.ModelName,
                Brand = model.Brand,
                IsActive = model.IsActive,
                CreatedAt = model.CreatedAt,
                VehicleCount = vehicleCount,
                CompatiblePartsCount = compatiblePartsCount
            });
        }

        return responses;
    }

    public async Task<VehicleModelResponse> CreateAsync(CreateVehicleModelRequest request)
    {
        var model = new VehicleModel
        {
            ModelName = request.ModelName,
            Brand = request.Brand,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        var createdModel = await _vehicleModelRepository.CreateAsync(model);
        
        return new VehicleModelResponse
        {
            ModelId = createdModel.ModelId,
            ModelName = createdModel.ModelName,
            Brand = createdModel.Brand,
            IsActive = createdModel.IsActive,
            CreatedAt = createdModel.CreatedAt,
            VehicleCount = 0,
            CompatiblePartsCount = 0
        };
    }

    public async Task<VehicleModelResponse> UpdateAsync(int id, UpdateVehicleModelRequest request)
    {
        var model = await _vehicleModelRepository.GetByIdAsync(id);
        if (model == null)
            throw new KeyNotFoundException($"Vehicle model with ID {id} not found.");

        if (request.ModelName != null)
            model.ModelName = request.ModelName;
        if (request.Brand != null)
            model.Brand = request.Brand;
        // Spec fields removed
        if (request.IsActive.HasValue)
            model.IsActive = request.IsActive.Value;

        // UpdatedAt removed - not in database

        var updatedModel = await _vehicleModelRepository.UpdateAsync(model);
        var vehicleCount = await _vehicleRepository.CountByModelIdAsync(id);
        var compatiblePartsCount = await _vehicleModelPartRepository.CountCompatiblePartsByModelIdAsync(id);

        return new VehicleModelResponse
        {
            ModelId = updatedModel.ModelId,
            ModelName = updatedModel.ModelName,
            Brand = updatedModel.Brand,
            IsActive = updatedModel.IsActive,
            CreatedAt = updatedModel.CreatedAt,
            VehicleCount = vehicleCount,
            CompatiblePartsCount = compatiblePartsCount
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var model = await _vehicleModelRepository.GetByIdAsync(id);
        if (model == null)
            return false;

        return await _vehicleModelRepository.DeleteAsync(id);
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        var model = await _vehicleModelRepository.GetByIdAsync(id);
        if (model == null)
            return false;

        model.IsActive = !model.IsActive;
        // UpdatedAt removed - not in database

        await _vehicleModelRepository.UpdateAsync(model);
        return true;
    }

    public async Task<IEnumerable<VehicleModelResponse>> SearchAsync(string searchTerm)
    {
        var models = await _vehicleModelRepository.SearchAsync(searchTerm);
        var responses = new List<VehicleModelResponse>();

        foreach (var model in models)
        {
            var vehicleCount = await _vehicleRepository.CountByModelIdAsync(model.ModelId);
            var compatiblePartsCount = await _vehicleModelPartRepository.CountCompatiblePartsByModelIdAsync(model.ModelId);

            responses.Add(new VehicleModelResponse
            {
                ModelId = model.ModelId,
                ModelName = model.ModelName,
                Brand = model.Brand,
                IsActive = model.IsActive,
                CreatedAt = model.CreatedAt,
                VehicleCount = vehicleCount,
                CompatiblePartsCount = compatiblePartsCount
            });
        }

        return responses;
    }
}
