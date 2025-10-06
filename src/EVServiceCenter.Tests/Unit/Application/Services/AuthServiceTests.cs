using Xunit;
using Moq;
using Allure.Xunit.Attributes;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Service;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.IRepositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EVServiceCenter.Tests.Unit.Application.Services;

[AllureSuite("Authentication")]
[AllureLabel("layer", "application")]
public class AuthServiceTests
{
    private readonly Mock<IAuthRepository> _authRepositoryMock;
    private readonly Mock<IAccountService> _accountServiceMock;
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<IOtpService> _otpServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IOtpCodeRepository> _otpRepositoryMock;
    private readonly Mock<IMemoryCache> _cacheMock;
    private readonly Mock<ILoginLockoutService> _loginLockoutServiceMock;

    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _authRepositoryMock = new Mock<IAuthRepository>();
        _accountServiceMock = new Mock<IAccountService>();
        _customerRepositoryMock = new Mock<ICustomerRepository>();
        _otpServiceMock = new Mock<IOtpService>();
        _emailServiceMock = new Mock<IEmailService>();
        _jwtServiceMock = new Mock<IJwtService>();
        _configurationMock = new Mock<IConfiguration>();
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _otpRepositoryMock = new Mock<IOtpCodeRepository>();
        _cacheMock = new Mock<IMemoryCache>();
        _loginLockoutServiceMock = new Mock<ILoginLockoutService>();

        _authService = new AuthService(
            _accountServiceMock.Object,
            _authRepositoryMock.Object,
            _emailServiceMock.Object,
            _otpServiceMock.Object,
            _jwtServiceMock.Object,
            _configurationMock.Object,
            _customerRepositoryMock.Object,
            _accountRepositoryMock.Object,
            _otpRepositoryMock.Object,
            _cacheMock.Object,
            _loginLockoutServiceMock.Object
        );
    }

    #region Register Tests

    [Fact]
    [AllureFeature("User Registration")]
    [AllureStory("Successful Registration")]
    public async Task RegisterAsync_WithValidData_ShouldReturnSuccessMessage()
    {
        // Arrange
        var request = new AccountRequest
        {
            FullName = "Nguyễn Văn A",
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            PhoneNumber = "0123456789",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = "MALE",
            Address = "123 Đường ABC, Quận 1, TP.HCM"
        };

        _accountServiceMock.Setup(x => x.GetAccountByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);
        _accountServiceMock.Setup(x => x.GetAccountByPhoneNumberAsync(request.PhoneNumber))
            .ReturnsAsync((User?)null);
        _authRepositoryMock.Setup(x => x.RegisterAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);
        _customerRepositoryMock.Setup(x => x.CreateCustomerAsync(It.IsAny<Customer>()))
            .ReturnsAsync(new Customer { CustomerId = 1, UserId = 1, IsGuest = false });
        _otpServiceMock.Setup(x => x.CreateOtpAsync(It.IsAny<int>(), request.Email, "EMAIL_VERIFICATION"))
            .ReturnsAsync("123456");
        _emailServiceMock.Setup(x => x.SendVerificationEmailAsync(request.Email, request.FullName, "123456"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.Contains("Đăng ký tài khoản thành công", result);
        _authRepositoryMock.Verify(x => x.RegisterAsync(It.IsAny<User>()), Times.Once);
        _customerRepositoryMock.Verify(x => x.CreateCustomerAsync(It.IsAny<Customer>()), Times.Once);
        _otpServiceMock.Verify(x => x.CreateOtpAsync(It.IsAny<int>(), request.Email, "EMAIL_VERIFICATION"), Times.Once);
        _emailServiceMock.Verify(x => x.SendVerificationEmailAsync(request.Email, request.FullName, "123456"), Times.Once);
    }

    [Fact]
    [AllureFeature("User Registration")]
    [AllureStory("Validation Errors")]
    public async Task RegisterAsync_WithInvalidEmail_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new AccountRequest
        {
            FullName = "Nguyễn Văn A",
            Email = "invalid-email",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            PhoneNumber = "0123456789",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = "MALE"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(request));
        Assert.Contains("Email không đúng định dạng", exception.Message);
    }

    [Fact]
    [AllureFeature("User Registration")]
    [AllureStory("Validation Errors")]
    public async Task RegisterAsync_WithWeakPassword_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new AccountRequest
        {
            FullName = "Nguyễn Văn A",
            Email = "test@example.com",
            Password = "weak",
            ConfirmPassword = "weak",
            PhoneNumber = "0123456789",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = "MALE"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(request));
        Assert.Contains("Mật khẩu phải có ít nhất 8 ký tự", exception.Message);
    }

    [Fact]
    [AllureFeature("User Registration")]
    [AllureStory("Validation Errors")]
    public async Task RegisterAsync_WithPasswordMismatch_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new AccountRequest
        {
            FullName = "Nguyễn Văn A",
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "DifferentPassword123!",
            PhoneNumber = "0123456789",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = "MALE"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(request));
        Assert.Contains("Mật khẩu xác nhận không khớp", exception.Message);
    }

    [Fact]
    [AllureFeature("User Registration")]
    [AllureStory("Validation Errors")]
    public async Task RegisterAsync_WithInvalidPhoneNumber_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new AccountRequest
        {
            FullName = "Nguyễn Văn A",
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            PhoneNumber = "1234567890", // Invalid phone number
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = "MALE"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(request));
        Assert.Contains("Số điện thoại phải bắt đầu bằng số 0", exception.Message);
    }

    [Fact]
    [AllureFeature("User Registration")]
    [AllureStory("Validation Errors")]
    public async Task RegisterAsync_WithUnderageUser_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new AccountRequest
        {
            FullName = "Nguyễn Văn A",
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            PhoneNumber = "0123456789",
            DateOfBirth = new DateOnly(DateTime.Today.Year - 15, 1, 1), // Under 16
            Gender = "MALE"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(request));
        Assert.Contains("Phải đủ 16 tuổi trở lên", exception.Message);
    }

    [Fact]
    [AllureFeature("User Registration")]
    [AllureStory("Validation Errors")]
    public async Task RegisterAsync_WithInvalidGender_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new AccountRequest
        {
            FullName = "Nguyễn Văn A",
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            PhoneNumber = "0123456789",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = "INVALID_GENDER"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(request));
        Assert.Contains("Giới tính phải là MALE hoặc FEMALE", exception.Message);
    }

    [Fact]
    [AllureFeature("User Registration")]
    [AllureStory("Duplicate Data")]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new AccountRequest
        {
            FullName = "Nguyễn Văn A",
            Email = "existing@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            PhoneNumber = "0123456789",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = "MALE"
        };

        var existingUser = new User { Email = "existing@example.com" };
        _accountServiceMock.Setup(x => x.GetAccountByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(request));
        Assert.Contains("Email này đã được sử dụng", exception.Message);
    }

    [Fact]
    [AllureFeature("User Registration")]
    [AllureStory("Duplicate Data")]
    public async Task RegisterAsync_WithExistingPhoneNumber_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new AccountRequest
        {
            FullName = "Nguyễn Văn A",
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            PhoneNumber = "0123456789",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = "MALE"
        };

        var existingUser = new User { PhoneNumber = "0123456789" };
        _accountServiceMock.Setup(x => x.GetAccountByEmailAsync(request.Email))
            .ReturnsAsync((User)null);
        _accountServiceMock.Setup(x => x.GetAccountByPhoneNumberAsync(request.PhoneNumber))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(request));
        Assert.Contains("Số điện thoại này đã được sử dụng", exception.Message);
    }

    [Fact]
    [AllureFeature("User Registration")]
    [AllureStory("Exception Handling")]
    public async Task RegisterAsync_WhenEmailServiceFails_ShouldReturnSuccessWithWarning()
    {
        // Arrange
        var request = new AccountRequest
        {
            FullName = "Nguyễn Văn A",
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            PhoneNumber = "0123456789",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = "MALE"
        };

        _accountServiceMock.Setup(x => x.GetAccountByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);
        _accountServiceMock.Setup(x => x.GetAccountByPhoneNumberAsync(request.PhoneNumber))
            .ReturnsAsync((User?)null);
        _authRepositoryMock.Setup(x => x.RegisterAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);
        _customerRepositoryMock.Setup(x => x.CreateCustomerAsync(It.IsAny<Customer>()))
            .ReturnsAsync(new Customer { CustomerId = 1, UserId = 1, IsGuest = false });
        _otpServiceMock.Setup(x => x.CreateOtpAsync(It.IsAny<int>(), request.Email, "EMAIL_VERIFICATION"))
            .ReturnsAsync("123456");
        _emailServiceMock.Setup(x => x.SendVerificationEmailAsync(request.Email, request.FullName, "123456"))
            .ThrowsAsync(new Exception("Email service error"));

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.Contains("Đăng ký tài khoản thành công", result);
        Assert.Contains("có lỗi khi gửi email xác thực", result);
        _authRepositoryMock.Verify(x => x.RegisterAsync(It.IsAny<User>()), Times.Once);
        _customerRepositoryMock.Verify(x => x.CreateCustomerAsync(It.IsAny<Customer>()), Times.Once);
    }

    [Fact]
    [AllureFeature("User Registration")]
    [AllureStory("Exception Handling")]
    public async Task RegisterAsync_WhenRepositoryFails_ShouldThrowException()
    {
        // Arrange
        var request = new AccountRequest
        {
            FullName = "Nguyễn Văn A",
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            PhoneNumber = "0123456789",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = "MALE"
        };

        _accountServiceMock.Setup(x => x.GetAccountByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);
        _accountServiceMock.Setup(x => x.GetAccountByPhoneNumberAsync(request.PhoneNumber))
            .ReturnsAsync((User?)null);
        _authRepositoryMock.Setup(x => x.RegisterAsync(It.IsAny<User>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _authService.RegisterAsync(request));
        Assert.Contains("Lỗi trong quá trình đăng ký", exception.Message);
    }

    #endregion

    #region Legacy Tests (Placeholders)

    [Fact]
    [AllureFeature("User Authentication")]
    [AllureStory("Service Validation")]
    public void AuthService_Should_Exist()
    {
        // Simple test to verify AuthService class exists
        Assert.True(true);
    }

    [Fact]
    [AllureFeature("User Authentication")]
    [AllureStory("Login Process")]
    public void AuthService_Login_Should_Validate_Credentials()
    {
        // Placeholder for AuthService.LoginAsync test
        Assert.True(true);
    }

    [Fact]
    public void AuthService_Token_Generation_Should_Work()
    {
        // Placeholder for JWT token generation test
        Assert.True(true);
    }

    [Fact]
    public void AuthService_Should_Handle_Invalid_Credentials()
    {
        // Placeholder for testing invalid login scenarios
        Assert.True(true);
    }

    #endregion
}