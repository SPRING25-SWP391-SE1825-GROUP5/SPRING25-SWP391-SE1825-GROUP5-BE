using System;

namespace EVServiceCenter.Domain.Entities;

public partial class Wishlist
{
    public int WishlistId { get; set; }

    public int CustomerId { get; set; }

    public int PartId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Customer Customer { get; set; }

    public virtual Part Part { get; set; }
}
