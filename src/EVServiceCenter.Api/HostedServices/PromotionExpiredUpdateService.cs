using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Infrastructure.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Api.HostedServices
{
    /// <summary>
    /// Background service tự động cập nhật status EXPIRED cho các promotion đã hết hạn
    /// Chạy mỗi giờ một lần
    /// </summary>
    public class PromotionExpiredUpdateService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<PromotionExpiredUpdateService> _logger;

        public PromotionExpiredUpdateService(
            IServiceProvider services,
            ILogger<PromotionExpiredUpdateService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PromotionExpiredUpdateService started");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<EVDbContext>();

                    var today = DateOnly.FromDateTime(DateTime.Today);
                    
                    // Lấy tất cả promotions ACTIVE đã hết hạn hoặc hết lượt sử dụng
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
                        _logger.LogInformation("Đã cập nhật {Count} promotion(s) thành EXPIRED", expiredPromotions.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PromotionExpiredUpdateService run failed");
                }

                // Chạy mỗi giờ một lần
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}

