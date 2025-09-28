using System;

namespace EVServiceCenter.Domain.Entities;

public partial class ProductReview
{
    public int ReviewId { get; set; }

    public int PartId { get; set; }

    public int CustomerId { get; set; }

    public int? OrderId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public bool IsVerified { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Part Part { get; set; }

    public virtual Customer Customer { get; set; }

    public virtual Order? Order { get; set; }
}
