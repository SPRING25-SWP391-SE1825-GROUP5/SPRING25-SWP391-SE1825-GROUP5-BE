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
				
				var timeoutMinutes = _configuration.GetValue<int>("Booking:PendingTimeoutMinutes", 30);
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
                        _logger.LogInformation($"Tự động hủy booking {b.BookingId} (Code: {b.BookingCode}) sau {elapsed:F1} phút");
                        // Nạp đầy đủ entity trước khi cập nhật để tránh ghi đè các FK bằng giá trị mặc định 0
                        var full = await bookingRepository.GetBookingByIdAsync(b.BookingId);
                        if (full != null)
                        {
                            if (full.Customer == null)
                            {
                                _logger.LogWarning($"Bỏ qua hủy booking {b.BookingId} vì thiếu Customer (tránh lỗi FK)");
                                continue;
                            }
                            full.Status = "CANCELLED";
                            full.UpdatedAt = now;
                            await bookingRepository.UpdateBookingAsync(full);
                            cancelledCount++;
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


