using System;

namespace EVServiceCenter.Domain.Entities;

public partial class Feedback
{
    public int FeedbackId { get; set; }

    public int? CustomerId { get; set; }


    public int? OrderId { get; set; }

    public int? WorkOrderId { get; set; }

    public int? PartId { get; set; }

    public int? TechnicianId { get; set; }

    public byte Rating { get; set; }

    public string? Comment { get; set; }

    public bool IsAnonymous { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Customer Customer { get; set; }
    public virtual Order? Order { get; set; }
    public virtual WorkOrder? WorkOrder { get; set; }
    public virtual Part? Part { get; set; }
    public virtual Technician? Technician { get; set; }
}


