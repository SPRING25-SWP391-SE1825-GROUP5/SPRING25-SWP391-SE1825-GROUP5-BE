using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.McpServer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.SetMinimumLevel(LogLevel.Information);
                    });
                    
                    // Đăng ký các services của EVServiceCenter
                    services.AddScoped<EVServiceCenter.Application.Interfaces.IServiceService, EVServiceCenter.Application.Service.ServiceService>();
                    services.AddScoped<EVServiceCenter.Application.Interfaces.IBookingService, EVServiceCenter.Application.Service.BookingService>();
                    
                    // Đăng ký MCP Server
                    services.AddHostedService<McpServerService>();
                });
    }

    public class McpServerService : BackgroundService
    {
        private readonly ILogger<McpServerService> _logger;

        public McpServerService(ILogger<McpServerService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EVServiceCenter MCP Server đang khởi động...");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("MCP Server đang chạy...");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
