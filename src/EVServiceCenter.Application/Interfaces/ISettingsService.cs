using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EVServiceCenter.Application.Interfaces;

public interface ISettingsService
{
    Task<BookingRealtimeSettingsDto> GetBookingRealtimeAsync();
    Task UpdateBookingRealtimeAsync(UpdateBookingRealtimeRequest request);

    Task<PayOsSettingsDto> GetPayOsAsync();
    Task UpdatePayOsAsync(UpdatePayOsSettingsRequest request);

    Task<GuestSessionSettingsDto> GetGuestSessionAsync();
    Task UpdateGuestSessionAsync(UpdateGuestSessionSettingsRequest request);

    Task<MaintenanceReminderSettingsDto> GetMaintenanceReminderAsync();
    Task UpdateMaintenanceReminderAsync(UpdateMaintenanceReminderSettingsRequest request);

    Task<RateLimitingSettingsDto> GetRateLimitingAsync();
    Task UpdateRateLimitingAsync(UpdateRateLimitingSettingsRequest request);
}

public class BookingRealtimeSettingsDto
{
    public int HoldTtlMinutes { get; set; }
    public required string HubPath { get; set; }
}

public class PayOsSettingsDto
{
    public int MinAmount { get; set; }
    public int DescriptionMaxLength { get; set; }
}

public class GuestSessionSettingsDto
{
    public required string CookieName { get; set; }
    public int TtlMinutes { get; set; }
    public bool SecureOnly { get; set; }
    public required string SameSite { get; set; }
    public required string Path { get; set; }
}

public class UpdateBookingRealtimeRequest
{
    public int HoldTtlMinutes { get; set; }
    public required string HubPath { get; set; }
}

public class UpdatePayOsSettingsRequest
{
    public int MinAmount { get; set; }
    public int DescriptionMaxLength { get; set; }
}

public class UpdateGuestSessionSettingsRequest
{
    public required string CookieName { get; set; }
    public int TtlMinutes { get; set; }
    public bool SecureOnly { get; set; }
    public required string SameSite { get; set; }
    public required string Path { get; set; }
}

public class MaintenanceReminderSettingsDto
{
    public int UpcomingDays { get; set; }
    public int DispatchHourLocal { get; set; }
    public required string TimeZoneId { get; set; }
}

public class UpdateMaintenanceReminderSettingsRequest
{
    public int UpcomingDays { get; set; }
    public int DispatchHourLocal { get; set; }
    public required string TimeZoneId { get; set; }
}

public class RateLimitingSettingsDto
{
    public bool Enabled { get; set; }
    public bool UseRedis { get; set; }
    public string[] BypassRoles { get; set; } = Array.Empty<string>();
    public Dictionary<string, RateLimitPolicySettingsDto> Policies { get; set; } = new();
}

public class RateLimitPolicySettingsDto
{
    public int PermitLimit { get; set; }
    public string Window { get; set; } = string.Empty; // Format: "00:01:00"
    public int QueueLimit { get; set; }
    public string ReplenishmentPeriod { get; set; } = string.Empty;
    public int TokensPerPeriod { get; set; }
    public bool AutoReplenishment { get; set; }
}

public class UpdateRateLimitingSettingsRequest
{
    public bool Enabled { get; set; }
    public bool UseRedis { get; set; }
    public string[] BypassRoles { get; set; } = Array.Empty<string>();
    public Dictionary<string, UpdateRateLimitPolicyRequest> Policies { get; set; } = new();
}

public class UpdateRateLimitPolicyRequest
{
    public int PermitLimit { get; set; }
    public string Window { get; set; } = string.Empty; // Format: "00:01:00"
    public int QueueLimit { get; set; }
    public string ReplenishmentPeriod { get; set; } = string.Empty;
    public int TokensPerPeriod { get; set; }
    public bool AutoReplenishment { get; set; }
}


