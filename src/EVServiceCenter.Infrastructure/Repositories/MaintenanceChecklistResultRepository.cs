using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Infrastructure.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class MaintenanceChecklistResultRepository : IMaintenanceChecklistResultRepository
    {
        private readonly EVDbContext _db;
        public MaintenanceChecklistResultRepository(EVDbContext db) { _db = db; }

        public async Task<List<MaintenanceChecklistResult>> GetByChecklistIdAsync(int checklistId)
        {
            return await _db.MaintenanceChecklistResults
                .Where(r => r.ChecklistId == checklistId)
                .ToListAsync();
        }

        public async Task UpsertAsync(MaintenanceChecklistResult result)
        {
            // Ưu tiên cập nhật theo ResultId nếu có
            MaintenanceChecklistResult? existing = null;
            if (result.ResultId != 0)
            {
                existing = await _db.MaintenanceChecklistResults
                    .FirstOrDefaultAsync(r => r.ResultId == result.ResultId);
            }
            else
            {
                // Nếu không có ResultId, dùng khóa tự nhiên (ChecklistId, PartId)
                existing = await _db.MaintenanceChecklistResults
                    .FirstOrDefaultAsync(r => r.ChecklistId == result.ChecklistId && r.PartId == result.PartId);
            }

            if (existing == null)
            {
                // đảm bảo không set ResultId thủ công cho identity
                result.ResultId = 0;
                await _db.MaintenanceChecklistResults.AddAsync(result);
            }
            else
            {
                existing.Description = result.Description;
                existing.Result = result.Result;
                existing.Comment = result.Comment;
            }
            await _db.SaveChangesAsync();
        }

        public async Task UpsertManyAsync(IEnumerable<MaintenanceChecklistResult> results)
        {
            foreach (var r in results)
            {
                await UpsertAsync(r);
            }
        }
    }
}


