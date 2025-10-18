using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Service
{
    public int ServiceId { get; set; }

    public string ServiceName { get; set; }

    public string Description { get; set; }

    public decimal BasePrice { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    // Removed BookingServices collection in single-service model

}
