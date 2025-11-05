using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EVServiceCenter.Application.Configurations;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Enums;
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

                    var reminderRepo = scope.ServiceProvider.GetRequiredService<IMaintenanceReminderRepository>();
                    var bookings = await bookingRepo.GetAllBookingsAsync();
                    var candidates = bookings.Where(b => (b.Status == "CONFIRMED" || b.Status == "IN_PROGRESS") && b.TechnicianSlotId.HasValue).ToList();

                    var sentBookingIds = new System.Collections.Generic.HashSet<int>();

                    foreach (var b in candidates)
                    {
                        var full = await bookingRepo.GetBookingDetailAsync(b.BookingId);
                        if (full?.TechnicianTimeSlot?.Slot == null) continue;
                        var at = new DateTime(full.TechnicianTimeSlot.WorkDate.Year, full.TechnicianTimeSlot.WorkDate.Month, full.TechnicianTimeSlot.WorkDate.Day,
                                              full.TechnicianTimeSlot.Slot.SlotTime.Hour, full.TechnicianTimeSlot.Slot.SlotTime.Minute, full.TechnicianTimeSlot.Slot.SlotTime.Second, DateTimeKind.Utc);
                        if (at < now || at > windowEnd) continue;
                        var emailTo = full.Customer?.User?.Email;
                        if (string.IsNullOrWhiteSpace(emailTo)) continue;

                        var existingReminders = await reminderRepo.QueryAsync(
                            customerId: full.Customer?.CustomerId,
                            vehicleId: full.VehicleId,
                            status: null,
                            from: null,
                            to: null
                        );
                        var appointmentReminder = existingReminders
                            .FirstOrDefault(r => r.Type == ReminderType.APPOINTMENT && r.VehicleId == full.VehicleId);

                        if (sentBookingIds.Contains(full.BookingId))
                        {
                            continue;
                        }

                        if (appointmentReminder != null && appointmentReminder.LastSentAt.HasValue)
                        {
                            var hoursSinceLastSent = (now - appointmentReminder.LastSentAt.Value).TotalHours;
                            if (hoursSinceLastSent < 24)
                            {
                                continue;
                            }
                        }

                        var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
                        var frontendUrl = config["App:FrontendUrl"] ?? "http://localhost:3000";
                        var bookingUrl = $"{frontendUrl}/profile?tab=history";
                        var fullName = full.Customer?.User?.FullName ?? "Khách hàng";
                        var centerName = full.Center?.CenterName ?? string.Empty;
                        var centerAddress = full.Center?.Address ?? string.Empty;
                        var date = full.TechnicianTimeSlot.WorkDate.ToString("yyyy-MM-dd");
                        var time = full.TechnicianTimeSlot.Slot.SlotTime.ToString(@"hh\:mm");

                        var subject = "Nhắc lịch hẹn";
                        var body = await templates.RenderAsync("AppointmentReminder", new System.Collections.Generic.Dictionary<string, string>
                        {
                            ["bookingId"] = full.BookingId.ToString(),
                            ["centerName"] = centerName,
                            ["centerAddress"] = centerAddress,
                            ["date"] = date,
                            ["time"] = time,
                            ["fullName"] = fullName,
                            ["bookingUrl"] = bookingUrl,
                            ["year"] = DateTime.UtcNow.Year.ToString(),
                            ["supportPhone"] = config["AppSettings:SupportPhone"] ?? "1900-xxxx"
                        });
                        await email.SendEmailAsync(emailTo, subject, body);
                        var userId = full.Customer?.User?.UserId ?? 0;
                        if (userId > 0)
                        {
                            await notifications.SendBookingNotificationAsync(userId, subject, $"Booking #{full.BookingId} lúc {at:u}", "APPOINTMENT");
                        }

                        if (appointmentReminder == null)
                        {
                            appointmentReminder = new MaintenanceReminder
                            {
                                VehicleId = full.VehicleId,
                                Type = ReminderType.APPOINTMENT,
                                Status = ReminderStatus.COMPLETED,
                                LastSentAt = now,
                                CreatedAt = now,
                                UpdatedAt = now
                            };
                            await reminderRepo.CreateAsync(appointmentReminder);
                        }
                        else
                        {
                            appointmentReminder.LastSentAt = now;
                            appointmentReminder.UpdatedAt = now;
                            await reminderRepo.UpdateAsync(appointmentReminder);
                        }

                        sentBookingIds.Add(full.BookingId);
                    }
                }

                var delay = TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes));
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}


