using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EVServiceCenter.Application.Configurations;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Infrastructure.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Api.HostedServices
{
    public class PromotionAppliedCleanupService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<PromotionAppliedCleanupService> _logger;
        private readonly PromotionOptions _options;

        public PromotionAppliedCleanupService(IServiceProvider services, IOptions<PromotionOptions> options, ILogger<PromotionAppliedCleanupService> logger)
        {
            _services = services;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PromotionAppliedCleanupService started with TTL {ttl} minutes", _options.AppliedTtlMinutes);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<EVDbContext>();

                    var nowUtc = DateTime.UtcNow;
                    var threshold = nowUtc.AddMinutes(-_options.AppliedTtlMinutes);

                    // Lấy tối đa 1000 bản ghi APPLIED đã quá hạn mỗi vòng
                    var ups = await db.UserPromotions
                        .Where(up => up.Status == "APPLIED" && up.UsedAt != default(DateTime) && up.UsedAt < threshold)
                        .OrderBy(up => up.UsedAt)
                        .Take(1000)
                        .ToListAsync(stoppingToken);

                    if (ups.Count > 0)
                    {
                        foreach (var up in ups)
                        {
                            up.OrderId = null;
                            up.BookingId = null;
                            up.Status = "SAVED";
                            up.DiscountAmount = 0;
                            up.UsedAt = nowUtc;
                        }
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PromotionAppliedCleanupService run failed");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}


