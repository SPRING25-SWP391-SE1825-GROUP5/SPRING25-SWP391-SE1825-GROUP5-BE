using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class UpsertTechnicianSkillItem
{
	[Required]
	public int SkillId { get; set; }

	[Required]
	[Range(1, 5, ErrorMessage = "Level phải từ 1 đến 5")]
	public byte Level { get; set; }

	[Range(0, 50, ErrorMessage = "Years phải từ 0 đến 50")]
	public byte Years { get; set; }
}

public class UpsertTechnicianSkillsRequest
{
	[Required]
	[MinLength(1, ErrorMessage = "Danh sách kỹ năng không được rỗng")]
	public List<UpsertTechnicianSkillItem> Items { get; set; } = new();
}


