using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class SendReminderBeforeAppointmentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int BookingId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string VehicleLicensePlate { get; set; }
        public string ServiceName { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public int ReminderHoursBefore { get; set; }
        public SendReminderBeforeAppointmentSummary Summary { get; set; }
        public List<SendReminderBeforeAppointmentResult> Results { get; set; } = new List<SendReminderBeforeAppointmentResult>();
    }

    public class SendReminderBeforeAppointmentSummary
    {
        public int TotalRemindersSent { get; set; }
        public int EmailSent { get; set; }
        public int SmsSent { get; set; }
        public int Failed { get; set; }
        public DateTime SentAt { get; set; }
    }

    public class SendReminderBeforeAppointmentResult
    {
        public string ReminderType { get; set; }
        public string Channel { get; set; } // EMAIL, SMS
        public bool Sent { get; set; }
        public string Error { get; set; }
        public DateTime SentAt { get; set; }
    }
}
