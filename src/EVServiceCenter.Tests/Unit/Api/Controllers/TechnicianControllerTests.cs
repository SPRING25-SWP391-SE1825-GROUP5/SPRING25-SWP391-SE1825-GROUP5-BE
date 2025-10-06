using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.WebAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EVServiceCenter.Tests.Unit.Api.Controllers;

public class TechnicianControllerTests
{
    [Fact]
    public async Task AvailabilityByService_Should_Return_SlotIds_For_Technician()
    {
        var techService = new Mock<ITechnicianService>(MockBehavior.Strict);
        var timeSlotService = new Mock<ITimeSlotService>(MockBehavior.Strict);
        var bookingService = new Mock<IBookingService>(MockBehavior.Strict);

        bookingService
            .Setup(x => x.GetAvailabilityAsync(1, new DateOnly(2025, 10, 10), It.IsAny<List<int>>()))
            .ReturnsAsync(new AvailabilityResponse
            {
                Date = new DateOnly(2025, 10, 10),
                TimeSlots = new List<TimeSlotAvailability>
                {
                    new TimeSlotAvailability { SlotId = 1, IsAvailable = true, AvailableTechnicians = new List<TechnicianAvailability>{ new TechnicianAvailability{ TechnicianId = 5, IsAvailable = true } } },
                    new TimeSlotAvailability { SlotId = 2, IsAvailable = true, AvailableTechnicians = new List<TechnicianAvailability>{ new TechnicianAvailability{ TechnicianId = 6, IsAvailable = true } } },
                    new TimeSlotAvailability { SlotId = 3, IsAvailable = false }
                }
            });

        var controller = new TechnicianController(techService.Object, timeSlotService.Object, bookingService.Object);

        var result = await controller.GetTechnicianAvailabilityByService(5, 1, "2025-10-10", 9) as OkObjectResult;

        Assert.NotNull(result);
        var value = result!.Value!;
        var successProp = value.GetType().GetProperty("success");
        var dataProp = value.GetType().GetProperty("data");
        Assert.NotNull(successProp);
        Assert.NotNull(dataProp);
        var success = (bool)successProp!.GetValue(value);
        var dataObj = dataProp!.GetValue(value)!;
        var slotIdsProp = dataObj.GetType().GetProperty("slotIds");
        var slotIds = (IEnumerable<int>)slotIdsProp!.GetValue(dataObj)!;
        Assert.True(success);
        Assert.Contains(1, slotIds);
        Assert.DoesNotContain(2, slotIds);
    }

    [Fact]
    public async Task CenterTechniciansAvailability_Should_Group_By_Technician()
    {
        var techService = new Mock<ITechnicianService>(MockBehavior.Strict);
        var timeSlotService = new Mock<ITimeSlotService>(MockBehavior.Strict);
        var bookingService = new Mock<IBookingService>(MockBehavior.Strict);

        bookingService
            .Setup(x => x.GetAvailabilityAsync(3, new DateOnly(2025, 10, 11), It.IsAny<List<int>>()))
            .ReturnsAsync(new AvailabilityResponse
            {
                Date = new DateOnly(2025, 10, 11),
                TimeSlots = new List<TimeSlotAvailability>
                {
                    new TimeSlotAvailability { SlotId = 1, IsAvailable = true, AvailableTechnicians = new List<TechnicianAvailability>{ new TechnicianAvailability{ TechnicianId = 7, IsAvailable = true } } },
                    new TimeSlotAvailability { SlotId = 2, IsAvailable = true, AvailableTechnicians = new List<TechnicianAvailability>{ new TechnicianAvailability{ TechnicianId = 8, IsAvailable = true }, new TechnicianAvailability{ TechnicianId = 7, IsAvailable = true } } },
                }
            });

        var controller = new TechnicianController(techService.Object, timeSlotService.Object, bookingService.Object);

        var result = await controller.GetCenterTechniciansAvailability(3, "2025-10-11", 12) as OkObjectResult;

        Assert.NotNull(result);
        var value = result!.Value!;
        var successProp = value.GetType().GetProperty("success");
        var dataProp = value.GetType().GetProperty("data");
        Assert.NotNull(successProp);
        Assert.NotNull(dataProp);
        var success = (bool)successProp!.GetValue(value);
        var dataObj = dataProp!.GetValue(value)!;
        var techniciansProp = dataObj.GetType().GetProperty("technicians");
        var technicians = (IDictionary<int, List<int>>)techniciansProp!.GetValue(dataObj)!;
        Assert.True(success);
        Assert.True(technicians.ContainsKey(7));
        Assert.Contains(1, technicians[7]);
        Assert.Contains(2, technicians[7]);
        Assert.True(technicians.ContainsKey(8));
        Assert.Contains(2, technicians[8]);
    }
}


