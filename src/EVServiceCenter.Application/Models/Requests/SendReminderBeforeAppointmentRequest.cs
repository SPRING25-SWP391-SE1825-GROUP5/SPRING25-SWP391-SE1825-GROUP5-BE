using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class SendReminderBeforeAppointmentRequest
    {
        [Required]
        public int BookingId { get; set; }

        public int? ReminderHoursBefore { get; set; } = 24; // Default 24 hours before

        public bool SendEmail { get; set; } = true;

        public bool SendSms { get; set; } = false;

        public string? CustomMessage { get; set; }

        public required List<string> ReminderTypes { get; set; } = new List<string> { "APPOINTMENT_REMINDER" }; // APPOINTMENT_REMINDER, MAINTENANCE_REMINDER, etc.
    }
}
