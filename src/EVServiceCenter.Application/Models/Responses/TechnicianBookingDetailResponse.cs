using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class TechnicianBookingDetailResponse
    {
        public int TechnicianId { get; set; }
        public int BookingId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string SlotTime { get; set; } = string.Empty;
        public int TechnicianSlotId { get; set; }
        
        // Service Information
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceDescription { get; set; } = string.Empty;
        public decimal ServicePrice { get; set; }
        
        // Center Information
        public int CenterId { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public string CenterAddress { get; set; } = string.Empty;
        public string CenterPhone { get; set; } = string.Empty;
        
        // Customer Information
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        
        // Vehicle Information
        public int VehicleId { get; set; }
        public string VehiclePlate { get; set; } = string.Empty;
        public string VehicleModel { get; set; } = string.Empty;
        public string VehicleColor { get; set; } = string.Empty;
        public int CurrentMileage { get; set; }
        public DateTime? LastServiceDate { get; set; }
        
        // Maintenance Checklist
        public List<MaintenanceChecklistInfo> MaintenanceChecklists { get; set; } = new();
        
        // Additional Information
        public string SpecialRequests { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class MaintenanceChecklistInfo
    {
        public int ChecklistId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<MaintenanceChecklistResultInfo> Results { get; set; } = new();
    }

    public class MaintenanceChecklistResultInfo
    {
        public int ResultId { get; set; }
        public int PartId { get; set; }
        public string PartName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Result { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}