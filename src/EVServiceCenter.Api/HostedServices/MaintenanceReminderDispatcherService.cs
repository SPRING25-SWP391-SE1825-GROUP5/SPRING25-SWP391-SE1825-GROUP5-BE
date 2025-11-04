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
    public class MaintenanceReminderDispatcherService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly MaintenanceReminderSchedulerOptions _options;

        public MaintenanceReminderDispatcherService(IServiceScopeFactory scopeFactory, IOptions<MaintenanceReminderSchedulerOptions> options)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_options.Enabled)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IMaintenanceReminderRepository>();
                    var email = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    var templates = scope.ServiceProvider.GetRequiredService<IEmailTemplateRenderer>();
                    var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    var today = DateTime.UtcNow.Date;

                    var pending = await repo.QueryAsync(null, null, "PENDING", null, null);
                    foreach (var r in pending)
                    {
                        if (r.DueDate.HasValue)
                        {
                            var due = r.DueDate.Value.ToDateTime(TimeOnly.MinValue).Date;
                            if (due < today)
                            {
                                r.Status = ReminderStatus.OVERDUE;
                                r.UpdatedAt = DateTime.UtcNow;
                                await repo.UpdateAsync(r);
                            }
                            else if (due == today)
                            {
                                r.Status = ReminderStatus.DUE;
                                r.UpdatedAt = DateTime.UtcNow;
                                await repo.UpdateAsync(r);
                            }
                        }
                    }

                    var dueList = await repo.QueryAsync(null, null, "DUE", null, null);
                    foreach (var r in dueList)
                    {
                        if (r.DueDate.HasValue)
                        {
                            var due = r.DueDate.Value.ToDateTime(TimeOnly.MinValue).Date;
                            if (due < today)
                            {
                                r.Status = ReminderStatus.OVERDUE;
                                r.UpdatedAt = DateTime.UtcNow;
                                await repo.UpdateAsync(r);
                            }
                        }
                    }

                    var toSend = (await repo.QueryAsync(null, null, "DUE", null, null))
                        .Concat(await repo.QueryAsync(null, null, "OVERDUE", null, null))
                        .ToList();

                    foreach (var r in toSend)
                    {
                        if (r.CadenceDays.HasValue)
                        {
                            var nextAllowed = r.LastSentAt.HasValue ? r.LastSentAt.Value.AddDays(r.CadenceDays.Value) : (DateTime?)null;
                            var canSend = !nextAllowed.HasValue || DateTime.UtcNow >= nextAllowed.Value;
                            if (!canSend) continue;
                        }
                        else
                        {
                            continue;
                        }

                        var emailTo = r.Vehicle?.Customer?.User?.Email;
                        if (string.IsNullOrWhiteSpace(emailTo)) continue;
                        var subject = r.Type == ReminderType.PACKAGE ? "Nhắc sử dụng gói" : "Nhắc bảo dưỡng";
                        var body = await templates.RenderAsync("MaintenanceReminder", new System.Collections.Generic.Dictionary<string, string>
                        {
                            ["vehicleId"] = r.VehicleId.ToString(),
                            ["serviceId"] = (r.ServiceId?.ToString() ?? string.Empty),
                            ["dueDate"] = (r.DueDate?.ToDateTime(TimeOnly.MinValue).ToString("u") ?? string.Empty),
                            ["dueMileage"] = (r.DueMileage?.ToString() ?? string.Empty)
                        });
                        await email.SendEmailAsync(emailTo, subject, body);
                        var userId = r.Vehicle?.Customer?.User?.UserId ?? 0;
                        if (userId > 0)
                        {
                            await notifications.SendBookingNotificationAsync(userId, subject, $"Xe #{r.VehicleId} đến hạn bảo dưỡng", "MAINTENANCE");
                        }
                        r.LastSentAt = DateTime.UtcNow;
                        r.UpdatedAt = DateTime.UtcNow;
                        await repo.UpdateAsync(r);
                    }
                }

                var delay = TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes));
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}


