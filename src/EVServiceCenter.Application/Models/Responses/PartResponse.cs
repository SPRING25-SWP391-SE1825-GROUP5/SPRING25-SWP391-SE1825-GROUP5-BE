using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class PartResponse
    {
        public int PartId { get; set; }
        public required string PartNumber { get; set; }
        public required string PartName { get; set; }
        public required string Brand { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal? Rating { get; set; }
    }
}
