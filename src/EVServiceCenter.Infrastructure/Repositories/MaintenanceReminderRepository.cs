using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Infrastructure.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class MaintenanceReminderRepository : IMaintenanceReminderRepository
    {
        private readonly EVDbContext _db;
        public MaintenanceReminderRepository(EVDbContext db) { _db = db; }

        public async Task<List<MaintenanceReminder>> QueryAsync(int? customerId, int? vehicleId, string? status, DateTime? from, DateTime? to)
        {
            var q = _db.MaintenanceReminders
                .Include(r => r.Vehicle)
                    .ThenInclude(v => v.VehicleModel)
                .Include(r => r.Vehicle)
                    .ThenInclude(v => v.Customer)
                        .ThenInclude(c => c.User)
                .Include(r => r.Service)
                .AsQueryable();

            if (customerId.HasValue) q = q.Where(r => r.Vehicle != null && r.Vehicle.CustomerId == customerId.Value);
            if (vehicleId.HasValue) q = q.Where(r => r.VehicleId == vehicleId);
            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim().ToUpperInvariant();
                if (s == "COMPLETED" || s == "DONE" || s == "FINISHED")
                {
                    q = q.Where(r => r.IsCompleted);
                }
                else if (s == "PENDING" || s == "OPEN" || s == "NOT_COMPLETED")
                {
                    q = q.Where(r => !r.IsCompleted);
                }
            }
            if (from.HasValue) q = q.Where(r => r.CreatedAt >= from.Value);
            if (to.HasValue) q = q.Where(r => r.CreatedAt <= to.Value);
            return await q.OrderByDescending(r => r.CreatedAt).ToListAsync();
        }

        public async Task<MaintenanceReminder> CreateAsync(MaintenanceReminder reminder)
        {
            _db.MaintenanceReminders.Add(reminder);
            await _db.SaveChangesAsync();
            return reminder;
        }

        public async Task UpdateAsync(MaintenanceReminder reminder)
        {
            _db.MaintenanceReminders.Update(reminder);
            await _db.SaveChangesAsync();
        }

        public async Task<MaintenanceReminder?> GetByIdAsync(int id)
        {
            return await _db.MaintenanceReminders
                .Include(r => r.Vehicle)
                    .ThenInclude(v => v.VehicleModel)
                .Include(r => r.Vehicle)
                    .ThenInclude(v => v.Customer)
                        .ThenInclude(c => c.User)
                .Include(r => r.Service)
                .FirstOrDefaultAsync(r => r.ReminderId == id);
        }

        public async Task<(List<MaintenanceReminder> Items, int TotalCount)> QueryForAdminAsync(
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
            string sortOrder)
        {
            var q = _db.MaintenanceReminders
                .Include(r => r.Vehicle)
                    .ThenInclude(v => v.VehicleModel)
                .Include(r => r.Vehicle)
                    .ThenInclude(v => v.Customer)
                        .ThenInclude(c => c.User)
                .Include(r => r.Service)
                .AsQueryable();

            // Apply filters
            if (customerId.HasValue)
                q = q.Where(r => r.Vehicle != null && r.Vehicle.CustomerId == customerId.Value);

            if (vehicleId.HasValue)
                q = q.Where(r => r.VehicleId == vehicleId.Value);

            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim().ToUpperInvariant();
                if (s == "COMPLETED" || s == "DONE" || s == "FINISHED")
                {
                    q = q.Where(r => r.IsCompleted || r.Status == Domain.Enums.ReminderStatus.COMPLETED);
                }
                else if (s == "PENDING" || s == "OPEN" || s == "NOT_COMPLETED")
                {
                    q = q.Where(r => !r.IsCompleted && r.Status != Domain.Enums.ReminderStatus.COMPLETED);
                }
                else
                {
                    // Try to parse as enum
                    if (Enum.TryParse<Domain.Enums.ReminderStatus>(s, true, out var statusEnum))
                    {
                        q = q.Where(r => r.Status == statusEnum);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                if (Enum.TryParse<Domain.Enums.ReminderType>(type.Trim().ToUpperInvariant(), true, out var typeEnum))
                {
                    q = q.Where(r => r.Type == typeEnum);
                }
            }

            if (from.HasValue)
                q = q.Where(r => r.CreatedAt >= from.Value);

            if (to.HasValue)
                q = q.Where(r => r.CreatedAt <= to.Value);

            // Search term - search by vehicle plate, customer name, service name
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.Trim().ToLower();
                q = q.Where(r =>
                    (r.Vehicle != null && r.Vehicle.LicensePlate != null && r.Vehicle.LicensePlate.ToLower().Contains(search)) ||
                    (r.Vehicle != null && r.Vehicle.VehicleModel != null && r.Vehicle.VehicleModel.ModelName != null && r.Vehicle.VehicleModel.ModelName.ToLower().Contains(search)) ||
                    (r.Vehicle != null && r.Vehicle.Customer != null && r.Vehicle.Customer.User != null && r.Vehicle.Customer.User.FullName != null && r.Vehicle.Customer.User.FullName.ToLower().Contains(search)) ||
                    (r.Service != null && r.Service.ServiceName != null && r.Service.ServiceName.ToLower().Contains(search))
                );
            }

            // Get total count before pagination
            var totalCount = await q.CountAsync();

            // Apply sorting
            var isDescending = sortOrder?.ToLowerInvariant() == "desc";
            switch (sortBy?.ToLowerInvariant())
            {
                case "duedate":
                    q = isDescending ? q.OrderByDescending(r => r.DueDate) : q.OrderBy(r => r.DueDate);
                    break;
                case "duemileage":
                    q = isDescending ? q.OrderByDescending(r => r.DueMileage) : q.OrderBy(r => r.DueMileage);
                    break;
                case "status":
                    q = isDescending ? q.OrderByDescending(r => r.Status) : q.OrderBy(r => r.Status);
                    break;
                case "type":
                    q = isDescending ? q.OrderByDescending(r => r.Type) : q.OrderBy(r => r.Type);
                    break;
                case "createdat":
                default:
                    q = isDescending ? q.OrderByDescending(r => r.CreatedAt) : q.OrderBy(r => r.CreatedAt);
                    break;
            }

            // Apply pagination
            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<int> CountAsync(
            int? customerId,
            int? vehicleId,
            string? status,
            string? type,
            DateTime? from,
            DateTime? to,
            string? searchTerm)
        {
            var q = _db.MaintenanceReminders
                .Include(r => r.Vehicle)
                    .ThenInclude(v => v.VehicleModel)
                .Include(r => r.Vehicle)
                    .ThenInclude(v => v.Customer)
                        .ThenInclude(c => c.User)
                .Include(r => r.Service)
                .AsQueryable();

            // Apply same filters as QueryForAdminAsync
            if (customerId.HasValue)
                q = q.Where(r => r.Vehicle != null && r.Vehicle.CustomerId == customerId.Value);

            if (vehicleId.HasValue)
                q = q.Where(r => r.VehicleId == vehicleId.Value);

            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim().ToUpperInvariant();
                if (s == "COMPLETED" || s == "DONE" || s == "FINISHED")
                {
                    q = q.Where(r => r.IsCompleted || r.Status == Domain.Enums.ReminderStatus.COMPLETED);
                }
                else if (s == "PENDING" || s == "OPEN" || s == "NOT_COMPLETED")
                {
                    q = q.Where(r => !r.IsCompleted && r.Status != Domain.Enums.ReminderStatus.COMPLETED);
                }
                else
                {
                    if (Enum.TryParse<Domain.Enums.ReminderStatus>(s, true, out var statusEnum))
                    {
                        q = q.Where(r => r.Status == statusEnum);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                if (Enum.TryParse<Domain.Enums.ReminderType>(type.Trim().ToUpperInvariant(), true, out var typeEnum))
                {
                    q = q.Where(r => r.Type == typeEnum);
                }
            }

            if (from.HasValue)
                q = q.Where(r => r.CreatedAt >= from.Value);

            if (to.HasValue)
                q = q.Where(r => r.CreatedAt <= to.Value);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.Trim().ToLower();
                q = q.Where(r =>
                    (r.Vehicle != null && r.Vehicle.LicensePlate != null && r.Vehicle.LicensePlate.ToLower().Contains(search)) ||
                    (r.Vehicle != null && r.Vehicle.VehicleModel != null && r.Vehicle.VehicleModel.ModelName != null && r.Vehicle.VehicleModel.ModelName.ToLower().Contains(search)) ||
                    (r.Vehicle != null && r.Vehicle.Customer != null && r.Vehicle.Customer.User != null && r.Vehicle.Customer.User.FullName != null && r.Vehicle.Customer.User.FullName.ToLower().Contains(search)) ||
                    (r.Service != null && r.Service.ServiceName != null && r.Service.ServiceName.ToLower().Contains(search))
                );
            }

            return await q.CountAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var reminder = await _db.MaintenanceReminders.FindAsync(id);
            if (reminder != null)
            {
                _db.MaintenanceReminders.Remove(reminder);
                await _db.SaveChangesAsync();
            }
        }
    }
}


