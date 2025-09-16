using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Otpcode
{
    public int Otpid { get; set; }

    public int UserId { get; set; }

    public string Otpcode1 { get; set; }

    public string Otptype { get; set; }

    public string ContactInfo { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public DateTime? UsedAt { get; set; }

    public int AttemptCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; }
}
