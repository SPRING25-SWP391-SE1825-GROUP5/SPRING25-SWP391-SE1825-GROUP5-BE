using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class WorkOrderPart
{
    public int WorkOrderPartId { get; set; }

    public int BookingId { get; set; }

    public int PartId { get; set; }

    public int? VehicleModelPartId { get; set; }

    public int QuantityUsed { get; set; }

    public virtual Part Part { get; set; }

    public virtual Booking Booking { get; set; }

    public virtual VehicleModelPart? VehicleModelPart { get; set; }
}
