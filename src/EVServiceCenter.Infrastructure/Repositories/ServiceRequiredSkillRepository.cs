using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Infrastructure.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class ServiceRequiredSkillRepository : IServiceRequiredSkillRepository
    {
        private readonly EVDbContext _db;
        public ServiceRequiredSkillRepository(EVDbContext db) { _db = db; }

        public async Task<IReadOnlyList<ServiceRequiredSkill>> GetByServiceIdAsync(int serviceId)
        {
            return await _db.Set<ServiceRequiredSkill>()
                .Where(x => x.ServiceId == serviceId)
                .Include(x => x.Skill)
                .ToListAsync();
        }
    }
}


