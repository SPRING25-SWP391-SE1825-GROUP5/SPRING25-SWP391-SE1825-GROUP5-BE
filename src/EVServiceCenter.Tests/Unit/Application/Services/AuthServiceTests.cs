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

    #region Login Tests

    [Fact]
    [AllureFeature("User Authentication")]
    [AllureStory("Login Success - Email")]
    public async Task LoginAsync_WithValidEmailAndPassword_ShouldReturnTokens()
    {
        var user = new User
        {
            UserId = 10,
            Email = "user@gmail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            FullName = "User",
            Role = "CUSTOMER",
            IsActive = true,
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _accountServiceMock.Setup(x => x.GetAccountByEmailAsync("user@gmail.com"))
            .ReturnsAsync(user);
        _loginLockoutServiceMock.Setup(x => x.IsAccountLockedAsync("user@gmail.com"))
            .ReturnsAsync(false);
        _jwtServiceMock.Setup(x => x.GenerateAccessToken(user)).Returns("access");
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh");
        _jwtServiceMock.Setup(x => x.GetTokenExpiration()).Returns(DateTime.UtcNow.AddHours(1));
        _jwtServiceMock.Setup(x => x.GetTokenExpirationInSeconds()).Returns(3600);
        _authRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var result = await _authService.LoginAsync(new LoginRequest { EmailOrPhone = "user@gmail.com", Password = "Password123!" });

        Assert.Equal("access", result.AccessToken);
        Assert.Equal("refresh", result.RefreshToken);
        _loginLockoutServiceMock.Verify(x => x.ClearFailedAttemptsAsync("user@gmail.com"), Times.Once);
        _authRepositoryMock.Verify(x => x.UpdateUserAsync(It.Is<User>(u => u.RefreshToken != null)), Times.Once);
    }

    [Fact]
    [AllureFeature("User Authentication")]
    [AllureStory("Login Failure - Invalid Identifier")]
    public async Task LoginAsync_WithInvalidIdentifier_ShouldThrow()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _authService.LoginAsync(new LoginRequest { EmailOrPhone = "not-email-or-phone", Password = "x" }));
        Assert.Contains("Vui lòng nhập email (@gmail.com) hoặc số điện thoại hợp lệ", ex.Message);
    }

    [Fact]
    [AllureFeature("User Authentication")]
    [AllureStory("Login Failure - Locked Out")]
    public async Task LoginAsync_WhenLockedOut_ShouldThrow()
    {
        _loginLockoutServiceMock.Setup(x => x.IsAccountLockedAsync("user@gmail.com"))
            .ReturnsAsync(true);

        // So that service treats as email path
        _accountServiceMock.Setup(x => x.GetAccountByEmailAsync("user@gmail.com")).ReturnsAsync((User)null);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _authService.LoginAsync(new LoginRequest { EmailOrPhone = "user@gmail.com", Password = "x" }));
        Assert.Contains("Tài khoản đã bị khóa", ex.Message);
    }

    [Fact]
    [AllureFeature("User Authentication")]
    [AllureStory("Login Failure - Wrong Password Records Attempts")]
    public async Task LoginAsync_WrongPassword_ShouldRecordFailedAttempt()
    {
        var user = new User
        {
            UserId = 11,
            Email = "user@gmail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectP@ss1"),
            IsActive = true,
            EmailVerified = true,
        };
        _accountServiceMock.Setup(x => x.GetAccountByEmailAsync("user@gmail.com")).ReturnsAsync(user);
        _loginLockoutServiceMock.Setup(x => x.IsAccountLockedAsync("user@gmail.com")).ReturnsAsync(false);
        _loginLockoutServiceMock.Setup(x => x.RecordFailedAttemptAsync("user@gmail.com")).Returns(Task.CompletedTask);
        _loginLockoutServiceMock.Setup(x => x.GetRemainingAttemptsAsync("user@gmail.com")).ReturnsAsync(3);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _authService.LoginAsync(new LoginRequest { EmailOrPhone = "user@gmail.com", Password = "WrongP@ss1" }));
        Assert.Contains("Còn 3 lần thử", ex.Message);
        _loginLockoutServiceMock.Verify(x => x.RecordFailedAttemptAsync("user@gmail.com"), Times.Once);
    }

    #endregion

    #region Logout / Verify Email / Resend / Reset Password

    [Fact]
    [AllureFeature("User Authentication")]
    [AllureStory("Logout")]
    public async Task LogoutAsync_Should_Clear_RefreshToken()
    {
        var user = new User { UserId = 5, Email = "a@gmail.com", RefreshToken = new byte[] { 1, 2 } };
        _authRepositoryMock.Setup(x => x.GetUserByIdAsync(5)).ReturnsAsync(user);
        _authRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var msg = await _authService.LogoutAsync(5);
        Assert.Contains("Đăng xuất thành công", msg);
        _authRepositoryMock.Verify(x => x.UpdateUserAsync(It.Is<User>(u => u.RefreshToken == null)), Times.Once);
    }

    [Fact]
    [AllureFeature("User Authentication")]
    [AllureStory("Verify Email")]
    public async Task VerifyEmailAsync_WithValidOtp_Should_Succeed()
    {
        var user = new User { UserId = 99, Email = "u@gmail.com", FullName = "U", EmailVerified = false };
        _authRepositoryMock.Setup(x => x.GetUserByIdAsync(99)).ReturnsAsync(user);
        _otpServiceMock.Setup(x => x.VerifyOtpAsync(99, "123456", "EMAIL_VERIFICATION")).ReturnsAsync(true);
        _authRepositoryMock.Setup(x => x.UpdateEmailVerifiedStatusAsync(99, true)).Returns(Task.CompletedTask);
        _authRepositoryMock.Setup(x => x.UpdateUserActiveStatusAsync(99, true)).Returns(Task.CompletedTask);
        _emailServiceMock.Setup(x => x.SendWelcomeEmailAsync(user.Email, user.FullName)).Returns(Task.CompletedTask);

        var msg = await _authService.VerifyEmailAsync(99, "123456");
        Assert.Contains("Xác thực email thành công", msg);
    }

    [Fact]
    [AllureFeature("User Authentication")]
    [AllureStory("Resend Verification")]
    public async Task ResendVerificationEmailAsync_WhenAllowed_Should_Send()
    {
        var user = new User { UserId = 7, Email = "u@gmail.com", FullName = "U", EmailVerified = false };
        _accountServiceMock.Setup(x => x.GetAccountByEmailAsync("u@gmail.com")).ReturnsAsync(user);
        _otpServiceMock.Setup(x => x.CanCreateNewOtpAsync(7, "EMAIL_VERIFICATION")).ReturnsAsync(true);
        _otpServiceMock.Setup(x => x.CreateOtpAsync(7, user.Email, "EMAIL_VERIFICATION")).ReturnsAsync("999999");
        _emailServiceMock.Setup(x => x.SendVerificationEmailAsync(user.Email, user.FullName, "999999")).Returns(Task.CompletedTask);

        var msg = await _authService.ResendVerificationEmailAsync("u@gmail.com");
        Assert.Contains("đã được gửi", msg);
    }

    [Fact]
    [AllureFeature("User Authentication")]
    [AllureStory("Request Reset Password")]
    public async Task RequestResetPasswordAsync_WithValidEmail_Should_SendOtp()
    {
        var user = new User { UserId = 15, Email = "u@gmail.com", FullName = "U", IsActive = true };
        _accountServiceMock.Setup(x => x.GetAccountByEmailAsync("u@gmail.com")).ReturnsAsync(user);
        _otpServiceMock.Setup(x => x.CanCreateNewOtpAsync(15, "PASSWORD_RESET")).ReturnsAsync(true);
        _otpServiceMock.Setup(x => x.CreateOtpAsync(15, user.Email, "PASSWORD_RESET")).ReturnsAsync("123456");
        _emailServiceMock.Setup(x => x.SendResetPasswordEmailAsync(user.Email, user.FullName, "123456")).Returns(Task.CompletedTask);

        var msg = await _authService.RequestResetPasswordAsync("u@gmail.com");
        Assert.Contains("Mã xác thực đặt lại mật khẩu", msg);
    }

    [Fact]
    [AllureFeature("User Authentication")]
    [AllureStory("Confirm Reset Password")]
    public async Task ConfirmResetPasswordAsync_WithValidData_Should_UpdatePassword()
    {
        var user = new User { UserId = 20, Email = "u@gmail.com", FullName = "U", IsActive = true, PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldP@ss1") };
        _accountServiceMock.Setup(x => x.GetAccountByEmailAsync("u@gmail.com")).ReturnsAsync(user);
        _otpServiceMock.Setup(x => x.VerifyOtpAsync(20, "654321", "PASSWORD_RESET")).ReturnsAsync(true);
        _authRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var msg = await _authService.ConfirmResetPasswordAsync(new ConfirmResetPasswordRequest
        {
            Email = "u@gmail.com",
            OtpCode = "654321",
            NewPassword = "NewP@ssword1",
            ConfirmPassword = "NewP@ssword1"
        });

        Assert.Contains("Đặt lại mật khẩu thành công", msg);
        _authRepositoryMock.Verify(x => x.UpdateUserAsync(It.Is<User>(u => BCrypt.Net.BCrypt.Verify("NewP@ssword1", u.PasswordHash))), Times.Once);
    }

    #endregion

    #region Profile / Avatar / Change Password / Verify OTP

    [Fact]
    public async Task GetUserProfileAsync_Should_Return_Profile()
    {
        var user = new User { UserId = 2, Email = "a@gmail.com", FullName = "A", IsActive = true };
        _authRepositoryMock.Setup(x => x.GetUserByIdAsync(2)).ReturnsAsync(user);
        var profile = await _authService.GetUserProfileAsync(2);
        Assert.Equal(2, profile.UserId);
        Assert.Equal("a@gmail.com", profile.Email);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_Should_Update_When_Valid()
    {
        var user = new User { UserId = 3, Email = "a@gmail.com", FullName = "A", IsActive = true, Gender = "MALE", DateOfBirth = new DateOnly(1990,1,1) };
        _authRepositoryMock.Setup(x => x.GetUserByIdAsync(3)).ReturnsAsync(user);
        _authRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var msg = await _authService.UpdateUserProfileAsync(3, new UpdateProfileRequest
        {
            FullName = "New Name",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = "MALE",
            Address = "Addr"
        });
        Assert.Contains("Cập nhật thông tin cá nhân thành công", msg);
    }

    [Fact]
    public async Task UpdateUserAvatarAsync_Should_Update()
    {
        var user = new User { UserId = 4, Email = "a@gmail.com", IsActive = true };
        _authRepositoryMock.Setup(x => x.GetUserByIdAsync(4)).ReturnsAsync(user);
        _authRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var msg = await _authService.UpdateUserAvatarAsync(4, "http://img");
        Assert.Contains("Cập nhật avatar thành công", msg);
    }

    [Fact]
    public async Task ChangePasswordAsync_Should_Update_When_Valid()
    {
        var currentHash = BCrypt.Net.BCrypt.HashPassword("CurrentP@ss1");
        var user = new User { UserId = 6, Email = "a@gmail.com", IsActive = true, PasswordHash = currentHash };
        _authRepositoryMock.Setup(x => x.GetUserByIdAsync(6)).ReturnsAsync(user);
        _authRepositoryMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var msg = await _authService.ChangePasswordAsync(6, new ChangePasswordRequest
        {
            CurrentPassword = "CurrentP@ss1",
            NewPassword = "NewP@ss1!",
            ConfirmNewPassword = "NewP@ss1!"
        });
        Assert.Contains("Đổi mật khẩu thành công", msg);
    }

    [Fact]
    public async Task VerifyOtpAsync_Should_Succeed_When_Matching()
    {
        var user = new User { UserId = 8, Email = "a@gmail.com", EmailVerified = false };
        var otp = new Otpcode { UserId = 8, Otpcode1 = "111111", ExpiresAt = DateTime.UtcNow.AddMinutes(5), IsUsed = false };
        _accountRepositoryMock.Setup(x => x.GetAccountByEmailAsync("a@gmail.com")).ReturnsAsync(user);
        _otpRepositoryMock.Setup(x => x.GetLastOtpCodeAsync(8, "EMAIL_VERIFICATION")).ReturnsAsync(otp);
        _otpRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Otpcode>())).Returns(Task.CompletedTask);
        _accountRepositoryMock.Setup(x => x.UpdateAccountAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var ok = await _authService.VerifyOtpAsync("a@gmail.com", "111111");
        Assert.True(ok);
        _otpRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Otpcode>(o => o.IsUsed)), Times.Once);
    }

    #endregion
}