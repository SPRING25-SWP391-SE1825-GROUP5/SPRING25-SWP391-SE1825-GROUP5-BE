using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Application.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EVServiceCenter.Api.HostedServices;

public class SlotAvailabilityUpdateService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public SlotAvailabilityUpdateService(
        IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(5);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var technicianTimeSlotRepository = scope.ServiceProvider.GetRequiredService<ITechnicianTimeSlotRepository>();
                
                var now = DateTimeHelper.Now;
                var today = DateOnly.FromDateTime(now);
                var currentTime = TimeOnly.FromDateTime(now);
                
                var expiredSlots = await technicianTimeSlotRepository.GetExpiredAvailableSlotsAsync(today, currentTime);
                
                if (expiredSlots.Any())
                {
                    foreach (var slot in expiredSlots)
                    {
                        try
                        {
                            slot.IsAvailable = false;
                            await technicianTimeSlotRepository.UpdateAsync(slot);
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
