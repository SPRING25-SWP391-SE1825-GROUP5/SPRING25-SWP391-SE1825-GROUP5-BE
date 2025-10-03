using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class UpsertTechnicianSkillItem
{
	[Required]
	public int SkillId { get; set; }

	[MaxLength(200)]
	public string? Notes { get; set; }
}

public class UpsertTechnicianSkillsRequest
{
	[Required]
	[MinLength(1, ErrorMessage = "Danh sách kỹ năng không được rỗng")]
	public List<UpsertTechnicianSkillItem> Items { get; set; } = new();
}


