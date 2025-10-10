using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class CreateVehicleServiceRemindersRequest
    {
        [Required]
        public int VehicleId { get; set; }

        [Required]
        public List<CreateVehicleServiceReminderItem> Reminders { get; set; } = new List<CreateVehicleServiceReminderItem>();
    }

    public class CreateVehicleServiceReminderItem
    {
        [Required]
        public int ServiceId { get; set; }

        public DateOnly? DueDate { get; set; }

        public int? DueMileage { get; set; }

        public string? Notes { get; set; }
    }
}
