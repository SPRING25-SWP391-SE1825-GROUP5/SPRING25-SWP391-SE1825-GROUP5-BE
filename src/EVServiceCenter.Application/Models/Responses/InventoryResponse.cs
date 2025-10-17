using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class InventoryResponse
    {
        public int InventoryId { get; set; }
        public int CenterId { get; set; }
        public required string CenterName { get; set; } = null!;
        public DateTime LastUpdated { get; set; }
        public int PartsCount { get; set; }
        public required List<InventoryPartResponse> InventoryParts { get; set; } = new List<InventoryPartResponse>();
    }

    public class InventoryPartResponse
    {
        public int InventoryPartId { get; set; }
        public int InventoryId { get; set; }
        public int PartId { get; set; }
        public required string PartNumber { get; set; } = null!;
        public required string PartName { get; set; } = null!;
        public required string Brand { get; set; } = null!;
        public decimal UnitPrice { get; set; }
        public int CurrentStock { get; set; }
        public int MinimumStock { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsLowStock { get; set; }
        public bool IsOutOfStock { get; set; }
    }
}
