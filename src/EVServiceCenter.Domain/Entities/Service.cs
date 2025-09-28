using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Service
{
    public int ServiceId { get; set; }

    public string ServiceName { get; set; }

    public string Description { get; set; }

    public decimal BasePrice { get; set; }

    public string RequiredSkills { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();

    public virtual ICollection<ServiceCredit> ServiceCredits { get; set; } = new List<ServiceCredit>();
}
