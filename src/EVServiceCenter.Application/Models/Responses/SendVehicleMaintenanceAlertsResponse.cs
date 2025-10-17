using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class SendVehicleMaintenanceAlertsResponse
    {
        public bool Success { get; set; }
        public required string Message { get; set; } = string.Empty;
        public SendVehicleMaintenanceAlertsSummary Summary { get; set; } = new();
        public required List<SendVehicleMaintenanceAlertsResult> Results { get; set; } = new();
    }

    public class SendVehicleMaintenanceAlertsSummary
    {
        public int TotalReminders { get; set; }
        public int SentEmails { get; set; }
        public int SentSms { get; set; }
        public int Failed { get; set; }
        public int UpcomingDays { get; set; }
    }

    public class SendVehicleMaintenanceAlertsResult
    {
        public int ReminderId { get; set; }
        public int VehicleId { get; set; }
        public int ServiceId { get; set; }
        public string? DueDate { get; set; }
        public int? DueMileage { get; set; }
        public bool EmailSent { get; set; }
        public bool SmsSent { get; set; }
        public string? Error { get; set; }
    }
}
