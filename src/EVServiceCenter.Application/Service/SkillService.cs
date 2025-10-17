using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service;

public class SkillService : ISkillService
{
	private readonly ISkillRepository _skillRepository;

	public SkillService(ISkillRepository skillRepository)
	{
		_skillRepository = skillRepository;
	}

	public async Task<List<SkillResponse>> GetAllAsync()
	{
		var list = await _skillRepository.GetAllAsync();
		return list.Select(Map).ToList();
	}

	public async Task<SkillResponse?> GetByIdAsync(int skillId)
	{
		var s = await _skillRepository.GetByIdAsync(skillId);
		return s == null ? null : Map(s);
	}

	public async Task<SkillResponse> CreateAsync(SkillCreateRequest request)
	{
		if (await _skillRepository.NameExistsAsync(request.Name))
			throw new System.ArgumentException("Tên kỹ năng đã tồn tại");
		var s = new Skill { Name = request.Name.Trim(), Description = request.Description?.Trim() ?? string.Empty };
		s = await _skillRepository.AddAsync(s);
		return Map(s);
	}

	public async Task<SkillResponse> UpdateAsync(int skillId, SkillUpdateRequest request)
	{
		var s = await _skillRepository.GetByIdAsync(skillId) ?? throw new System.ArgumentException("Kỹ năng không tồn tại");
		if (await _skillRepository.NameExistsAsync(request.Name.Trim(), skillId))
			throw new System.ArgumentException("Tên kỹ năng đã tồn tại");
		s.Name = request.Name.Trim();
		s.Description = request.Description?.Trim() ?? string.Empty;
		await _skillRepository.UpdateAsync(s);
		return Map(s);
	}

	public async Task DeleteAsync(int skillId)
	{
		await _skillRepository.DeleteAsync(skillId);
	}

	private static SkillResponse Map(Skill s) => new()
	{
		SkillId = s.SkillId,
		Name = s.Name,
		Description = s.Description
	};
}


