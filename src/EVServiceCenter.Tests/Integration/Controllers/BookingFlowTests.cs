using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Api;
using EVServiceCenter.Application.Configurations;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Application.Service;
using EVServiceCenter.WebAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EVServiceCenter.Tests.Integration.Controllers;

public class BookingFlowTests
{
    private static BookingController CreateController(
        IBookingService bookingService,
        IBookingHistoryService historyService,
        EVServiceCenter.Application.Interfaces.IHoldStore holdStore,
        IGuestBookingService guestService)
    {
        var hubMock = new Mock<IHubContext<BookingHub>>(MockBehavior.Loose);
        var opts = Options.Create(new BookingRealtimeOptions { HoldTtlMinutes = 1 });
        return new BookingController(bookingService, historyService, holdStore, hubMock.Object, opts, guestService);
    }

    [Fact]
    public async Task Availability_Should_Return_Ok_With_Data()
    {
        var bookingSvc = new Mock<IBookingService>();
        bookingSvc.Setup(x => x.GetAvailabilityAsync(1, new DateOnly(2025, 10, 15), It.IsAny<List<int>>()))
            .ReturnsAsync(new AvailabilityResponse
            {
                CenterId = 1,
                Date = new DateOnly(2025, 10, 15),
                TimeSlots = new List<TimeSlotAvailability>
                {
                    new TimeSlotAvailability{ SlotId = 1, IsAvailable = true }
                }
            });

        var controller = CreateController(bookingSvc.Object, Mock.Of<IBookingHistoryService>(), new InMemoryHoldStore(), Mock.Of<IGuestBookingService>());

        var result = await controller.GetAvailability(1, "2025-10-15", "1");
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task GuestBooking_Should_Return_Ok()
    {
        var guestSvc = new Mock<IGuestBookingService>(MockBehavior.Strict);
        guestSvc
            .Setup(x => x.CreateGuestBookingAsync(It.IsAny<GuestBookingRequest>()))
            .ReturnsAsync(new GuestBookingResponse { BookingId = 99, CheckoutUrl = "https://pay" });

        var controller = CreateController(Mock.Of<IBookingService>(), Mock.Of<IBookingHistoryService>(), new InMemoryHoldStore(), guestSvc.Object);

        var req = new GuestBookingRequest
        {
            CenterId = 1,
            FullName = "Guest",
            PhoneNumber = "0900000000",
            BookingDate = new DateOnly(2025, 10, 16),
            SlotId = 2,
            ServiceId = 1,
            LicensePlate = "ABC-12345"
        };

        var result = await controller.CreateGuestBooking(req);
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public void Hold_Race_Should_Allow_First_And_Reject_Second()
    {
        var store = new InMemoryHoldStore();
        var date = new DateOnly(2025, 10, 17);

        var ok1 = store.TryHold(1, date, slotId: 3, technicianId: 7, customerId: 1, ttl: TimeSpan.FromSeconds(1), out _);
        var ok2 = store.TryHold(1, date, slotId: 3, technicianId: 7, customerId: 2, ttl: TimeSpan.FromSeconds(1), out _);

        Assert.True(ok1);
        Assert.False(ok2);
    }
}


