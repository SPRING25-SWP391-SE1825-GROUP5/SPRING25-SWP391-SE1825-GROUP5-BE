using Microsoft.AspNetCore.Mvc;
using Moq;
using EVServiceCenter.Api.Controllers;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using Allure.Xunit.Attributes;
using System.Threading.Tasks;
using System;

namespace EVServiceCenter.Tests.Unit.Api.Controllers
{
    [AllureSuite("Vehicle Controller Tests")]
    public class VehicleControllerTests
    {
        private readonly Mock<IVehicleService> _vehicleServiceMock;
        private readonly VehicleController _controller;

        public VehicleControllerTests()
        {
            _vehicleServiceMock = new Mock<IVehicleService>();
            _controller = new VehicleController(_vehicleServiceMock.Object);
        }

        [AllureXunit(DisplayName = "UpdateVehicle - Should return success when valid request")]
        public async Task UpdateVehicle_WithValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var vehicleId = 1;
            var request = new UpdateVehicleRequest
            {
                Color = "Red",
                CurrentMileage = 10000,
                PurchaseDate = new DateOnly(2020, 1, 1)
            };

            var vehicleResponse = new VehicleResponse
            {
                VehicleId = vehicleId,
                Color = request.Color,
                CurrentMileage = request.CurrentMileage,
                PurchaseDate = request.PurchaseDate
            };

            _vehicleServiceMock.Setup(x => x.UpdateVehicleAsync(vehicleId, request))
                .ReturnsAsync(vehicleResponse);

            // Act
            var result = await _controller.UpdateVehicle(vehicleId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value as dynamic;
            Assert.True(response.success);
            Assert.Contains("Cập nhật xe thành công", response.message.ToString());
        }

        [AllureXunit(DisplayName = "UpdateVehicle - Should return bad request when invalid purchase date")]
        public async Task UpdateVehicle_WithInvalidPurchaseDate_ShouldReturnBadRequest()
        {
            // Arrange
            var vehicleId = 1;
            var request = new UpdateVehicleRequest
            {
                Color = "Red",
                CurrentMileage = 10000,
                PurchaseDate = new DateOnly(1800, 1, 1) // Invalid date - too old
            };

            // Act
            var result = await _controller.UpdateVehicle(vehicleId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value as dynamic;
            Assert.False(response.success);
            Assert.Contains("Dữ liệu không hợp lệ", response.message.ToString());
        }

        [AllureXunit(DisplayName = "UpdateVehicle - Should return bad request when future purchase date")]
        public async Task UpdateVehicle_WithFuturePurchaseDate_ShouldReturnBadRequest()
        {
            // Arrange
            var vehicleId = 1;
            var request = new UpdateVehicleRequest
            {
                Color = "Red",
                CurrentMileage = 10000,
                PurchaseDate = new DateOnly(2200, 1, 1) // Invalid date - too far in future
            };

            // Act
            var result = await _controller.UpdateVehicle(vehicleId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value as dynamic;
            Assert.False(response.success);
            Assert.Contains("Dữ liệu không hợp lệ", response.message.ToString());
        }

        [AllureXunit(DisplayName = "UpdateVehicle - Should return bad request when invalid vehicle ID")]
        public async Task UpdateVehicle_WithInvalidVehicleId_ShouldReturnBadRequest()
        {
            // Arrange
            var vehicleId = 0; // Invalid ID
            var request = new UpdateVehicleRequest
            {
                Color = "Red",
                CurrentMileage = 10000,
                PurchaseDate = new DateOnly(2020, 1, 1)
            };

            // Act
            var result = await _controller.UpdateVehicle(vehicleId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value as dynamic;
            Assert.False(response.success);
            Assert.Contains("ID xe không hợp lệ", response.message.ToString());
        }

        [AllureXunit(DisplayName = "UpdateVehicle - Should return bad request when vehicle not found")]
        public async Task UpdateVehicle_WhenVehicleNotFound_ShouldReturnBadRequest()
        {
            // Arrange
            var vehicleId = 999;
            var request = new UpdateVehicleRequest
            {
                Color = "Red",
                CurrentMileage = 10000,
                PurchaseDate = new DateOnly(2020, 1, 1)
            };

            _vehicleServiceMock.Setup(x => x.UpdateVehicleAsync(vehicleId, request))
                .ThrowsAsync(new ArgumentException("Không tìm thấy xe với ID này."));

            // Act
            var result = await _controller.UpdateVehicle(vehicleId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value as dynamic;
            Assert.False(response.success);
            Assert.Contains("Không tìm thấy xe", response.message.ToString());
        }

        [AllureXunit(DisplayName = "UpdateVehicle - Should return bad request when invalid color")]
        public async Task UpdateVehicle_WithInvalidColor_ShouldReturnBadRequest()
        {
            // Arrange
            var vehicleId = 1;
            var request = new UpdateVehicleRequest
            {
                Color = "", // Invalid - too short
                CurrentMileage = 10000,
                PurchaseDate = new DateOnly(2020, 1, 1)
            };

            // Act
            var result = await _controller.UpdateVehicle(vehicleId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value as dynamic;
            Assert.False(response.success);
            Assert.Contains("Dữ liệu không hợp lệ", response.message.ToString());
        }

        [AllureXunit(DisplayName = "UpdateVehicle - Should return bad request when negative mileage")]
        public async Task UpdateVehicle_WithNegativeMileage_ShouldReturnBadRequest()
        {
            // Arrange
            var vehicleId = 1;
            var request = new UpdateVehicleRequest
            {
                Color = "Red",
                CurrentMileage = -1000, // Invalid - negative
                PurchaseDate = new DateOnly(2020, 1, 1)
            };

            // Act
            var result = await _controller.UpdateVehicle(vehicleId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value as dynamic;
            Assert.False(response.success);
            Assert.Contains("Dữ liệu không hợp lệ", response.message.ToString());
        }

        [AllureXunit(DisplayName = "CreateVehicle - Should return success when valid request")]
        public async Task CreateVehicle_WithValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var request = new CreateVehicleRequest
            {
                CustomerId = 1,
                Vin = "12345678901234567",
                LicensePlate = "29-T8 2843",
                Color = "Red",
                CurrentMileage = 10000,
                PurchaseDate = new DateOnly(2020, 1, 1)
            };

            var vehicleResponse = new VehicleResponse
            {
                VehicleId = 1,
                Color = request.Color,
                CurrentMileage = request.CurrentMileage,
                PurchaseDate = request.PurchaseDate
            };

            _vehicleServiceMock.Setup(x => x.CreateVehicleAsync(request))
                .ReturnsAsync(vehicleResponse);

            // Act
            var result = await _controller.CreateVehicle(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var response = createdResult.Value as dynamic;
            Assert.True(response.success);
            Assert.Contains("Tạo xe thành công", response.message.ToString());
        }

        [AllureXunit(DisplayName = "CreateVehicle - Should return bad request when invalid purchase date")]
        public async Task CreateVehicle_WithInvalidPurchaseDate_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new CreateVehicleRequest
            {
                CustomerId = 1,
                Vin = "12345678901234567",
                LicensePlate = "29-T8 2843",
                Color = "Red",
                CurrentMileage = 10000,
                PurchaseDate = new DateOnly(1800, 1, 1) // Invalid date - too old
            };

            // Act
            var result = await _controller.CreateVehicle(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value as dynamic;
            Assert.False(response.success);
            Assert.Contains("Dữ liệu không hợp lệ", response.message.ToString());
        }

        [AllureXunit(DisplayName = "GetVehicleById - Should return success when valid ID")]
        public async Task GetVehicleById_WithValidId_ShouldReturnSuccess()
        {
            // Arrange
            var vehicleId = 1;
            var vehicleResponse = new VehicleResponse
            {
                VehicleId = vehicleId,
                Color = "Red",
                CurrentMileage = 10000,
                PurchaseDate = new DateOnly(2020, 1, 1)
            };

            _vehicleServiceMock.Setup(x => x.GetVehicleByIdAsync(vehicleId))
                .ReturnsAsync(vehicleResponse);

            // Act
            var result = await _controller.GetVehicleById(vehicleId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value as dynamic;
            Assert.True(response.success);
            Assert.Contains("Lấy thông tin xe thành công", response.message.ToString());
        }
    }
}
