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

    [Fact]
    public async Task GetTechnicianSkills_Should_Return_List_Of_Skills()
    {
        // Arrange
        var techService = new Mock<ITechnicianService>(MockBehavior.Strict);
        var timeSlotService = new Mock<ITimeSlotService>(MockBehavior.Strict);
        var bookingService = new Mock<IBookingService>(MockBehavior.Strict);

        var sample = new List<TechnicianSkillResponse>
        {
            new TechnicianSkillResponse { TechnicianId = 10, TechnicianName = "Tech A", SkillId = 1, SkillName = "Skill 1", SkillDescription = "Desc 1", Notes = null },
            new TechnicianSkillResponse { TechnicianId = 10, TechnicianName = "Tech A", SkillId = 2, SkillName = "Skill 2", SkillDescription = "Desc 2", Notes = "Note" },
        };

        techService
            .Setup(x => x.GetTechnicianSkillsAsync(10))
            .ReturnsAsync(sample);

        var controller = new TechnicianController(techService.Object, timeSlotService.Object, bookingService.Object);

        // Act
        var result = await controller.GetTechnicianSkills(10) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result!.Value!;
        var successProp = value.GetType().GetProperty("success");
        var dataProp = value.GetType().GetProperty("data");
        var totalProp = value.GetType().GetProperty("total");
        Assert.NotNull(successProp);
        Assert.NotNull(dataProp);
        Assert.NotNull(totalProp);
        Assert.True((bool)successProp!.GetValue(value)!);
        var data = (IEnumerable<TechnicianSkillResponse>)dataProp!.GetValue(value)!;
        var total = (int)totalProp!.GetValue(value)!;
        Assert.Equal(2, total);
        Assert.Equal(2, data.Count());

        techService.Verify(x => x.GetTechnicianSkillsAsync(10), Times.Once);
    }

    [Fact]
    public async Task UpsertSkills_Should_Return_Ok_On_Success()
    {
        // Arrange
        var techService = new Mock<ITechnicianService>(MockBehavior.Strict);
        var timeSlotService = new Mock<ITimeSlotService>(MockBehavior.Strict);
        var bookingService = new Mock<IBookingService>(MockBehavior.Strict);

        var request = new UpsertTechnicianSkillsRequest
        {
            Items = new List<UpsertTechnicianSkillItem>
            {
                new UpsertTechnicianSkillItem{ SkillId = 3, Notes = "good" },
                new UpsertTechnicianSkillItem{ SkillId = 4 }
            }
        };

        techService
            .Setup(x => x.UpsertSkillsAsync(15, request))
            .Returns(Task.CompletedTask);

        var controller = new TechnicianController(techService.Object, timeSlotService.Object, bookingService.Object);

        // Act
        var result = await controller.UpsertSkills(15, request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result!.Value!;
        var successProp = value.GetType().GetProperty("success");
        Assert.True((bool)successProp!.GetValue(value)!);
        techService.Verify(x => x.UpsertSkillsAsync(15, request), Times.Once);
    }

    [Fact]
    public async Task UpsertSkills_Should_Return_BadRequest_On_Invalid_Model()
    {
        // Arrange
        var techService = new Mock<ITechnicianService>(MockBehavior.Strict);
        var timeSlotService = new Mock<ITimeSlotService>(MockBehavior.Strict);
        var bookingService = new Mock<IBookingService>(MockBehavior.Strict);

        var controller = new TechnicianController(techService.Object, timeSlotService.Object, bookingService.Object);
        controller.ModelState.AddModelError("Items", "Danh sách kỹ năng không được rỗng");

        var request = new UpsertTechnicianSkillsRequest { Items = new List<UpsertTechnicianSkillItem>() };

        // Act
        var result = await controller.UpsertSkills(20, request) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        var value = result!.Value!;
        var successProp = value.GetType().GetProperty("success");
        Assert.False((bool)successProp!.GetValue(value)!);
        techService.Verify(x => x.UpsertSkillsAsync(It.IsAny<int>(), It.IsAny<UpsertTechnicianSkillsRequest>()), Times.Never);
    }
}


