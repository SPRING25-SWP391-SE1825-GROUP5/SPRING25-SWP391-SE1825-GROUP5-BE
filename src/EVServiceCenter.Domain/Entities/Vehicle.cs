using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Vehicle
{
    public int VehicleId { get; set; }

    public int CustomerId { get; set; }

    public string Vin { get; set; }

    public string LicensePlate { get; set; }

    public string Color { get; set; }

    public int CurrentMileage { get; set; }

    public DateOnly? LastServiceDate { get; set; }

    public DateOnly? NextServiceDue { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Customer Customer { get; set; }

    public virtual ICollection<MaintenanceReminder> MaintenanceReminders { get; set; } = new List<MaintenanceReminder>();
}
