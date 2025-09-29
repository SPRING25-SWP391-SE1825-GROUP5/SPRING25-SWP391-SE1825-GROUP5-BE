using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class MaintenanceChecklistRepository : IMaintenanceChecklistRepository
    {
        private readonly EVDbContext _db;
        public MaintenanceChecklistRepository(EVDbContext db) { _db = db; }

        public async Task<MaintenanceChecklist> GetByWorkOrderIdAsync(int workOrderId)
        {
            return await _db.MaintenanceChecklists
                .Include(c => c.MaintenanceChecklistResults)
                .FirstOrDefaultAsync(c => c.WorkOrderId == workOrderId);
        }

        public async Task<MaintenanceChecklist> CreateAsync(MaintenanceChecklist checklist)
        {
            _db.MaintenanceChecklists.Add(checklist);
            await _db.SaveChangesAsync();
            return checklist;
        }
    }
}


