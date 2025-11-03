using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Configurations;

public class RateLimitingOptions
{
    public bool Enabled { get; set; } = true;
    public bool UseRedis { get; set; } = true;
    public string[] BypassRoles { get; set; } = Array.Empty<string>();
    public Dictionary<string, RateLimitPolicyOptions> Policies { get; set; } = new();
}

public class RateLimitPolicyOptions
{
    public int PermitLimit { get; set; }
    public TimeSpan Window { get; set; }
    public int QueueLimit { get; set; }
    public TimeSpan ReplenishmentPeriod { get; set; }
    public int TokensPerPeriod { get; set; }
    public bool AutoReplenishment { get; set; } = true;
}

