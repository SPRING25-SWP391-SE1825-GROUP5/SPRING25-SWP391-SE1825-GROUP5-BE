using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class ServicePackage
{
    public int PackageId { get; set; }

    public string PackageCode { get; set; }

    public string PackageName { get; set; }

    public string Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ServicePackageItem> ServicePackageItems { get; set; } = new List<ServicePackageItem>();
}
