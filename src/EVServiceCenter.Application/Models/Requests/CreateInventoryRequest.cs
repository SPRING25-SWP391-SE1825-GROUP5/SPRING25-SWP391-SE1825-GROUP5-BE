namespace EVServiceCenter.Application.Models.Requests
{
	public class CreateInventoryRequest
	{
		public int CenterId { get; set; }
		public int PartId { get; set; }
		public int CurrentStock { get; set; }
		public int MinimumStock { get; set; }
	}
}
