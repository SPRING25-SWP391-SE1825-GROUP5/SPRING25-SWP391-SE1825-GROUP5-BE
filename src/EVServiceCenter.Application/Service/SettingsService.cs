using System.Threading.Tasks;
using EVServiceCenter.Application.Configurations;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace EVServiceCenter.Application.Service;

public class SettingsService : ISettingsService
{
    private readonly ISystemSettingRepository _repo;
    private readonly IOptionsMonitor<BookingRealtimeOptions> _bookingOptions;
    private readonly IOptionsMonitor<PayOsOptions> _payOsOptions;
    private readonly IOptionsMonitor<GuestSessionOptions> _guestOptions;
    private readonly IOptionsMonitor<MaintenanceReminderOptions> _reminderOptions;

    // Setting keys
    private const string BookingHoldTtlKey = "BookingRealtime.HoldTtlMinutes";
    private const string BookingHubPathKey = "BookingRealtime.HubPath";
    private const string PayOsMinAmountKey = "PayOS.MinAmount";
    private const string PayOsDescMaxKey = "PayOS.DescriptionMaxLength";

    public SettingsService(ISystemSettingRepository repo, IOptionsMonitor<BookingRealtimeOptions> bookingOptions, IOptionsMonitor<PayOsOptions> payOsOptions, IOptionsMonitor<GuestSessionOptions> guestOptions, IOptionsMonitor<MaintenanceReminderOptions> reminderOptions)
    {
        _repo = repo;
        _bookingOptions = bookingOptions;
        _payOsOptions = payOsOptions;
        _guestOptions = guestOptions;
        _reminderOptions = reminderOptions;
    }

    public Task<BookingRealtimeSettingsDto> GetBookingRealtimeAsync()
    {
        var snap = _bookingOptions.CurrentValue;
        return Task.FromResult(new BookingRealtimeSettingsDto
        {
            HoldTtlMinutes = snap.HoldTtlMinutes,
            HubPath = snap.HubPath
        });
    }

    public async Task UpdateBookingRealtimeAsync(UpdateBookingRealtimeRequest request)
    {
        // Validation basic
        if (request.HoldTtlMinutes <= 0) throw new System.ArgumentOutOfRangeException(nameof(request.HoldTtlMinutes));
        if (string.IsNullOrWhiteSpace(request.HubPath)) throw new System.ArgumentException("HubPath is required", nameof(request.HubPath));

        await _repo.UpsertAsync(BookingHoldTtlKey, request.HoldTtlMinutes.ToString(), "Hold TTL in minutes for booking holds");
        await _repo.UpsertAsync(BookingHubPathKey, request.HubPath, "SignalR hub path for booking realtime");
    }

    public Task<PayOsSettingsDto> GetPayOsAsync()
    {
        var snap = _payOsOptions.CurrentValue;
        return Task.FromResult(new PayOsSettingsDto
        {
            MinAmount = snap.MinAmount,
            DescriptionMaxLength = snap.DescriptionMaxLength
        });
    }

    public async Task UpdatePayOsAsync(UpdatePayOsSettingsRequest request)
    {
        if (request.MinAmount < 0) throw new System.ArgumentOutOfRangeException(nameof(request.MinAmount));
        if (request.DescriptionMaxLength <= 0) throw new System.ArgumentOutOfRangeException(nameof(request.DescriptionMaxLength));

        await _repo.UpsertAsync(PayOsMinAmountKey, request.MinAmount.ToString(), "Minimum amount for PayOS payments");
        await _repo.UpsertAsync(PayOsDescMaxKey, request.DescriptionMaxLength.ToString(), "Max description length for PayOS payments");
    }

    public Task<GuestSessionSettingsDto> GetGuestSessionAsync()
    {
        var snap = _guestOptions.CurrentValue;
        return Task.FromResult(new GuestSessionSettingsDto
        {
            CookieName = snap.CookieName,
            TtlMinutes = snap.TtlMinutes,
            SecureOnly = snap.SecureOnly,
            SameSite = snap.SameSite,
            Path = snap.Path
        });
    }

    public async Task UpdateGuestSessionAsync(UpdateGuestSessionSettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CookieName)) throw new System.ArgumentException("CookieName is required", nameof(request.CookieName));
        if (request.TtlMinutes <= 0) throw new System.ArgumentOutOfRangeException(nameof(request.TtlMinutes));

        await _repo.UpsertAsync("GuestSession.CookieName", request.CookieName, "Guest anonymous session cookie name");
        await _repo.UpsertAsync("GuestSession.TtlMinutes", request.TtlMinutes.ToString(), "Guest session TTL (minutes)");
        await _repo.UpsertAsync("GuestSession.SecureOnly", request.SecureOnly ? "true" : "false", "Cookie secure flag");
        await _repo.UpsertAsync("GuestSession.SameSite", request.SameSite ?? "Lax", "Cookie SameSite");
        await _repo.UpsertAsync("GuestSession.Path", request.Path ?? "/", "Cookie path");
    }

    public Task<MaintenanceReminderSettingsDto> GetMaintenanceReminderAsync()
    {
        var snap = _reminderOptions.CurrentValue;
        return Task.FromResult(new MaintenanceReminderSettingsDto
        {
            UpcomingDays = snap.UpcomingDays,
            DispatchHourLocal = snap.DispatchHourLocal,
            TimeZoneId = snap.TimeZoneId
        });
    }

    public async Task UpdateMaintenanceReminderAsync(UpdateMaintenanceReminderSettingsRequest request)
    {
        if (request.UpcomingDays <= 0) throw new System.ArgumentOutOfRangeException(nameof(request.UpcomingDays));
        if (request.DispatchHourLocal < 0 || request.DispatchHourLocal > 23) throw new System.ArgumentOutOfRangeException(nameof(request.DispatchHourLocal));
        if (string.IsNullOrWhiteSpace(request.TimeZoneId)) throw new System.ArgumentException("TimeZoneId is required", nameof(request.TimeZoneId));

        await _repo.UpsertAsync("MaintenanceReminder.UpcomingDays", request.UpcomingDays.ToString(), "Days ahead to consider upcoming reminders");
        await _repo.UpsertAsync("MaintenanceReminder.DispatchHourLocal", request.DispatchHourLocal.ToString(), "Local hour to dispatch reminder emails");
        await _repo.UpsertAsync("MaintenanceReminder.TimeZoneId", request.TimeZoneId, "Windows/Olson timezone id");
    }
}


