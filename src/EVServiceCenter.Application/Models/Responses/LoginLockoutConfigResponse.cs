using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class LoginLockoutConfigResponse
    {
        public int MaxFailedAttempts { get; set; }
        public int LockoutDurationMinutes { get; set; }
        public required string CacheKeyPrefix { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
