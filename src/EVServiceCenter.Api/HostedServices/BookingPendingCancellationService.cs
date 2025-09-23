using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Api.HostedServices;

public class BookingPendingCancellationService : BackgroundService
{
	private readonly ILogger<BookingPendingCancellationService> _logger;
	private readonly IServiceScopeFactory _serviceScopeFactory;
	private readonly IConfiguration _configuration;

	public BookingPendingCancellationService(ILogger<BookingPendingCancellationService> logger,
		IServiceScopeFactory serviceScopeFactory,
		IConfiguration configuration)
	{
		_logger = logger;
		_serviceScopeFactory = serviceScopeFactory;
		_configuration = configuration;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var interval = TimeSpan.FromMinutes(1);
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				using var scope = _serviceScopeFactory.CreateScope();
				var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
				
				var timeoutMinutes = _configuration.GetValue<int>("Booking:PendingTimeoutMinutes", 5);
				var all = await bookingRepository.GetAllBookingsAsync();
				var now = DateTime.UtcNow;
				foreach (var b in all.Where(b => b.Status == "PENDING"))
				{
					var elapsed = now - b.CreatedAt.ToUniversalTime();
					if (elapsed.TotalMinutes >= timeoutMinutes)
					{
						b.Status = "CANCELLED";
						await bookingRepository.UpdateBookingAsync(b);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi khi tự hủy booking PENDING quá hạn");
			}
			await Task.Delay(interval, stoppingToken);
		}
	}
}


