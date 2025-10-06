using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IVehicleService
    {
        Task<VehicleListResponse> GetVehiclesAsync(int pageNumber = 1, int pageSize = 10, int? customerId = null, string searchTerm = null);
        Task<VehicleResponse> GetVehicleByIdAsync(int vehicleId);
        Task<VehicleResponse> CreateVehicleAsync(CreateVehicleRequest request);
        Task<VehicleResponse> UpdateVehicleAsync(int vehicleId, UpdateVehicleRequest request);
        
        // Remaining methods
        Task<CustomerResponse> GetCustomerByVehicleIdAsync(int vehicleId);
        Task<VehicleResponse> GetVehicleByVinOrLicensePlateAsync(string vinOrLicensePlate);
    }
}
