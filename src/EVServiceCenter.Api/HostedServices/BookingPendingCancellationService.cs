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
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Api.HostedServices;

public class BookingPendingCancellationService : BackgroundService
{
	private readonly IServiceScopeFactory _serviceScopeFactory;
	private readonly IConfiguration _configuration;
	private readonly ILogger<BookingPendingCancellationService>? _logger;

	public BookingPendingCancellationService(
		IServiceScopeFactory serviceScopeFactory,
		IConfiguration configuration,
		ILogger<BookingPendingCancellationService>? logger = null)
	{
		_serviceScopeFactory = serviceScopeFactory;
		_configuration = configuration;
		_logger = logger;
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

				// 1. Hủy các booking PENDING quá thời gian chờ
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

							await bookingService.UpdateBookingStatusAsync(b.BookingId, updateRequest);
							_logger?.LogInformation("Đã tự động hủy booking {BookingId} do quá thời gian chờ (PENDING timeout)", b.BookingId);
						}
						catch (Exception ex)
						{
							_logger?.LogError(ex, "Lỗi khi hủy booking {BookingId} do quá thời gian chờ", b.BookingId);
						}
					}
				}

				// 2. Hủy các booking có WorkDate đã qua ngày hiện tại
				var expiredBookings = await bookingRepository.GetBookingsWithExpiredWorkDateAsync();
				foreach (var booking in expiredBookings)
				{
					try
					{
						var fullBooking = await bookingRepository.GetBookingByIdAsync(booking.BookingId);
						if (fullBooking == null)
						{
							continue;
						}

						// Kiểm tra lại status để tránh race condition
						if (fullBooking.Status == "CANCELLED" ||
						    fullBooking.Status == "COMPLETED" ||
						    fullBooking.Status == "PAID")
						{
							continue;
						}

						// Kiểm tra lại WorkDate
						if (fullBooking.TechnicianTimeSlot == null ||
						    fullBooking.TechnicianTimeSlot.WorkDate.Date >= DateTime.UtcNow.Date)
						{
							continue;
						}

						var updateRequest = new UpdateBookingStatusRequest
						{
							Status = "CANCELLED",
							ForceCancel = true // Bỏ qua validation khi hủy tự động do WorkDate đã qua
						};

						await bookingService.UpdateBookingStatusAsync(booking.BookingId, updateRequest);
						_logger?.LogInformation(
							"Đã tự động hủy booking {BookingId} do WorkDate đã qua (WorkDate: {WorkDate})",
							booking.BookingId,
							fullBooking.TechnicianTimeSlot.WorkDate.ToString("yyyy-MM-dd"));
					}
					catch (Exception ex)
					{
						_logger?.LogError(ex, "Lỗi khi hủy booking {BookingId} do WorkDate đã qua", booking.BookingId);
					}
				}
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "Lỗi trong BookingPendingCancellationService");
			}
			await Task.Delay(interval, stoppingToken);
		}
	}
}


