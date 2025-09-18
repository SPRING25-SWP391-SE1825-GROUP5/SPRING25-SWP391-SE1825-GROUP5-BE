using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class PartResponse
    {
        public int PartId { get; set; }
        public string PartNumber { get; set; }
        public string PartName { get; set; }
        public string Brand { get; set; }
        public decimal UnitPrice { get; set; }
        public string Unit { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
