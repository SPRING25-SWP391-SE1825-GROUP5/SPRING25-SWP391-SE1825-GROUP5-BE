using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Service;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Xunit;

namespace EVServiceCenter.Tests.Unit.Application.Services;

public class TimeSlotServiceTests
{
    private class InMemoryTimeSlotRepository : ITimeSlotRepository
    {
        private readonly List<TimeSlot> _items = new();

        public Task<TimeSlot> CreateTimeSlotAsync(TimeSlot timeSlot)
        {
            timeSlot.SlotId = _items.Count == 0 ? 1 : _items.Max(x => x.SlotId) + 1;
            _items.Add(timeSlot);
            return Task.FromResult(timeSlot);
        }

        public Task<bool> DeleteAsync(int slotId)
        {
            var ts = _items.FirstOrDefault(x => x.SlotId == slotId);
            if (ts == null) return Task.FromResult(false);
            _items.Remove(ts);
            return Task.FromResult(true);
        }

        public Task<List<TimeSlot>> GetActiveTimeSlotsAsync()
        {
            return Task.FromResult(_items.Where(x => x.IsActive).OrderBy(x => x.SlotTime).ToList());
        }

        public Task<List<TimeSlot>> GetAllTimeSlotsAsync()
        {
            return Task.FromResult(_items.OrderBy(x => x.SlotTime).ToList());
        }

        public Task<TimeSlot> GetByIdAsync(int slotId)
        {
            return Task.FromResult(_items.FirstOrDefault(x => x.SlotId == slotId));
        }

        public Task<TimeSlot> UpdateAsync(TimeSlot timeSlot)
        {
            var idx = _items.FindIndex(x => x.SlotId == timeSlot.SlotId);
            if (idx >= 0) _items[idx] = timeSlot;
            return Task.FromResult(timeSlot);
        }
    }

    [Fact]
    public async Task CreateTimeSlot_Should_Succeed_For_New_TimeAndLabel()
    {
        var repo = new InMemoryTimeSlotRepository();
        var service = new TimeSlotService(repo);

        var request = new CreateTimeSlotRequest
        {
            SlotTime = new TimeOnly(8, 0),
            SlotLabel = "08:00-08:30",
            IsActive = true
        };

        var created = await service.CreateTimeSlotAsync(request);

        Assert.NotNull(created);
        Assert.Equal("08:00-08:30", created.SlotLabel);
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task CreateTimeSlot_Should_Throw_When_Duplicate_Time_Or_Label()
    {
        var repo = new InMemoryTimeSlotRepository();
        var service = new TimeSlotService(repo);

        await service.CreateTimeSlotAsync(new CreateTimeSlotRequest
        {
            SlotTime = new TimeOnly(8, 0),
            SlotLabel = "08:00-08:30",
            IsActive = true
        });

        // Trùng thời gian
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.CreateTimeSlotAsync(new CreateTimeSlotRequest
            {
                SlotTime = new TimeOnly(8, 0),
                SlotLabel = "08:00-08:30 khác",
                IsActive = true
            }));

        // Trùng nhãn
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.CreateTimeSlotAsync(new CreateTimeSlotRequest
            {
                SlotTime = new TimeOnly(8, 30),
                SlotLabel = "08:00-08:30",
                IsActive = true
            }));
    }
}


