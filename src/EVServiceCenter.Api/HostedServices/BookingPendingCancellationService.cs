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
				var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
				
				var timeoutMinutes = _configuration.GetValue<double>("Booking:PendingTimeoutMinutes", 30);
                // Dùng truy vấn tối giản, tránh include để không đụng các cột NULL bắt buộc từ bảng liên quan
                var all = await bookingRepository.GetAllForAutoCancelAsync();
                var now = DateTime.UtcNow;
				var cancelledCount = 0;
				
                foreach (var b in all.Where(b => string.Equals(b.Status, "PENDING", StringComparison.OrdinalIgnoreCase)))
				{
                    // Chuẩn hóa CreatedAt về UTC nếu cần
                    var created = b.CreatedAt.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(b.CreatedAt, DateTimeKind.Utc)
                        : b.CreatedAt.ToUniversalTime();
                    var threshold = now.AddMinutes(-timeoutMinutes);
                    // Không hủy booking mới tạo trong 2 phút đầu để tránh lệch đồng hồ
                    if (created <= threshold && (now - created) > TimeSpan.FromMinutes(2))
					{
                        var elapsed = (now - created).TotalMinutes;
                        _logger.LogInformation($"Tự động hủy booking #{b.BookingId} sau {elapsed:F1} phút");
                        // Kiểm tra booking còn tồn tại và có thể hủy không
                        try
                        {
                            var fullBooking = await bookingRepository.GetBookingByIdAsync(b.BookingId);
                            if (fullBooking == null)
                            {
                                _logger.LogWarning($"Booking #{b.BookingId} không tồn tại, bỏ qua");
                                continue;
                            }
                            
                            if (fullBooking.Status != "PENDING")
                            {
                                _logger.LogInformation($"Booking #{b.BookingId} đã có status {fullBooking.Status}, bỏ qua");
                                continue;
                            }
                            
                            var updateRequest = new UpdateBookingStatusRequest
                            {
                                Status = "CANCELLED"
                            };
                            
                            var result = await bookingService.UpdateBookingStatusAsync(b.BookingId, updateRequest);
                            if (result != null)
                            {
                                cancelledCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Lỗi khi tự động hủy booking #{b.BookingId}");
                        }
					}
				}
				
				if (cancelledCount > 0)
				{
					_logger.LogInformation($"Đã tự động hủy {cancelledCount} booking PENDING quá hạn");
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


