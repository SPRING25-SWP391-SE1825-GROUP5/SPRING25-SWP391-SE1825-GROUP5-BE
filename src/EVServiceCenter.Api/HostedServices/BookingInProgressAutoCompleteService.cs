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
                using var scope = _serviceScopeFactory.CreateScope();
                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                var options = _configuration
                    .GetSection(InProgressGuardOptions.SectionName)
                    .Get<InProgressGuardOptions>() ?? new InProgressGuardOptions();

                var now = DateTimeHelper.Now;
                var today = now.Date;
                var currentTime = TimeOnly.FromDateTime(now);

                var candidates = await bookingRepository.GetBookingsForAutoCompleteAsync(today);

                foreach (var booking in candidates)
                {
                    try
                    {
                        if (booking.TechnicianTimeSlot == null || booking.TechnicianTimeSlot.Slot == null)
                        {
                            continue;
                        }

                        var slotTime = booking.TechnicianTimeSlot.Slot.SlotTime;
                        var threshold = slotTime.AddMinutes(options.GraceAfterMinutes);

                        if (currentTime < threshold)
                        {
                            continue;
                        }

                        if (!string.Equals(booking.Status, BookingStatusConstants.InProgress, StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(booking.Status, BookingStatusConstants.CheckedIn, StringComparison.OrdinalIgnoreCase))
                        {
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
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Lỗi trong BookingInProgressAutoCompleteService");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
