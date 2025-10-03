using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces;

public interface IVehicleModelPartService
{
    Task<IEnumerable<VehicleModelPartResponse>> GetPartsByModelIdAsync(int modelId);
    Task<IEnumerable<VehicleModelPartResponse>> GetModelsByPartIdAsync(int partId);
    Task<VehicleModelPartResponse> CreateAsync(CreateVehicleModelPartRequest request);
    Task<VehicleModelPartResponse> UpdateAsync(int id, UpdateVehicleModelPartRequest request);
    Task<bool> DeleteAsync(int id);
    Task<bool> ToggleCompatibilityAsync(int id);
    Task<IEnumerable<VehicleModelPartResponse>> GetCompatiblePartsAsync(int modelId);
    Task<IEnumerable<VehicleModelPartResponse>> GetIncompatiblePartsAsync(int modelId);
}
