using Xunit;
using Moq;
using Allure.Xunit.Attributes;
using EVServiceCenter.Api.Controllers;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Application.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace EVServiceCenter.Tests.Unit.Api.Controllers
{
    [AllureSuite("Maintenance Reminder Controller")]
    [AllureLabel("layer", "api")]
    public class MaintenanceReminderControllerTests
    {
        private readonly Mock<IMaintenanceReminderRepository> _maintenanceReminderRepositoryMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IOptions<MaintenanceReminderOptions>> _optionsMock;
        private readonly Mock<IVehicleRepository> _vehicleRepositoryMock;
        private readonly Mock<IServiceService> _serviceServiceMock;
        private readonly Mock<IBookingRepository> _bookingRepositoryMock;

        private readonly MaintenanceReminderController _controller;
        private readonly MaintenanceReminderOptions _options;

        public MaintenanceReminderControllerTests()
        {
            _maintenanceReminderRepositoryMock = new Mock<IMaintenanceReminderRepository>();
            _emailServiceMock = new Mock<IEmailService>();
            _optionsMock = new Mock<IOptions<MaintenanceReminderOptions>>();
            _vehicleRepositoryMock = new Mock<IVehicleRepository>();
            _serviceServiceMock = new Mock<IServiceService>();
            _bookingRepositoryMock = new Mock<IBookingRepository>();

            _options = new MaintenanceReminderOptions
            {
                UpcomingDays = 7
            };
            _optionsMock.Setup(x => x.Value).Returns(_options);

            _controller = new MaintenanceReminderController(
                _maintenanceReminderRepositoryMock.Object,
                _emailServiceMock.Object,
                _optionsMock.Object,
                _vehicleRepositoryMock.Object,
                _serviceServiceMock.Object,
                _bookingRepositoryMock.Object
            );
        }

        #region CreateVehicleServiceReminders Tests

        [Fact]
        [AllureDescription("Should create vehicle service reminders successfully")]
        [AllureTag("CreateVehicleServiceReminders")]
        public async Task CreateVehicleServiceReminders_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new CreateVehicleServiceRemindersRequest
            {
                VehicleId = 1,
                Reminders = new List<CreateVehicleServiceReminderItem>
                {
                    new CreateVehicleServiceReminderItem
                    {
                        ServiceId = 1,
                        DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                        DueMileage = 50000
                    }
                }
            };

            var vehicle = new Vehicle
            {
                VehicleId = 1,
                LicensePlate = "ABC-123",
                Vin = "VIN123456",
                CustomerId = 1
            };

            var service = new ServiceResponse
            {
                ServiceId = 1,
                ServiceName = "Thay dầu động cơ",
                Description = "Thay dầu và lọc dầu động cơ"
            };

            var createdReminder = new MaintenanceReminder
            {
                ReminderId = 1,
                VehicleId = 1,
                ServiceId = 1,
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                DueMileage = 50000,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };

            _vehicleRepositoryMock.Setup(x => x.GetVehicleByIdAsync(1))
                .ReturnsAsync(vehicle);
            _serviceServiceMock.Setup(x => x.GetServiceByIdAsync(1))
                .ReturnsAsync(service);
            _maintenanceReminderRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<MaintenanceReminder>()))
                .ReturnsAsync(createdReminder);

            // Act
            var result = await _controller.CreateVehicleServiceReminders(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<CreateVehicleServiceRemindersResponse>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.Equal(1, response.CreatedRemindersCount);
            Assert.Equal("ABC-123", response.VehicleLicensePlate);
            Assert.Single(response.CreatedReminders);
            Assert.Equal(1, response.CreatedReminders.First().ReminderId);
        }

        [Fact]
        [AllureDescription("Should return NotFound when vehicle does not exist")]
        [AllureTag("CreateVehicleServiceReminders")]
        public async Task CreateVehicleServiceReminders_VehicleNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new CreateVehicleServiceRemindersRequest
            {
                VehicleId = 999,
                Reminders = new List<CreateVehicleServiceReminderItem>()
            };

            _vehicleRepositoryMock.Setup(x => x.GetVehicleByIdAsync(999))
                .ReturnsAsync((Vehicle)null);

            // Act
            var result = await _controller.CreateVehicleServiceReminders(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;
            var successProperty = response.GetType().GetProperty("success");
            var messageProperty = response.GetType().GetProperty("message");
            Assert.False((bool)successProperty.GetValue(response));
            Assert.Equal("Không tìm thấy xe với ID đã cho", (string)messageProperty.GetValue(response));
        }

        [Fact]
        [AllureDescription("Should return BadRequest when service does not exist")]
        [AllureTag("CreateVehicleServiceReminders")]
        public async Task CreateVehicleServiceReminders_ServiceNotFound_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateVehicleServiceRemindersRequest
            {
                VehicleId = 1,
                Reminders = new List<CreateVehicleServiceReminderItem>
                {
                    new CreateVehicleServiceReminderItem { ServiceId = 999 }
                }
            };

            var vehicle = new Vehicle { VehicleId = 1, LicensePlate = "ABC-123" };

            _vehicleRepositoryMock.Setup(x => x.GetVehicleByIdAsync(1))
                .ReturnsAsync(vehicle);
            _serviceServiceMock.Setup(x => x.GetServiceByIdAsync(999))
                .ReturnsAsync((ServiceResponse)null);

            // Act
            var result = await _controller.CreateVehicleServiceReminders(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            var successProperty = response.GetType().GetProperty("success");
            var messageProperty = response.GetType().GetProperty("message");
            Assert.False((bool)successProperty.GetValue(response));
            Assert.Contains("Không tìm thấy dịch vụ với ID: 999", (string)messageProperty.GetValue(response));
        }

        #endregion

        #region GetCustomerVehicleServiceReminders Tests

        [Fact]
        [AllureDescription("Should get customer vehicle service reminders successfully")]
        [AllureTag("GetCustomerVehicleServiceReminders")]
        public async Task GetCustomerVehicleServiceReminders_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new GetCustomerVehicleServiceRemindersRequest
            {
                CustomerId = 1,
                Page = 1,
                PageSize = 10
            };

            var vehicles = new List<Vehicle>
            {
                new Vehicle
                {
                    VehicleId = 1,
                    LicensePlate = "ABC-123",
                    Vin = "VIN123456",
                    CustomerId = 1,
                    Customer = new Customer
                    {
                        CustomerId = 1,
                        User = new User { FullName = "Nguyễn Văn A" }
                    },
                    VehicleModel = new VehicleModel { ModelName = "Tesla Model 3" }
                }
            };

            var reminders = new List<MaintenanceReminder>
            {
                new MaintenanceReminder
                {
                    ReminderId = 1,
                    VehicleId = 1,
                    ServiceId = 1,
                    DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                    DueMileage = 50000,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    Vehicle = vehicles.First()
                }
            };

            var service = new ServiceResponse
            {
                ServiceId = 1,
                ServiceName = "Thay dầu động cơ",
                Description = "Thay dầu và lọc dầu động cơ"
            };

            _vehicleRepositoryMock.Setup(x => x.GetAllVehiclesAsync())
                .ReturnsAsync(vehicles);
            _maintenanceReminderRepositoryMock.Setup(x => x.QueryAsync(1, 1, null, null, null))
                .ReturnsAsync(reminders);
            _serviceServiceMock.Setup(x => x.GetServiceByIdAsync(1))
                .ReturnsAsync(service);

            // Act
            var result = await _controller.GetCustomerVehicleServiceReminders(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<GetCustomerVehicleServiceRemindersResponse>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.Equal(1, response.CustomerId);
            Assert.Equal("Nguyễn Văn A", response.CustomerName);
            Assert.Equal(1, response.TotalCount);
            Assert.Single(response.Reminders);
            Assert.Equal("PENDING", response.Reminders.First().Status);
        }

        [Fact]
        [AllureDescription("Should return NotFound when customer does not exist")]
        [AllureTag("GetCustomerVehicleServiceReminders")]
        public async Task GetCustomerVehicleServiceReminders_CustomerNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new GetCustomerVehicleServiceRemindersRequest
            {
                CustomerId = 999
            };

            _vehicleRepositoryMock.Setup(x => x.GetAllVehiclesAsync())
                .ReturnsAsync(new List<Vehicle>());

            // Act
            var result = await _controller.GetCustomerVehicleServiceReminders(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;
            var successProperty = response.GetType().GetProperty("success");
            var messageProperty = response.GetType().GetProperty("message");
            Assert.False((bool)successProperty.GetValue(response));
            Assert.Equal("Không tìm thấy khách hàng với ID đã cho", (string)messageProperty.GetValue(response));
        }

        #endregion

        #region SendReminderBeforeAppointment Tests

        [Fact]
        [AllureDescription("Should send reminder before appointment successfully")]
        [AllureTag("SendReminderBeforeAppointment")]
        public async Task SendReminderBeforeAppointment_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new SendReminderBeforeAppointmentRequest
            {
                BookingId = 1,
                ReminderHoursBefore = 24,
                SendEmail = true,
                SendSms = false
            };

            var booking = new Booking
            {
                BookingId = 1,
                CustomerId = 1,
                VehicleId = 1,
                ServiceId = 1,
                CenterId = 1,
                Status = "CONFIRMED",
                Customer = new Customer
                {
                    CustomerId = 1,
                    User = new User
                    {
                        FullName = "Nguyễn Văn A",
                        Email = "test@example.com",
                        PhoneNumber = "0123456789"
                    }
                },
                Vehicle = new Vehicle
                {
                    VehicleId = 1,
                    LicensePlate = "ABC-123",
                    Vin = "VIN123456"
                },
                Service = new Service
                {
                    ServiceId = 1,
                    ServiceName = "Thay dầu động cơ"
                },
                Center = new ServiceCenter
                {
                    CenterId = 1,
                    CenterName = "EV Service Center",
                    Address = "123 Main St",
                    PhoneNumber = "0123456789"
                }
            };

            _bookingRepositoryMock.Setup(x => x.GetBookingWithDetailsByIdAsync(1))
                .ReturnsAsync(booking);

            // Act
            var result = await _controller.SendReminderBeforeAppointment(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<SendReminderBeforeAppointmentResponse>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.Equal(1, response.BookingId);
            Assert.Equal("Nguyễn Văn A", response.CustomerName);
            Assert.Equal("test@example.com", response.CustomerEmail);
            Assert.Equal("ABC-123", response.VehicleLicensePlate);
            Assert.Equal("Thay dầu động cơ", response.ServiceName);
            Assert.Equal(1, response.Summary.EmailSent);
            Assert.Equal(0, response.Summary.SmsSent);
        }

        [Fact]
        [AllureDescription("Should return NotFound when booking does not exist")]
        [AllureTag("SendReminderBeforeAppointment")]
        public async Task SendReminderBeforeAppointment_BookingNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new SendReminderBeforeAppointmentRequest
            {
                BookingId = 999
            };

            _bookingRepositoryMock.Setup(x => x.GetBookingWithDetailsByIdAsync(999))
                .ReturnsAsync((Booking)null);

            // Act
            var result = await _controller.SendReminderBeforeAppointment(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;
            var successProperty = response.GetType().GetProperty("success");
            var messageProperty = response.GetType().GetProperty("message");
            Assert.False((bool)successProperty.GetValue(response));
            Assert.Equal("Không tìm thấy lịch hẹn với ID đã cho", (string)messageProperty.GetValue(response));
        }

        [Fact]
        [AllureDescription("Should return BadRequest when booking status is not valid")]
        [AllureTag("SendReminderBeforeAppointment")]
        public async Task SendReminderBeforeAppointment_InvalidBookingStatus_ReturnsBadRequest()
        {
            // Arrange
            var request = new SendReminderBeforeAppointmentRequest
            {
                BookingId = 1
            };

            var booking = new Booking
            {
                BookingId = 1,
                Status = "CANCELLED"
            };

            _bookingRepositoryMock.Setup(x => x.GetBookingWithDetailsByIdAsync(1))
                .ReturnsAsync(booking);

            // Act
            var result = await _controller.SendReminderBeforeAppointment(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            var successProperty = response.GetType().GetProperty("success");
            var messageProperty = response.GetType().GetProperty("message");
            Assert.False((bool)successProperty.GetValue(response));
            Assert.Equal("Chỉ có thể gửi nhắc nhở cho lịch hẹn đã xác nhận hoặc đang chờ", (string)messageProperty.GetValue(response));
        }

        #endregion

        #region SendVehicleMaintenanceAlerts Tests

        [Fact]
        [AllureDescription("Should send vehicle maintenance alerts successfully")]
        [AllureTag("SendVehicleMaintenanceAlerts")]
        public async Task SendVehicleMaintenanceAlerts_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new SendVehicleMaintenanceAlertsRequest
            {
                VehicleId = 1,
                CustomerId = 1,
                UpcomingDays = 7,
                SendEmail = true,
                SendSms = false
            };

            var reminders = new List<MaintenanceReminder>
            {
                new MaintenanceReminder
                {
                    ReminderId = 1,
                    VehicleId = 1,
                    ServiceId = 1,
                    DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
                    DueMileage = 50000,
                    IsCompleted = false,
                    Vehicle = new Vehicle
                    {
                        VehicleId = 1,
                        Customer = new Customer
                        {
                            CustomerId = 1,
                            User = new User
                            {
                                FullName = "Nguyễn Văn A",
                                Email = "test@example.com",
                                PhoneNumber = "0123456789"
                            }
                        }
                    }
                }
            };

            _maintenanceReminderRepositoryMock.Setup(x => x.QueryAsync(1, 1, "PENDING", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(reminders);

            // Act
            var result = await _controller.SendVehicleMaintenanceAlerts(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<SendVehicleMaintenanceAlertsResponse>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.Equal(1, response.Summary.TotalReminders);
            Assert.Equal(1, response.Summary.SentEmails);
            Assert.Equal(0, response.Summary.SentSms);
            Assert.Equal(0, response.Summary.Failed);
            Assert.Single(response.Results);
        }

        [Fact]
        [AllureDescription("Should handle exception when sending alerts fails")]
        [AllureTag("SendVehicleMaintenanceAlerts")]
        public async Task SendVehicleMaintenanceAlerts_ExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            var request = new SendVehicleMaintenanceAlertsRequest
            {
                VehicleId = 1
            };

            _maintenanceReminderRepositoryMock.Setup(x => x.QueryAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.SendVehicleMaintenanceAlerts(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = statusCodeResult.Value;
            var successProperty = response.GetType().GetProperty("success");
            var messageProperty = response.GetType().GetProperty("message");
            Assert.False((bool)successProperty.GetValue(response));
            Assert.Equal("Lỗi khi gửi thông báo nhắc nhở", (string)messageProperty.GetValue(response));
        }

        #endregion

        #region GetUpcomingReminders Tests

        [Fact]
        [AllureDescription("Should get upcoming reminders successfully")]
        [AllureTag("GetUpcomingReminders")]
        public async Task GetUpcomingReminders_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var reminders = new List<MaintenanceReminder>
            {
                new MaintenanceReminder
                {
                    ReminderId = 1,
                    VehicleId = 1,
                    ServiceId = 1,
                    DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
                    DueMileage = 50000,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    Vehicle = new Vehicle
                    {
                        VehicleId = 1,
                        LicensePlate = "ABC-123",
                        Customer = new Customer
                        {
                            CustomerId = 1,
                            User = new User
                            {
                                FullName = "Nguyễn Văn A",
                                Email = "test@example.com",
                                PhoneNumber = "0123456789"
                            }
                        }
                    }
                }
            };

            _maintenanceReminderRepositoryMock.Setup(x => x.QueryAsync(It.IsAny<int?>(), It.IsAny<int?>(), "PENDING", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(reminders);

            // Act
            var result = await _controller.GetUpcomingReminders();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var successProperty = response.GetType().GetProperty("success");
            var messageProperty = response.GetType().GetProperty("message");
            Assert.True((bool)successProperty.GetValue(response));
            Assert.Equal("Lấy danh sách reminders sắp đến hạn thành công", (string)messageProperty.GetValue(response));
        }

        #endregion

        #region SendTestEmail Tests

        [Fact]
        [AllureDescription("Should send test email successfully")]
        [AllureTag("SendTestEmail")]
        public async Task SendTestEmail_ValidReminder_ReturnsOkResult()
        {
            // Arrange
            var reminderId = 1;
            var reminder = new MaintenanceReminder
            {
                ReminderId = 1,
                VehicleId = 1,
                ServiceId = 1,
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
                DueMileage = 50000,
                Vehicle = new Vehicle
                {
                    VehicleId = 1,
                    Customer = new Customer
                    {
                        CustomerId = 1,
                        User = new User
                        {
                            FullName = "Nguyễn Văn A",
                            Email = "test@example.com"
                        }
                    }
                }
            };

            _maintenanceReminderRepositoryMock.Setup(x => x.GetByIdAsync(reminderId))
                .ReturnsAsync(reminder);

            // Act
            var result = await _controller.SendTestEmail(reminderId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var successProperty = response.GetType().GetProperty("success");
            var messageProperty = response.GetType().GetProperty("message");
            Assert.True((bool)successProperty.GetValue(response));
            Assert.Equal("Đã gửi email test thành công", (string)messageProperty.GetValue(response));
        }

        [Fact]
        [AllureDescription("Should return NotFound when reminder does not exist")]
        [AllureTag("SendTestEmail")]
        public async Task SendTestEmail_ReminderNotFound_ReturnsNotFound()
        {
            // Arrange
            var reminderId = 999;

            _maintenanceReminderRepositoryMock.Setup(x => x.GetByIdAsync(reminderId))
                .ReturnsAsync((MaintenanceReminder)null);

            // Act
            var result = await _controller.SendTestEmail(reminderId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;
            var successProperty = response.GetType().GetProperty("success");
            var messageProperty = response.GetType().GetProperty("message");
            Assert.False((bool)successProperty.GetValue(response));
            Assert.Equal("Không tìm thấy reminder", (string)messageProperty.GetValue(response));
        }

        #endregion
    }
}
