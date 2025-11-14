using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class WorkOrderPart
{
    public int WorkOrderPartId { get; set; }

    public int BookingId { get; set; }

    public int PartId { get; set; }

    public int? CategoryId { get; set; }

    public int QuantityUsed { get; set; }

    public string? Status { get; set; } = "DRAFT";

    // Removed: CreatedAt, UpdatedAt, ApprovedAt per requirements

    // Renamed: ApprovedByUserId -> ApprovedByStaffId (references StaffId)
    public int? ApprovedByStaffId { get; set; }

    public DateTime? ConsumedAt { get; set; }

    // Removed: ConsumedByUserId per requirements

    public virtual Part Part { get; set; }

    public virtual Booking Booking { get; set; }

    public virtual PartCategory? Category { get; set; }

    // Đánh dấu hàng do khách cung cấp và liên kết về OrderItem nguồn
    public bool IsCustomerSupplied { get; set; }

    public int? SourceOrderItemId { get; set; }
}
