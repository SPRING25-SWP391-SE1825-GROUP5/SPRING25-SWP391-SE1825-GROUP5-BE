using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EVServiceCenter.Infrastructure.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Api.HostedServices
{
    /// <summary>
    /// Background service tự động cập nhật IsActive = false cho các service package đã hết hạn
    /// Chạy mỗi giờ một lần
    /// </summary>
    public class ServicePackageExpiredUpdateService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ServicePackageExpiredUpdateService> _logger;

        public ServicePackageExpiredUpdateService(
            IServiceProvider services,
            ILogger<ServicePackageExpiredUpdateService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ServicePackageExpiredUpdateService started");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<EVDbContext>();

                    var now = DateTime.UtcNow;
                    
                    // Cập nhật trực tiếp bằng SQL để tránh lỗi với trigger
                    // Dùng FormattableString với ExecuteSqlInterpolatedAsync để tránh SQL injection
                    var affectedRows = await db.Database.ExecuteSqlInterpolatedAsync(
                        $@"UPDATE [dbo].[ServicePackages] 
                          SET [IsActive] = 0, [UpdatedAt] = {now}
                          WHERE [IsActive] = 1 
                          AND [ValidTo] IS NOT NULL 
                          AND [ValidTo] < {now}",
                        stoppingToken);

                    if (affectedRows > 0)
                    {
                        _logger.LogInformation("Đã cập nhật {Count} service package(s) thành INACTIVE (hết hạn)", affectedRows);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ServicePackageExpiredUpdateService run failed");
                }

                // Chạy mỗi giờ một lần
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}

