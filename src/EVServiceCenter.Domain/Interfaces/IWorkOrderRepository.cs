using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IWorkOrderRepository
    {
        Task<WorkOrder?> GetByBookingIdAsync(int bookingId);
        Task<WorkOrder> CreateAsync(WorkOrder workOrder);
        Task<WorkOrder?> GetByIdAsync(int id);
        Task UpdateAsync(WorkOrder workOrder);
        // New queries for listings
        Task<(System.Collections.Generic.List<WorkOrder> items, int total)> QueryAsync(int? centerId, int? technicianId, int? customerId, int? vehicleId, int? serviceId, string status, System.DateTime? from, System.DateTime? to, int page, int size, string sort, bool includeRelations = false);
        Task<System.Collections.Generic.List<WorkOrder>> GetByCenterAsync(int centerId, System.DateTime? from, System.DateTime? to, string status);
        Task<System.Collections.Generic.List<WorkOrder>> GetByTechnicianAsync(int technicianId, System.DateTime? from, System.DateTime? to, string status);
        Task<System.Collections.Generic.List<WorkOrder>> GetByCustomerAsync(int customerId, System.DateTime? from, System.DateTime? to, string status);
        Task<System.Collections.Generic.List<WorkOrder>> GetByCustomerVehicleAsync(int customerId, int vehicleId, System.DateTime? from, System.DateTime? to, string status);
        Task<object> GetStatisticsAsync(int? centerId, System.DateTime? from, System.DateTime? to, string groupBy);
        Task<WorkOrder?> GetLastCompletedByVehicleAsync(int vehicleId);
        Task<bool> TechnicianHasActiveWorkOrderAsync(int technicianId, int? excludeWorkOrderId = null);
    }
}


