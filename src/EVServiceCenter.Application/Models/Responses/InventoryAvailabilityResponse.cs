using System;

namespace EVServiceCenter.Application.Models.Responses
{
	public class InventoryAvailabilityResponse
	{
		public int PartId { get; set; }
		public int TotalStock { get; set; }
		public int MinimumStock { get; set; }
		public bool IsLowStock { get; set; }
		public bool IsOutOfStock { get; set; }
		public decimal UnitPrice { get; set; }
		public string Unit { get; set; }
		public DateTime LastUpdated { get; set; }
	}
}
