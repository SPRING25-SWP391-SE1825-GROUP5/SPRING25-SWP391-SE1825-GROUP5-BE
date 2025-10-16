namespace EVServiceCenter.Application.Models.Responses;

public class SkillResponse
{
	public int SkillId { get; set; }
	public required string Name { get; set; }
	public string? Description { get; set; }
}


