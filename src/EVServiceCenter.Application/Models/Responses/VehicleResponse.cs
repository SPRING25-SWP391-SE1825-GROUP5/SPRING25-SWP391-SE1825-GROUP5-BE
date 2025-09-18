using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class VehicleResponse
    {
        public int VehicleId { get; set; }
        public int CustomerId { get; set; }
        public int ModelId { get; set; }
        public string Vin { get; set; }
        public string LicensePlate { get; set; }
        public string Color { get; set; }
        public int CurrentMileage { get; set; }
        public DateOnly? LastServiceDate { get; set; }
        public DateOnly? NextServiceDue { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Related data
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string ModelBrand { get; set; }
        public string ModelName { get; set; }
        public int ModelYear { get; set; }
        public decimal? BatteryCapacity { get; set; }
        public int? Range { get; set; }
    }
}
