using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class MaintenanceReminderRepository : IMaintenanceReminderRepository
    {
        private readonly EVDbContext _db;
        public MaintenanceReminderRepository(EVDbContext db) { _db = db; }

        public async Task<List<MaintenanceReminder>> QueryAsync(int? customerId, int? vehicleId, string status, DateTime? from, DateTime? to)
        {
            var q = _db.MaintenanceReminders
                .Include(r => r.Vehicle)
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

        public async Task<MaintenanceReminder> GetByIdAsync(int id)
        {
            return await _db.MaintenanceReminders.FirstOrDefaultAsync(r => r.ReminderId == id);
        }
    }
}


