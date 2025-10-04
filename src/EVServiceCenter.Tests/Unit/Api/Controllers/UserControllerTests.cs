using Microsoft.AspNetCore.Mvc;
using Moq;
using EVServiceCenter.Api.Controllers;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Allure.Xunit.Attributes;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace EVServiceCenter.Tests.Unit.Api.Controllers
{
    [AllureSuite("User Controller Tests")]
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IAccountService> _accountServiceMock;
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _accountServiceMock = new Mock<IAccountService>();
            _authServiceMock = new Mock<IAuthService>();
            _emailServiceMock = new Mock<IEmailService>();

            _controller = new UserController(
                _userServiceMock.Object,
                _accountServiceMock.Object,
                _authServiceMock.Object,
                _emailServiceMock.Object
            );

            // Setup default user claims for authorization
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "ADMIN")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [AllureXunit(DisplayName = "AssignUserRole - Should return success when valid request")]
        public async Task AssignUserRole_WithValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var request = new AssignUserRoleRequest
            {
                UserId = 2,
                Role = "STAFF"
            };

            _userServiceMock.Setup(x => x.AssignUserRoleAsync(request.UserId, request.Role))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.AssignUserRole(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value as dynamic;
            Assert.True(response.success);
            Assert.Contains("STAFF", response.message.ToString());
        }

        [AllureXunit(DisplayName = "AssignUserRole - Should return bad request when invalid role")]
        public async Task AssignUserRole_WithInvalidRole_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new AssignUserRoleRequest
            {
                UserId = 2,
                Role = "INVALID_ROLE"
            };

            // Act
            var result = await _controller.AssignUserRole(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value as dynamic;
            Assert.False(response.success);
            Assert.Contains("Dữ liệu không hợp lệ", response.message.ToString());
        }

        [AllureXunit(DisplayName = "AssignUserRole - Should return bad request when trying to change own role")]
        public async Task AssignUserRole_WhenChangingOwnRole_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new AssignUserRoleRequest
            {
                UserId = 1, // Same as current user ID
                Role = "STAFF"
            };

            // Act
            var result = await _controller.AssignUserRole(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value as dynamic;
            Assert.False(response.success);
            Assert.Contains("chính mình", response.message.ToString());
        }

        [AllureXunit(DisplayName = "AssignUserRole - Should return bad request when user not found")]
        public async Task AssignUserRole_WhenUserNotFound_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new AssignUserRoleRequest
            {
                UserId = 999,
                Role = "STAFF"
            };

            _userServiceMock.Setup(x => x.AssignUserRoleAsync(request.UserId, request.Role))
                .ThrowsAsync(new ArgumentException("Không tìm thấy người dùng với ID này."));

            // Act
            var result = await _controller.AssignUserRole(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value as dynamic;
            Assert.False(response.success);
            Assert.Contains("Không tìm thấy người dùng", response.message.ToString());
        }

        [AllureXunit(DisplayName = "CreateUser - Should return success when valid request")]
        public async Task CreateUser_WithValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                FullName = "Test User",
                Email = "test@example.com",
                Password = "Password123!",
                PhoneNumber = "0123456789",
                DateOfBirth = new DateOnly(1990, 1, 1),
                Gender = "MALE",
                Address = "Test Address"
            };

            var userResponse = new UserResponse
            {
                UserId = 1,
                FullName = request.FullName,
                Email = request.Email,
                Role = "CUSTOMER",
                EmailVerified = false
            };

            _userServiceMock.Setup(x => x.CreateUserAsync(It.IsAny<CreateUserRequest>()))
                .ReturnsAsync(userResponse);

            // Act
            var result = await _controller.CreateUser(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var response = createdResult.Value as dynamic;
            Assert.True(response.success);
            Assert.Contains("Tạo người dùng thành công", response.message.ToString());
        }

        [AllureXunit(DisplayName = "CreateUser - Should return bad request when invalid email")]
        public async Task CreateUser_WithInvalidEmail_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                FullName = "Test User",
                Email = "invalid-email",
                Password = "Password123!",
                PhoneNumber = "0123456789",
                DateOfBirth = new DateOnly(1990, 1, 1),
                Gender = "MALE",
                Address = "Test Address"
            };

            // Act
            var result = await _controller.CreateUser(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value as dynamic;
            Assert.False(response.success);
            Assert.Contains("Dữ liệu không hợp lệ", response.message.ToString());
        }

        [AllureXunit(DisplayName = "ActivateUser - Should return success when valid ID")]
        public async Task ActivateUser_WithValidId_ShouldReturnSuccess()
        {
            // Arrange
            var userId = 1;
            _userServiceMock.Setup(x => x.ActivateUserAsync(userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ActivateUser(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value as dynamic;
            Assert.True(response.success);
            Assert.Contains("Kích hoạt người dùng thành công", response.message.ToString());
        }

        [AllureXunit(DisplayName = "DeactivateUser - Should return bad request when invalid ID")]
        public async Task DeactivateUser_WithInvalidId_ShouldReturnBadRequest()
        {
            // Arrange
            var userId = 0;

            // Act
            var result = await _controller.DeactivateUser(userId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value as dynamic;
            Assert.False(response.success);
            Assert.Contains("ID người dùng không hợp lệ", response.message.ToString());
        }
    }
}
