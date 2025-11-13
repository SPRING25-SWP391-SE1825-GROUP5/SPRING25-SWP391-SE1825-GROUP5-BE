using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EVServiceCenter.Api.HostedServices;

public class BookingPendingCancellationService : BackgroundService
{
	private readonly IServiceScopeFactory _serviceScopeFactory;
	private readonly IConfiguration _configuration;

	public BookingPendingCancellationService(
		IServiceScopeFactory serviceScopeFactory,
		IConfiguration configuration)
	{
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
				var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
				
				var timeoutMinutes = _configuration.GetValue<double>("Booking:PendingTimeoutMinutes", 30);
				var all = await bookingRepository.GetAllForAutoCancelAsync();
				var now = DateTime.UtcNow;
				
				foreach (var b in all.Where(b => string.Equals(b.Status, "PENDING", StringComparison.OrdinalIgnoreCase)))
				{
					var created = b.CreatedAt.Kind == DateTimeKind.Unspecified
						? DateTime.SpecifyKind(b.CreatedAt, DateTimeKind.Utc)
						: b.CreatedAt.ToUniversalTime();
					var threshold = now.AddMinutes(-timeoutMinutes);
					if (created <= threshold && (now - created) > TimeSpan.FromMinutes(2))
					{
						try
						{
							var fullBooking = await bookingRepository.GetBookingByIdAsync(b.BookingId);
							if (fullBooking == null)
							{
								continue;
							}
							
							if (fullBooking.Status != "PENDING")
							{
								continue;
							}
							
							var updateRequest = new UpdateBookingStatusRequest
							{
								Status = "CANCELLED"
							};
							
							var result = await bookingService.UpdateBookingStatusAsync(b.BookingId, updateRequest);
						}
						catch (Exception)
						{
						}
					}
				}
			}
			catch (Exception)
			{
			}
			await Task.Delay(interval, stoppingToken);
		}
	}
}


