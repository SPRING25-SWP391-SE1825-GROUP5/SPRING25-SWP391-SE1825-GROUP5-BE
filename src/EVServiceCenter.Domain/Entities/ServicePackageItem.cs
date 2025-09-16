using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class ServicePackageItem
{
    public int PackageId { get; set; }

    public int ServiceId { get; set; }

    public decimal Quantity { get; set; }

    public int? SortOrder { get; set; }

    public virtual ServicePackage Package { get; set; }

    public virtual Service Service { get; set; }
}
