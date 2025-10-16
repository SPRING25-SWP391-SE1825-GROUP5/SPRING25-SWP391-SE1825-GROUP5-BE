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


