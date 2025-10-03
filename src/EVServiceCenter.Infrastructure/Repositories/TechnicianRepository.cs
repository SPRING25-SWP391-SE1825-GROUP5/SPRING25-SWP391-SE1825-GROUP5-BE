using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class TechnicianRepository : ITechnicianRepository
    {
        private readonly EVDbContext _context;

        public TechnicianRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<Technician>> GetAllTechniciansAsync()
        {
            return await _context.Technicians
                .Include(t => t.User)
                .Include(t => t.Center)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<Technician> GetTechnicianByIdAsync(int technicianId)
        {
            return await _context.Technicians
                .Include(t => t.User)
                .Include(t => t.Center)
                .FirstOrDefaultAsync(t => t.TechnicianId == technicianId);
        }

        public async Task<Technician> GetTechnicianByUserIdAsync(int userId)
        {
            return await _context.Technicians
                .Include(t => t.User)
                .Include(t => t.Center)
                .FirstOrDefaultAsync(t => t.UserId == userId);
        }

        public async Task<List<Technician>> GetTechniciansByCenterIdAsync(int centerId)
        {
            return await _context.Technicians
                .Include(t => t.User)
                .Include(t => t.Center)
                .Where(t => t.CenterId == centerId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<Technician> CreateTechnicianAsync(Technician technician)
        {
            _context.Technicians.Add(technician);
            await _context.SaveChangesAsync();
            return technician;
        }

        public async Task UpdateTechnicianAsync(Technician technician)
        {
            _context.Technicians.Update(technician);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteTechnicianAsync(int technicianId)
        {
            var technician = await _context.Technicians.FindAsync(technicianId);
            if (technician != null)
            {
                _context.Technicians.Remove(technician);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> TechnicianExistsAsync(int technicianId)
        {
            return await _context.Technicians.AnyAsync(t => t.TechnicianId == technicianId);
        }

        public async Task<bool> IsUserAlreadyTechnicianAsync(int userId)
        {
            return await _context.Technicians.AnyAsync(t => t.UserId == userId);
        }

        public async Task UpsertSkillsAsync(int technicianId, IEnumerable<TechnicianSkill> skills)
        {
            var existing = await _context.TechnicianSkills
                .Where(ts => ts.TechnicianId == technicianId)
                .ToListAsync();

            // Update or add
            foreach (var s in skills)
            {
                var found = existing.FirstOrDefault(x => x.SkillId == s.SkillId);
                if (found == null)
                {
                    _context.TechnicianSkills.Add(new TechnicianSkill
                    {
                        TechnicianId = technicianId,
                        SkillId = s.SkillId,
                        Notes = s.Notes
                    });
                }
                else
                {
                    found.Notes = s.Notes;
                    _context.TechnicianSkills.Update(found);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task RemoveSkillAsync(int technicianId, int skillId)
        {
            var entity = await _context.TechnicianSkills
                .FirstOrDefaultAsync(ts => ts.TechnicianId == technicianId && ts.SkillId == skillId);
            if (entity != null)
            {
                _context.TechnicianSkills.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}