using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EVServiceCenter.Domain.Enums;

namespace EVServiceCenter.Application.Models.Requests
{
    public class SetVehicleRemindersRequest
    {
        [Required]
        public required List<SetVehicleReminderItem> Items { get; set; } = new List<SetVehicleReminderItem>();
    }

    public class SetVehicleReminderItem
    {
        [Required]
        public int ServiceId { get; set; }

        public DateTime? DueDate { get; set; }

        public int? DueMileage { get; set; }

        public ReminderType? Type { get; set; }

        public int? CadenceDays { get; set; }
    }
}
