using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface ISkillRepository
    {
        Task<List<Skill>> GetAllAsync();
        Task<Skill?> GetByIdAsync(int skillId);
        Task<Skill> AddAsync(Skill skill);
        Task UpdateAsync(Skill skill);
        Task DeleteAsync(int skillId);
        Task<bool> NameExistsAsync(string name, int? excludeId = null);
    }
}


