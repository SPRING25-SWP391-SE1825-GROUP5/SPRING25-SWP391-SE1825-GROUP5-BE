namespace EVServiceCenter.Application.Models.Requests
{
	public class UpdatePartRequest
	{
		public string PartName { get; set; }
		public string Brand { get; set; }
		public decimal UnitPrice { get; set; }
		public string? ImageUrl { get; set; }
		public bool IsActive { get; set; }
	}
}
