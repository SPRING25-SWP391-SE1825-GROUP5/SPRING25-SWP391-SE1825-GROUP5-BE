using System;

namespace EVServiceCenter.Domain.Entities;

public partial class LoginHistory
{
    public long LoginHistoryId { get; set; }

    public int? UserId { get; set; }

    public DateTime AttemptAt { get; set; }

    public int FailedLoginAttempts { get; set; }

    public DateTime? LockoutUntil { get; set; }

    public virtual User? User { get; set; }
}


