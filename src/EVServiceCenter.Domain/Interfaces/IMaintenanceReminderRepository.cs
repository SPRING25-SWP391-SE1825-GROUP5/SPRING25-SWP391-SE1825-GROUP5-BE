using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IMaintenanceReminderRepository
    {
        Task<List<MaintenanceReminder>> QueryAsync(int? customerId, int? vehicleId, string? status, DateTime? from, DateTime? to);
        Task<MaintenanceReminder> CreateAsync(MaintenanceReminder reminder);
        Task UpdateAsync(MaintenanceReminder reminder);
        Task<MaintenanceReminder?> GetByIdAsync(int id);

        // Admin methods
        Task<(List<MaintenanceReminder> Items, int TotalCount)> QueryForAdminAsync(
            int page,
            int pageSize,
            int? customerId,
            int? vehicleId,
            string? status,
            string? type,
            DateTime? from,
            DateTime? to,
            string? searchTerm,
            string sortBy,
            string sortOrder);

        Task<int> CountAsync(
            int? customerId,
            int? vehicleId,
            string? status,
            string? type,
            DateTime? from,
            DateTime? to,
            string? searchTerm);

        Task DeleteAsync(int id);
    }
}


