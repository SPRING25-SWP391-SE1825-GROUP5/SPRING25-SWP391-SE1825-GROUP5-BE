using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IVehicleRepository
    {
        Task<List<Vehicle>> GetAllVehiclesAsync();
        Task<Vehicle> GetVehicleByIdAsync(int vehicleId);
        Task<Vehicle> CreateVehicleAsync(Vehicle vehicle);
        Task UpdateVehicleAsync(Vehicle vehicle);
        Task DeleteVehicleAsync(int vehicleId);
        Task<bool> IsVinUniqueAsync(string vin, int? excludeVehicleId = null);
        Task<bool> IsLicensePlateUniqueAsync(string licensePlate, int? excludeVehicleId = null);
        Task<bool> VehicleExistsAsync(int vehicleId);
    }
}
