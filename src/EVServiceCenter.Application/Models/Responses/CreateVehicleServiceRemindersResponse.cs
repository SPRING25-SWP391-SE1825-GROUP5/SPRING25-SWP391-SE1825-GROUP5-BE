using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class CreateVehicleServiceRemindersResponse
    {
        public bool Success { get; set; }
        public required string Message { get; set; }
        public int VehicleId { get; set; }
        public required string VehicleLicensePlate { get; set; }
        public int CreatedRemindersCount { get; set; }
        public required List<CreatedVehicleServiceReminder> CreatedReminders { get; set; } = new List<CreatedVehicleServiceReminder>();
    }

    public class CreatedVehicleServiceReminder
    {
        public int ReminderId { get; set; }
        public int VehicleId { get; set; }
        public int ServiceId { get; set; }
        public required string ServiceName { get; set; }
        public string? DueDate { get; set; }
        public int? DueMileage { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
