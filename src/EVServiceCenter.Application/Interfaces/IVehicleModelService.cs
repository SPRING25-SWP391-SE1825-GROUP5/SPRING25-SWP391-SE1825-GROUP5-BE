using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces;

public interface IVehicleModelService
{
    Task<VehicleModelResponse> GetByIdAsync(int id);
    Task<IEnumerable<VehicleModelResponse>> GetAllAsync();
    Task<IEnumerable<VehicleModelResponse>> GetActiveModelsAsync();
    Task<VehicleModelResponse> CreateAsync(CreateVehicleModelRequest request);
    Task<VehicleModelResponse> UpdateAsync(int id, UpdateVehicleModelRequest request);
    Task<bool> DeleteAsync(int id);
    Task<bool> ToggleActiveAsync(int id);
    Task<IEnumerable<VehicleModelResponse>> SearchAsync(string searchTerm);
}
