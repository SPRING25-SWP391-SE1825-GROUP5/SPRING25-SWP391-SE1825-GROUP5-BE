using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Infrastructure.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class SkillRepository : ISkillRepository
    {
        private readonly EVDbContext _context;
        public SkillRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<Skill>> GetAllAsync()
        {
            return await _context.Skills.OrderBy(s => s.Name).ToListAsync();
        }

        public async Task<Skill?> GetByIdAsync(int skillId)
        {
            return await _context.Skills.FirstOrDefaultAsync(s => s.SkillId == skillId);
        }

        public async Task<Skill> AddAsync(Skill skill)
        {
            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();
            return skill;
        }

        public async Task UpdateAsync(Skill skill)
        {
            _context.Skills.Update(skill);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int skillId)
        {
            var entity = await _context.Skills.FindAsync(skillId);
            if (entity != null)
            {
                _context.Skills.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
        {
            var q = _context.Skills.AsQueryable().Where(s => s.Name == name);
            if (excludeId.HasValue) q = q.Where(s => s.SkillId != excludeId.Value);
            return await q.AnyAsync();
        }
    }
}


