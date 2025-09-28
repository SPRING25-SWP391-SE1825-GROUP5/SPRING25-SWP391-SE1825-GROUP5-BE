using System;

namespace EVServiceCenter.Domain.Entities;

public partial class ServiceCreditUsage
{
    public int UsageId { get; set; }

    public int CreditId { get; set; }

    public int? BookingId { get; set; }

    public int? WorkOrderId { get; set; }

    public DateTime UsedAt { get; set; }

    public virtual ServiceCredit Credit { get; set; }

    public virtual Booking Booking { get; set; }

    public virtual WorkOrder WorkOrder { get; set; }
}
