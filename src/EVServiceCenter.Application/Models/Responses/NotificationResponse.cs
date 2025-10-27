using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class NotificationResponse
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string Type { get; set; } = string.Empty; // BOOKING, SYSTEM, etc.
        public string Status { get; set; } = string.Empty; // NEW, READ, etc.
    }
}
