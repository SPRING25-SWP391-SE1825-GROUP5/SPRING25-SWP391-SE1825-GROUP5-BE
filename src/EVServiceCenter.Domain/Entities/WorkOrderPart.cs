using System;
using System.Collections.Generic;
using EVServiceCenter.Domain.Enums;

namespace EVServiceCenter.Domain.Entities;

public partial class WorkOrderPart
{
    public int WorkOrderPartId { get; set; }

    public int BookingId { get; set; }

    public int PartId { get; set; }

    public int? VehicleModelPartId { get; set; }

    public int QuantityUsed { get; set; }

    public WorkOrderPartStatus Status { get; set; } = WorkOrderPartStatus.DRAFT;

    public decimal? UnitPrice { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public int? ApprovedByUserId { get; set; }

    public DateTime? ConsumedAt { get; set; }

    public int? ConsumedByUserId { get; set; }

    public virtual Part Part { get; set; }

    public virtual Booking Booking { get; set; }

    public virtual VehicleModelPart? VehicleModelPart { get; set; }
}
