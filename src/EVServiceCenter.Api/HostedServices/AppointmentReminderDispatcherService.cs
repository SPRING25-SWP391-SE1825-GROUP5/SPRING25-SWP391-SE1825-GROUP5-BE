using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EVServiceCenter.Application.Configurations;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EVServiceCenter.Api.HostedServices
{
    public class AppointmentReminderDispatcherService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly AppointmentReminderOptions _options;
        private readonly MaintenanceReminderOptions _maintenanceOptions;

        public AppointmentReminderDispatcherService(IServiceScopeFactory scopeFactory, IOptions<AppointmentReminderOptions> options, IOptions<MaintenanceReminderOptions> maintenanceOptions)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _maintenanceOptions = maintenanceOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_options.Enabled)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var bookingRepo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                    var email = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    var templates = scope.ServiceProvider.GetRequiredService<IEmailTemplateRenderer>();
                    var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    var now = DateTime.UtcNow;
                    var windowHours = _options.WindowHours ?? _maintenanceOptions.AppointmentReminderHours;
                    var windowEnd = now.AddHours(windowHours);

                    var bookings = await bookingRepo.GetAllBookingsAsync();
                    var candidates = bookings.Where(b => (b.Status == "CONFIRMED" || b.Status == "IN_PROGRESS") && b.TechnicianSlotId.HasValue).ToList();

                    foreach (var b in candidates)
                    {
                        var full = await bookingRepo.GetBookingDetailAsync(b.BookingId);
                        if (full?.TechnicianTimeSlot?.Slot == null) continue;
                        var at = new DateTime(full.TechnicianTimeSlot.WorkDate.Year, full.TechnicianTimeSlot.WorkDate.Month, full.TechnicianTimeSlot.WorkDate.Day,
                                              full.TechnicianTimeSlot.Slot.SlotTime.Hour, full.TechnicianTimeSlot.Slot.SlotTime.Minute, full.TechnicianTimeSlot.Slot.SlotTime.Second, DateTimeKind.Utc);
                        if (at < now || at > windowEnd) continue;
                        var emailTo = full.Customer?.User?.Email;
                        if (string.IsNullOrWhiteSpace(emailTo)) continue;
                        var subject = "Nhắc lịch hẹn";
                        var body = await templates.RenderAsync("AppointmentReminder", new System.Collections.Generic.Dictionary<string, string>
                        {
                            ["bookingId"] = full.BookingId.ToString(),
                            ["centerName"] = full.Center?.CenterName ?? string.Empty,
                            ["appointmentUtc"] = at.ToString("u")
                        });
                        await email.SendEmailAsync(emailTo, subject, body);
                        var userId = full.Customer?.User?.UserId ?? 0;
                        if (userId > 0)
                        {
                            await notifications.SendBookingNotificationAsync(userId, subject, $"Booking #{full.BookingId} lúc {at:u}", "APPOINTMENT");
                        }
                    }
                }

                var delay = TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes));
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}


