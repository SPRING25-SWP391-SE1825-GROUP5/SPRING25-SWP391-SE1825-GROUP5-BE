using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class ServiceCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; }

    public string Description { get; set; }

    public bool IsActive { get; set; }

    public int? ParentCategoryId { get; set; }

    public virtual ICollection<ServiceCategory> InverseParentCategory { get; set; } = new List<ServiceCategory>();

    public virtual ServiceCategory ParentCategory { get; set; }

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}
