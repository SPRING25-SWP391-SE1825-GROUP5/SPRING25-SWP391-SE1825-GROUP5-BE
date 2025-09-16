using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Channel
{
    public int ChannelId { get; set; }

    public string Code { get; set; }

    public string Name { get; set; }

    public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
}
