using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class SkillUpdateRequest
{
	[Required]
	[StringLength(100, MinimumLength = 2)]
	public string Name { get; set; }

	[StringLength(255)]
	public string? Description { get; set; }
}


