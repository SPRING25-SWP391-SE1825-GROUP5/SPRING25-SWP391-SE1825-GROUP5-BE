using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EVServiceCenter.Infrastructure.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Api.HostedServices
{
    public class ServicePackageExpiredUpdateService : BackgroundService
    {
        private readonly IServiceProvider _services;

        public ServicePackageExpiredUpdateService(
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

                    var now = DateTime.UtcNow;
                    
                    var affectedRows = await db.Database.ExecuteSqlInterpolatedAsync(
                        $@"UPDATE [dbo].[ServicePackages] 
                          SET [IsActive] = 0, [UpdatedAt] = {now}
                          WHERE [IsActive] = 1 
                          AND [ValidTo] IS NOT NULL 
                          AND [ValidTo] < {now}",
                        stoppingToken);

                    if (affectedRows > 0)
                    {
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

