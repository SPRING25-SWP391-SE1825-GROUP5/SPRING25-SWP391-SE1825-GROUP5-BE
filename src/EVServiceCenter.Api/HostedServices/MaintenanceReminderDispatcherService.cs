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
using Microsoft.Extensions.Logging;
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
                    var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
                    var centerRepo = scope.ServiceProvider.GetRequiredService<ICenterRepository>();

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

                        // Lấy thông tin đầy đủ cho template
                        var frontendUrl = config["App:FrontendUrl"] ?? "http://localhost:3000";
                        var bookingUrl = $"{frontendUrl}/booking?serviceId={r.ServiceId}&vehicleId={r.VehicleId}";
                        var fullName = r.Vehicle?.Customer?.User?.FullName ?? "Khách hàng";
                        var vehicleName = $"{r.Vehicle?.VehicleModel?.ModelName ?? "Xe"} - {r.Vehicle?.LicensePlate ?? ""}";
                        var serviceName = r.Service?.ServiceName ?? "Bảo dưỡng định kỳ";

                        // Lấy centerName: ưu tiên từ booking gần nhất, sau đó lấy center active đầu tiên
                        var centerName = "Trung tâm gần nhất";
                        try
                        {
                            // Thử lấy từ booking gần nhất của vehicle này
                            var bookingRepo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                            var allBookings = await bookingRepo.GetAllBookingsAsync();
                            var recentBooking = allBookings
                                .Where(b => b.VehicleId == r.VehicleId &&
                                           b.CenterId > 0 &&
                                           (b.Status == "COMPLETED" || b.Status == "PAID"))
                                .OrderByDescending(b => b.CreatedAt)
                                .FirstOrDefault();

                            if (recentBooking != null && recentBooking.CenterId > 0)
                            {
                                var center = await centerRepo.GetCenterByIdAsync(recentBooking.CenterId);
                                if (center != null && center.IsActive)
                                {
                                    centerName = center.CenterName;
                                }
                            }

                            // Nếu không có, lấy center active đầu tiên
                            if (centerName == "Trung tâm gần nhất")
                            {
                                var activeCenters = await centerRepo.GetActiveCentersAsync();
                                var firstCenter = activeCenters.FirstOrDefault();
                                if (firstCenter != null)
                                {
                                    centerName = firstCenter.CenterName;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log nhưng không throw, dùng giá trị mặc định
                            var logger = scope.ServiceProvider.GetRequiredService<ILogger<MaintenanceReminderDispatcherService>>();
                            logger.LogWarning("Không thể lấy centerName cho reminder {ReminderId}, sử dụng giá trị mặc định. Error: {Error}", r.ReminderId, ex.Message ?? ex.ToString());
                        }
                        var dueDateFormatted = r.DueDate?.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy") ?? string.Empty;
                        var dueMileageFormatted = r.DueMileage?.ToString("N0") ?? string.Empty;
                        var cadenceDaysFormatted = r.CadenceDays?.ToString() ?? "7";
                        var year = DateTime.UtcNow.Year.ToString();
                        var supportPhone = config["AppSettings:SupportPhone"] ?? config["Support:Phone"] ?? "1900-xxxx";

                        var body = await templates.RenderAsync("MaintenanceReminder", new System.Collections.Generic.Dictionary<string, string>
                        {
                            ["vehicleId"] = r.VehicleId.ToString(),
                            ["vehicleName"] = vehicleName,
                            ["serviceId"] = (r.ServiceId?.ToString() ?? string.Empty),
                            ["serviceName"] = serviceName,
                            ["dueDate"] = dueDateFormatted,
                            ["dueMileage"] = dueMileageFormatted,
                            ["cadenceDays"] = cadenceDaysFormatted,
                            ["centerName"] = centerName,
                            ["fullName"] = fullName,
                            ["bookingUrl"] = bookingUrl,
                            ["year"] = year,
                            ["supportPhone"] = supportPhone
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


