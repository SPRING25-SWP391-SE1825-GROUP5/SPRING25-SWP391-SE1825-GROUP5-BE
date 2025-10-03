using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Staff
{
    public int StaffId { get; set; }

    public int UserId { get; set; }

    public int CenterId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ServiceCenter Center { get; set; }

    public virtual User User { get; set; }
}
