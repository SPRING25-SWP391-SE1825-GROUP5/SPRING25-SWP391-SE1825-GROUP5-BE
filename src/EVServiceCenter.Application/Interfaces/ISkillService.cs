using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces;

public interface ISkillService
{
	Task<List<SkillResponse>> GetAllAsync();
	Task<SkillResponse?> GetByIdAsync(int skillId);
	Task<SkillResponse> CreateAsync(SkillCreateRequest request);
	Task<SkillResponse> UpdateAsync(int skillId, SkillUpdateRequest request);
	Task DeleteAsync(int skillId);
}


