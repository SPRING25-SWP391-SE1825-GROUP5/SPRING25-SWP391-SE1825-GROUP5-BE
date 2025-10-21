using System;

namespace EVServiceCenter.Application.Models.Responses
{
	public class InventoryAvailabilityResponse
	{
		public int PartId { get; set; }
		public required string PartNumber { get; set; } = null!;
		public required string PartName { get; set; } = null!;
		public required string Brand { get; set; } = null!;
		public int TotalStock { get; set; }
		public int MinimumStock { get; set; }
		public bool IsLowStock { get; set; }
		public bool IsOutOfStock { get; set; }
		public decimal UnitPrice { get; set; }
		public decimal? Rating { get; set; }
		public DateTime LastUpdated { get; set; }
	}
}
