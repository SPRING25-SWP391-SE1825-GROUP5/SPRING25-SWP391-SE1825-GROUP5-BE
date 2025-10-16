using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class GetCustomerVehicleServiceRemindersResponse
    {
        public bool Success { get; set; }
        public required string Message { get; set; }
        public int CustomerId { get; set; }
        public required string CustomerName { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public required List<CustomerVehicleServiceReminder> Reminders { get; set; } = new List<CustomerVehicleServiceReminder>();
    }

    public class CustomerVehicleServiceReminder
    {
        public int ReminderId { get; set; }
        public int VehicleId { get; set; }
        public required string VehicleLicensePlate { get; set; }
        public required string VehicleVin { get; set; }
        public required string VehicleModel { get; set; }
        public int ServiceId { get; set; }
        public required string ServiceName { get; set; }
        public string? ServiceDescription { get; set; }
        public string? DueDate { get; set; }
        public int? DueMileage { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public required string Status { get; set; } // PENDING, OVERDUE, COMPLETED
        public int? DaysUntilDue { get; set; }
        public int? MilesUntilDue { get; set; }
    }
}
