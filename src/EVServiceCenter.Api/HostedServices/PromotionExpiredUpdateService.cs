using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Infrastructure.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Api.HostedServices
{
    public class PromotionExpiredUpdateService : BackgroundService
    {
        private readonly IServiceProvider _services;

        public PromotionExpiredUpdateService(
            IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<EVDbContext>();

                    var today = DateOnly.FromDateTime(DateTime.Today);
                    
                    var expiredPromotions = await db.Promotions
                        .Where(p => p.Status == "ACTIVE" && 
                                   ((p.EndDate.HasValue && p.EndDate.Value < today) ||
                                    (p.UsageLimit.HasValue && p.UsageCount >= p.UsageLimit.Value)))
                        .ToListAsync(stoppingToken);

                    if (expiredPromotions.Count > 0)
                    {
                        foreach (var promotion in expiredPromotions)
                        {
                            promotion.Status = "EXPIRED";
                            promotion.UpdatedAt = DateTime.UtcNow;
                        }

                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception)
                {
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}

