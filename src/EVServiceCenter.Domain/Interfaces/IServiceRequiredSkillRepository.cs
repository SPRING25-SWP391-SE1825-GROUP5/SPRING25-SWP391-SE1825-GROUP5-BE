using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IServiceRequiredSkillRepository
    {
        Task<IReadOnlyList<ServiceRequiredSkill>> GetByServiceIdAsync(int serviceId);
    }
}


