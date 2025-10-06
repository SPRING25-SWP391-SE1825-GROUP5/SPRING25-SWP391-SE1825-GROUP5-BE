using System;
using System.Linq;
using System.Threading;
using EVServiceCenter.Application.Service;
using Xunit;

namespace EVServiceCenter.Tests.Unit.Application.Services;

public class HoldStoreTests
{
    [Fact]
    public void TryHold_Should_Succeed_Then_Second_Hold_Fails_For_Same_TechSlot()
    {
        var store = new InMemoryHoldStore();
        var centerId = 1;
        var date = new DateOnly(2025, 10, 15);
        var slotId = 3;
        var techId = 7;

        var ok1 = store.TryHold(centerId, date, slotId, techId, customerId: 101, ttl: TimeSpan.FromSeconds(2), out _);
        var ok2 = store.TryHold(centerId, date, slotId, techId, customerId: 102, ttl: TimeSpan.FromSeconds(2), out _);

        Assert.True(ok1);
        Assert.False(ok2);
        Assert.True(store.IsHeld(centerId, date, slotId, techId));
    }

    [Fact]
    public void Hold_Should_Expire_After_TTL()
    {
        var store = new InMemoryHoldStore();
        var centerId = 1;
        var date = new DateOnly(2025, 10, 15);
        var slotId = 4;
        var techId = 9;

        var ok = store.TryHold(centerId, date, slotId, techId, customerId: 101, ttl: TimeSpan.FromMilliseconds(100), out _);
        Assert.True(ok);
        Thread.Sleep(200);
        Assert.False(store.IsHeld(centerId, date, slotId, techId));
    }
}


