using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class VehicleResponse
    {
        public int VehicleId { get; set; }
        public int CustomerId { get; set; }
        public required string Vin { get; set; }
        public required string LicensePlate { get; set; }
        public required string Color { get; set; }
        public int CurrentMileage { get; set; }
        public DateOnly? LastServiceDate { get; set; }
        public DateOnly? PurchaseDate { get; set; }
        public DateOnly? NextServiceDue { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Related data
        public required string CustomerName { get; set; }
        public required string CustomerPhone { get; set; }
    }
}
