using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Service
{
    public int ServiceId { get; set; }

    public int CategoryId { get; set; }

    public string ServiceName { get; set; }

    public string Description { get; set; }

    public int EstimatedDuration { get; set; }

    public int RequiredSlots { get; set; }

    public decimal BasePrice { get; set; }

    public string RequiredSkills { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();

    public virtual ServiceCategory Category { get; set; }

    public virtual ICollection<ServicePackageItem> ServicePackageItems { get; set; } = new List<ServicePackageItem>();
}
