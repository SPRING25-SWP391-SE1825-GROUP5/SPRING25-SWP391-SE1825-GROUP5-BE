using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces;

public interface IVehicleModelRepository
{
    Task<VehicleModel?> GetByIdAsync(int id);
    Task<IEnumerable<VehicleModel>> GetAllAsync();
    Task<IEnumerable<VehicleModel>> GetActiveModelsAsync();
    Task<VehicleModel> CreateAsync(VehicleModel model);
    Task<VehicleModel> UpdateAsync(VehicleModel model);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<VehicleModel>> SearchAsync(string searchTerm);
}
