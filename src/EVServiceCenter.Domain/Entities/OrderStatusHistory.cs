using System;

namespace EVServiceCenter.Domain.Entities;

public partial class OrderStatusHistory
{
    public int HistoryId { get; set; }

    public int OrderId { get; set; }

    public string Status { get; set; }

    public string? Notes { get; set; }

    public int? CreatedBy { get; set; }

    public bool SystemGenerated { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Order Order { get; set; }

    public virtual User? CreatedByUser { get; set; }
}
