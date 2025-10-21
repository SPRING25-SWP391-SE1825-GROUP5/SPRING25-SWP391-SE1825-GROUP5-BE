using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Allure.Xunit.Attributes;
using EVServiceCenter.Api.Controllers;
using EVServiceCenter.Application.Configurations;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EVServiceCenter.Tests.Unit.Api.Controllers
{
    [AllureSuite("Reminder Controller")]
    [AllureLabel("layer", "api")]
    public class ReminderControllerTests
    {
        private readonly Mock<IMaintenanceReminderRepository> _repo;
        private readonly Mock<IEmailService> _email;
        private readonly Mock<IOptions<MaintenanceReminderOptions>> _optionsMock;

        private readonly ReminderController _controller;
        private readonly MaintenanceReminderOptions _options;

        public ReminderControllerTests()
        {
            _repo = new Mock<IMaintenanceReminderRepository>(MockBehavior.Strict);
            _email = new Mock<IEmailService>(MockBehavior.Strict);
            _optionsMock = new Mock<IOptions<MaintenanceReminderOptions>>(MockBehavior.Strict);

            _options = new MaintenanceReminderOptions
            {
                UpcomingDays = 7,
                AppointmentReminderHours = 24
            };
            _optionsMock.Setup(x => x.Value).Returns(_options);

            _controller = new ReminderController(_repo.Object, _email.Object, _optionsMock.Object);
        }

        [Fact]
        [AllureDescription("SetVehicleReminders should return BadRequest when vehicleId invalid")]
        public async Task SetVehicleReminders_InvalidVehicleId_ReturnsBadRequest()
        {
            var req = new SetVehicleRemindersRequest
            {
                Items = new List<SetVehicleReminderItem>
                {
                    new SetVehicleReminderItem { ServiceId = 1, DueMileage = 1000 }
                }
            };

            var result = await _controller.SetVehicleReminders(0, req);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        [AllureDescription("SetVehicleReminders should create reminders and return count")]
        public async Task SetVehicleReminders_ValidRequest_ReturnsOkWithAddedCount()
        {
            var req = new SetVehicleRemindersRequest
            {
                Items = new List<SetVehicleReminderItem>
                {
                    new SetVehicleReminderItem { ServiceId = 10, DueDate = DateTime.UtcNow.AddDays(10) },
                    new SetVehicleReminderItem { ServiceId = 11, DueMileage = 12345 }
                }
            };

            _repo
                .SetupSequence(r => r.CreateAsync(It.IsAny<MaintenanceReminder>()))
                .ReturnsAsync(new MaintenanceReminder { ReminderId = 1, VehicleId = 5, ServiceId = 10 })
                .ReturnsAsync(new MaintenanceReminder { ReminderId = 2, VehicleId = 5, ServiceId = 11 });

            var result = await _controller.SetVehicleReminders(5, req) as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value!;
            var addedProp = value.GetType().GetProperty("added");
            Assert.NotNull(addedProp);
            var added = (int)addedProp!.GetValue(value)!;
            Assert.Equal(2, added);
        }

        [Fact]
        [AllureDescription("GetVehicleAlerts filters by UpcomingDays window")]
        public async Task GetVehicleAlerts_Filters_By_Window()
        {
            var now = DateTime.UtcNow.Date;
            var within = new MaintenanceReminder { ReminderId = 1, VehicleId = 7, DueDate = DateOnly.FromDateTime(now.AddDays(3)) };
            var outside = new MaintenanceReminder { ReminderId = 2, VehicleId = 7, DueDate = DateOnly.FromDateTime(now.AddDays(30)) };

            _repo
                .Setup(r => r.QueryAsync(null, 7, "PENDING", null, null))
                .ReturnsAsync(new List<MaintenanceReminder> { within, outside });

            var result = await _controller.GetVehicleAlerts(7) as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value!;
            var dataProp = value.GetType().GetProperty("data");
            var list = ((IEnumerable<MaintenanceReminder>)dataProp!.GetValue(value)!).ToList();
            Assert.Single(list);
            Assert.Equal(1, list[0].ReminderId);
        }

        [Fact]
        [AllureDescription("Create returns BadRequest when VehicleId missing")]
        public async Task Create_InvalidRequest_ReturnsBadRequest()
        {
            var result = await _controller.Create(null);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        [AllureDescription("Create returns CreatedAt with created entity")]
        public async Task Create_ValidRequest_ReturnsCreated()
        {
            var req = new ReminderController.CreateReminderRequest
            {
                VehicleId = 9,
                ServiceId = 3,
                DueDate = DateTime.UtcNow.AddDays(5),
                DueMileage = 1111
            };

            var created = new MaintenanceReminder { ReminderId = 123, VehicleId = 9, ServiceId = 3 };
            _repo.Setup(r => r.CreateAsync(It.IsAny<MaintenanceReminder>())).ReturnsAsync(created);

            var result = await _controller.Create(req);
            var createdAt = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(ReminderController.GetById), createdAt.ActionName);
        }

        [Fact]
        [AllureDescription("GetById returns NotFound when missing")]
        public async Task GetById_NotFound()
        {
            _repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync((MaintenanceReminder)null);
            var result = await _controller.GetById(5);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        [AllureDescription("Update modifies fields and returns Ok")]
        public async Task Update_ValidRequest_UpdatesAndReturnsOk()
        {
            var existing = new MaintenanceReminder { ReminderId = 10, DueMileage = 100 };
            _repo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(existing);
            _repo.Setup(r => r.UpdateAsync(existing)).Returns(Task.CompletedTask);

            var req = new ReminderController.UpdateReminderRequest
            {
                DueMileage = 200
            };

            var result = await _controller.Update(10, req) as OkObjectResult;
            Assert.NotNull(result);
            Assert.Equal(200, ((MaintenanceReminder)((dynamic)result!.Value!).data).DueMileage);
        }

        [Fact]
        [AllureDescription("Complete sets IsCompleted and CompletedAt")]
        public async Task Complete_SetsCompleted_WhenNotCompleted()
        {
            var existing = new MaintenanceReminder { ReminderId = 22, IsCompleted = false };
            _repo.Setup(r => r.GetByIdAsync(22)).ReturnsAsync(existing);
            _repo.Setup(r => r.UpdateAsync(existing)).Returns(Task.CompletedTask);

            var result = await _controller.Complete(22) as OkObjectResult;
            Assert.NotNull(result);
            var data = ((dynamic)result!.Value!).data;
            Assert.True(((MaintenanceReminder)data).IsCompleted);
            Assert.NotNull(((MaintenanceReminder)data).CompletedAt);
        }

        [Fact]
        [AllureDescription("Snooze shifts DueDate by provided days or default option")]
        public async Task Snooze_Shifts_DueDate()
        {
            var existing = new MaintenanceReminder { ReminderId = 33, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date) };
            _repo.Setup(r => r.GetByIdAsync(33)).ReturnsAsync(existing);
            _repo.Setup(r => r.UpdateAsync(existing)).Returns(Task.CompletedTask);

            var result = await _controller.Snooze(33, new ReminderController.SnoozeRequest { Days = 5 }) as OkObjectResult;
            Assert.NotNull(result);
            var after = ((MaintenanceReminder)((dynamic)result!.Value!).data).DueDate;
            Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(5)), after);
        }

        [Fact]
        [AllureDescription("Upcoming calls repository with window filter")]
        public async Task Upcoming_Returns_List()
        {
            _repo.Setup(r => r.QueryAsync(null, null, "PENDING", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<MaintenanceReminder> { new MaintenanceReminder { ReminderId = 1 } });

            var result = await _controller.Upcoming(null) as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value!;
            var dataProp = value.GetType().GetProperty("data");
            var list = (IEnumerable<MaintenanceReminder>)dataProp!.GetValue(value)!;
            Assert.Single(list);
        }

        [Fact]
        [AllureDescription("SendTest sends email when available")]
        public async Task SendTest_Sends_Email_When_EmailAvailable()
        {
            var reminder = new MaintenanceReminder
            {
                ReminderId = 77,
                VehicleId = 2,
                Vehicle = new Vehicle
                {
                    Customer = new Customer
                    {
                        User = new User { Email = "test@example.com" }
                    }
                }
            };

            _repo.Setup(r => r.GetByIdAsync(77)).ReturnsAsync(reminder);
            _email.Setup(e => e.SendEmailAsync("test@example.com", It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.SendTest(77) as OkObjectResult;
            Assert.NotNull(result);
            _email.Verify(e => e.SendEmailAsync("test@example.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        [AllureDescription("Dispatch with Auto picks reminders and sends emails")]
        public async Task Dispatch_Auto_Sends_For_Reminders_With_Email()
        {
            var withEmail = new MaintenanceReminder
            {
                ReminderId = 1,
                Vehicle = new Vehicle { Customer = new Customer { User = new User { Email = "a@b.com" } } }
            };
            var noEmail = new MaintenanceReminder
            {
                ReminderId = 2,
                Vehicle = new Vehicle { Customer = new Customer { User = new User { Email = null } } }
            };

            _repo.Setup(r => r.QueryAsync(null, null, "PENDING", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<MaintenanceReminder> { withEmail, noEmail });
            _email.Setup(e => e.SendEmailAsync("a@b.com", It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.Dispatch(new ReminderController.DispatchRequest { Auto = true }) as OkObjectResult;
            Assert.NotNull(result);
            _email.Verify(e => e.SendEmailAsync("a@b.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            var value = result!.Value!;
            var sentProp = value.GetType().GetProperty("sent");
            var sent = (int)sentProp!.GetValue(value)!;
            Assert.Equal(1, sent);
        }
    }
}


