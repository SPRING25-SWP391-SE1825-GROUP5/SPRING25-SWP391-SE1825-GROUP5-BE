using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class InventoryResponse
    {
        public int InventoryId { get; set; }
        public int CenterId { get; set; }
        public string CenterName { get; set; }
        public int PartId { get; set; }
        public string PartNumber { get; set; }
        public string PartName { get; set; }
        public string Brand { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Unit { get; set; }
        public int CurrentStock { get; set; }
        public int MinimumStock { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsLowStock { get; set; }
        public bool IsOutOfStock { get; set; }
    }
}
