using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces;

public interface IVehicleModelPartRepository
{
    Task<VehicleModelPart?> GetByIdAsync(int id);
    Task<IEnumerable<VehicleModelPart>> GetByModelIdAsync(int modelId);
    Task<IEnumerable<VehicleModelPart>> GetByPartIdAsync(int partId);
    Task<VehicleModelPart?> GetByModelAndPartIdAsync(int modelId, int partId);
    Task<IEnumerable<VehicleModelPart>> GetCompatiblePartsByModelIdAsync(int modelId);
    Task<IEnumerable<VehicleModelPart>> GetIncompatiblePartsByModelIdAsync(int modelId);
    Task<VehicleModelPart> CreateAsync(VehicleModelPart modelPart);
    Task<VehicleModelPart> UpdateAsync(VehicleModelPart modelPart);
    Task<bool> DeleteAsync(int id);
    Task<int> CountCompatiblePartsByModelIdAsync(int modelId);
    Task<int> CountIncompatiblePartsByModelIdAsync(int modelId);
}
