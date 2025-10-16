using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class SkillCreateRequest
{
	[Required]
	[StringLength(100, MinimumLength = 2)]
	public required string Name { get; set; }

	[StringLength(255)]
	public string? Description { get; set; }
}


