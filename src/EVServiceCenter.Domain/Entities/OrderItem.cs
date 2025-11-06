using System;

namespace EVServiceCenter.Domain.Entities;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int PartId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    // Số lượng đã được dùng để thay thế tại trung tâm (hàng của khách)
    public int ConsumedQty { get; set; }

    public virtual Order Order { get; set; }

    public virtual Part Part { get; set; }


}
