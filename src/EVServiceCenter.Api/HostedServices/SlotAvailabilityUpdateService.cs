using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Application.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Api.HostedServices;

/// <summary>
/// Background service tự động cập nhật slot availability khi qua giờ
/// Chạy mỗi 5 phút để kiểm tra và cập nhật các slot đã qua giờ
/// </summary>
public class SlotAvailabilityUpdateService : BackgroundService
{
    private readonly ILogger<SlotAvailabilityUpdateService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public SlotAvailabilityUpdateService(
        ILogger<SlotAvailabilityUpdateService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(5); // Chạy mỗi 5 phút
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var technicianTimeSlotRepository = scope.ServiceProvider.GetRequiredService<ITechnicianTimeSlotRepository>();
                
                var now = DateTimeHelper.Now; // Sử dụng giờ Việt Nam
                var today = DateOnly.FromDateTime(now);
                var currentTime = TimeOnly.FromDateTime(now);
                
                _logger.LogInformation($"Kiểm tra slot availability - Ngày: {today:yyyy-MM-dd}, Giờ hiện tại: {currentTime:HH:mm}");
                
                // Lấy các slot đã qua giờ nhưng vẫn available
                var expiredSlots = await technicianTimeSlotRepository.GetExpiredAvailableSlotsAsync(today, currentTime);
                
                if (expiredSlots.Any())
                {
                    _logger.LogInformation($"Tìm thấy {expiredSlots.Count} slot đã qua giờ cần cập nhật");
                    
                    var updatedCount = 0;
                    foreach (var slot in expiredSlots)
                    {
                        try
                        {
                            slot.IsAvailable = false;
                            await technicianTimeSlotRepository.UpdateAsync(slot);
                            updatedCount++;
                            
                            _logger.LogInformation($"Đã cập nhật slot {slot.TechnicianSlotId} (Technician: {slot.TechnicianId}, Slot: {slot.SlotId}, WorkDate: {slot.WorkDate:yyyy-MM-dd}) thành unavailable");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Lỗi khi cập nhật slot {slot.TechnicianSlotId}");
                        }
                    }
                    
                    if (updatedCount > 0)
                    {
                        _logger.LogInformation($"Đã cập nhật {updatedCount} slot thành unavailable do qua giờ");
                    }
                }
                else
                {
                    _logger.LogDebug("Không có slot nào cần cập nhật");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật slot availability");
            }
            
            await Task.Delay(interval, stoppingToken);
        }
    }
}
