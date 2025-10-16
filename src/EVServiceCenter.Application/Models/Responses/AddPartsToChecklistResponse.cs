using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class AddPartsToChecklistResponse
    {
        public int ServiceId { get; set; }
        public required string ServiceName { get; set; }
        public int AddedPartsCount { get; set; }
        public required List<ServicePartResponse> AddedParts { get; set; } = new List<ServicePartResponse>();
    }

    public class ServicePartResponse
    {
        public int ServiceId { get; set; }
        public int PartId { get; set; }
        public required string PartName { get; set; }
        public required string PartNumber { get; set; }
        public required string Brand { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? Notes { get; set; }
    }
}

