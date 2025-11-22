using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EVServiceCenter.Application.Configurations;
using EVServiceCenter.Application.Constants;
using EVServiceCenter.Application.Helpers;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Api.HostedServices;

public class BookingInProgressAutoCompleteService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BookingInProgressAutoCompleteService>? _logger;

    public BookingInProgressAutoCompleteService(
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration,
        ILogger<BookingInProgressAutoCompleteService>? logger = null)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(5);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBookingsWithRetryAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Lỗi nghiêm trọng trong BookingInProgressAutoCompleteService");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task ProcessBookingsWithRetryAsync(CancellationToken stoppingToken)
    {
        const int maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(10);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                var options = _configuration
                    .GetSection(InProgressGuardOptions.SectionName)
                    .Get<InProgressGuardOptions>() ?? new InProgressGuardOptions();

                var now = DateTimeHelper.Now;
                var today = now.Date;
                var currentTime = TimeOnly.FromDateTime(now);

                _logger?.LogDebug("Bắt đầu kiểm tra bookings cần auto-complete cho ngày {Date}", today);

                var candidates = await bookingRepository.GetBookingsForAutoCompleteAsync(today);

                _logger?.LogInformation("Tìm thấy {Count} bookings ứng viên để auto-complete", candidates.Count);

                foreach (var booking in candidates)
                {
                    try
                    {
                        if (booking.TechnicianTimeSlot == null || booking.TechnicianTimeSlot.Slot == null)
                        {
                            _logger?.LogWarning("Booking {BookingId} không có TechnicianTimeSlot hoặc Slot", booking.BookingId);
                            continue;
                        }

                        var slotTime = booking.TechnicianTimeSlot.Slot.SlotTime;
                        var threshold = slotTime.AddMinutes(options.GraceAfterMinutes);

                        if (currentTime < threshold)
                        {
                            _logger?.LogDebug("Booking {BookingId} chưa đến thời gian auto-complete (SlotTime: {SlotTime}, Threshold: {Threshold}, Now: {Now})", 
                                booking.BookingId, slotTime, threshold, currentTime);
                            continue;
                        }

                        if (!string.Equals(booking.Status, BookingStatusConstants.InProgress, StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(booking.Status, BookingStatusConstants.CheckedIn, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger?.LogDebug("Booking {BookingId} có status {Status} không phù hợp để auto-complete", booking.BookingId, booking.Status);
                            continue;
                        }

                        var updateRequest = new UpdateBookingStatusRequest
                        {
                            Status = BookingStatusConstants.Completed
                        };

                        await bookingService.UpdateBookingStatusAsync(booking.BookingId, updateRequest);

                        _logger?.LogInformation(
                            "Đã tự động chuyển booking {BookingId} sang COMPLETED do đã qua thời gian slot (SlotTime: {SlotTime}, Now: {Now})",
                            booking.BookingId,
                            slotTime.ToString("HH:mm"),
                            now.ToString("O"));
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Lỗi khi auto-complete booking {BookingId}", booking.BookingId);
                    }
                }

                // Thành công, thoát khỏi retry loop
                return;
            }
            catch (Exception ex) when (IsTransientError(ex))
            {
                _logger?.LogWarning(ex, "Lỗi tạm thời trong BookingInProgressAutoCompleteService (Lần thử {Attempt}/{MaxRetries})", attempt, maxRetries);
                
                if (attempt == maxRetries)
                {
                    _logger?.LogError(ex, "Đã thử {MaxRetries} lần nhưng vẫn gặp lỗi kết nối database", maxRetries);
                    throw;
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(retryDelay * attempt, stoppingToken);
                }
            }
        }
    }

    private static bool IsTransientError(Exception ex)
    {
        // Kiểm tra các loại lỗi tạm thời
        if (ex is Microsoft.Data.SqlClient.SqlException sqlEx)
        {
            // Các error numbers cho transient errors
            var transientErrorNumbers = new[] { 2, 20, 64, 233, 10053, 10054, 10060, 40197, 40501, 40613, 49918, 49919, 49920, 11001 };
            return transientErrorNumbers.Contains(sqlEx.Number);
        }

        if (ex is TimeoutException || ex is System.ComponentModel.Win32Exception)
        {
            return true;
        }

        return ex.Message.Contains("timeout") || 
               ex.Message.Contains("network") || 
               ex.Message.Contains("connection");
    }
}
