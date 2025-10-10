using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface ITechnicianRepository
    {
        Task<List<Technician>> GetAllTechniciansAsync();
        Task<Technician> GetTechnicianByIdAsync(int technicianId);
        Task<Technician> GetTechnicianByUserIdAsync(int userId);
        Task<List<Technician>> GetTechniciansByCenterIdAsync(int centerId);
        Task<Technician> CreateTechnicianAsync(Technician technician);
        Task UpdateTechnicianAsync(Technician technician);
        Task DeleteTechnicianAsync(int technicianId);
        Task<bool> TechnicianExistsAsync(int technicianId);
        Task<bool> IsUserAlreadyTechnicianAsync(int userId);
        Task UpsertSkillsAsync(int technicianId, IEnumerable<TechnicianSkill> skills);
        Task RemoveSkillAsync(int technicianId, int skillId);
        Task<List<TechnicianSkill>> GetTechnicianSkillsAsync(int technicianId);
    }
}