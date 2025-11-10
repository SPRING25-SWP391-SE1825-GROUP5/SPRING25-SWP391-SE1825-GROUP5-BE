using System;

namespace EVServiceCenter.Api.Configurations
{
	public class AiServiceOptions
	{
		public const string SectionName = "AiService";
		public string? BaseUrl { get; set; }
		public string? InternalToken { get; set; }
		public int TimeoutSeconds { get; set; } = 30;
	}
}


