using Microsoft.Extensions.Configuration;
using Moq;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.AuthenDTOs;
using MovieTheater.Domain.DTOs.EmailDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System.Linq.Expressions;

namespace MovieTheater.UnitTest.Services;

public class AuthServiceTests
{
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILoggerService> _mockLoggerService;
    private readonly AuthService _authService;
    private readonly Mock<IGenericRepository<User>> _mockUserRepository;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public AuthServiceTests()
    {
        _mockEmailService = new Mock<IEmailService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLoggerService = new Mock<ILoggerService>();
        _mockUserRepository = new Mock<IGenericRepository<User>>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Setup JWT configuration
        // Setup JWT configuration
        var configSection = new Mock<IConfigurationSection>();
        configSection.Setup(x => x.Value).Returns("this_is_a_very_secure_key_for_testing_purposes_only_12345678901234567890");
        _mockConfiguration.Setup(x => x.GetSection("JWT:SecretKey")).Returns(configSection.Object);
        _mockConfiguration.Setup(x => x["JWT:SecretKey"]).Returns("this_is_a_very_secure_key_for_testing_purposes_only_12345678901234567890");

        // Setup UnitOfWork to return the user repository
        _mockUnitOfWork.Setup(uow => uow.Users).Returns(_mockUserRepository.Object);
        
         _authService = new AuthService(_mockUnitOfWork.Object, _mockEmailService.Object, _mockLoggerService.Object);

        // Add this line to set up the OtpStorages repository
        var mockOtpStorageRepository = new Mock<IGenericRepository<OtpStorage>>();
        _mockUnitOfWork.Setup(uow => uow.OtpStorages).Returns(mockOtpStorageRepository.Object);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsLoginResponseDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var password = "Password123!";

        var hashedPassword = new PasswordHasher().HashPassword(password);
        var user = new User
        {
            Id = userId,
            Email = email,
            Password = hashedPassword,
            UserStatus = UserStatus.Active,
            Role = RoleType.Member,
            IsEmailVerified = true
        };

        var loginDto = new LoginRequestDto
        {
            Email = email,
            Password = password
        };

        _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(loginDto, _mockConfiguration.Object);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        
        // Verify user was updated with refresh token
        _mockUserRepository.Verify(repo => repo.Update(It.Is<User>(u => 
            u.Id == userId && 
            u.RefreshToken != null && 
            u.RefreshTokenExpiryTime != null)), 
            Times.Once);
            
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        _mockLoggerService.Verify(logger => logger.Success(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ThrowsException()
    {
        // Arrange
        var loginDto = new LoginRequestDto
        {
            Email = "nonexistent@example.com",
            Password = "password"
        };

        _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _authService.LoginAsync(loginDto, _mockConfiguration.Object));
            
        Assert.Contains("Account does not exist", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithIncorrectPassword_ThrowsException()
    {
        // Arrange
        var email = "test@example.com";
        var correctPassword = "CorrectPassword123!";
        var wrongPassword = "WrongPassword123!";
        
        var hashedPassword = new PasswordHasher().HashPassword(correctPassword);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Password = hashedPassword,
            UserStatus = UserStatus.Active,
            Role = RoleType.Member,
            IsEmailVerified = true
        };

        var loginDto = new LoginRequestDto
        {
            Email = email,
            Password = wrongPassword
        };

        _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _authService.LoginAsync(loginDto, _mockConfiguration.Object));
            
        Assert.Contains("Password is incorrect", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithBannedAccount_ThrowsException()
    {
        // Arrange
        var email = "banned@example.com";
        var password = "Password123!";
        
        var hashedPassword = new PasswordHasher().HashPassword(password);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Password = hashedPassword,
            UserStatus = UserStatus.Banned,
            Role = RoleType.Member,
            IsEmailVerified = true
        };

        var loginDto = new LoginRequestDto
        {
            Email = email,
            Password = password
        };

        _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _authService.LoginAsync(loginDto, _mockConfiguration.Object));
            
        Assert.Contains("account has been banned", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task LoginAsync_WithUnverifiedAccount_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "unverified@example.com";
        var password = "Password123!";

        var hashedPassword = new PasswordHasher().HashPassword(password);
        var user = new User
        {
            Id = userId,
            Email = email,
            Password = hashedPassword,
            UserStatus = UserStatus.Pending,
            Role = RoleType.Member,
            IsEmailVerified = false
        };

        var loginDto = new LoginRequestDto
        {
            Email = email,
            Password = password
        };

        _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _authService.LoginAsync(loginDto, _mockConfiguration.Object));

        Assert.Contains("Account have not verified yet", exception.Message);
    }

    [Fact]
    public async Task LogoutAsync_WithValidUser_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserStatus = UserStatus.Active,
            RefreshToken = "valid-refresh-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        };

        _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _authService.LogoutAsync(userId);

        // Assert
        Assert.True(result);
        _mockUserRepository.Verify(repo => repo.Update(It.Is<User>(u =>
            u.Id == userId &&
            u.RefreshToken == null &&
            u.RefreshTokenExpiryTime == null)),
            Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_WithNonExistentUser_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync((User)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _authService.LogoutAsync(userId));

        Assert.Contains("Account does not exist", exception.Message);
    }

    [Fact]
    public async Task LogoutAsync_WithDeletedUser_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "deleted@example.com",
            UserStatus = UserStatus.Deleted,
            IsDeleted = true,
            RefreshToken = "token"
        };

        _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _authService.LogoutAsync(userId));

        Assert.Contains("Account has been disabled or banned", exception.Message);
    }

    [Fact]
    public async Task LogoutAsync_WithAlreadyLoggedOutUser_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "loggedout@example.com",
            UserStatus = UserStatus.Active,
            RefreshToken = null
        };

        _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _authService.LogoutAsync(userId));

        Assert.Contains("User previously logged out", exception.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshToken = "valid-refresh-token";
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            RefreshToken = refreshToken,
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1),
            Role = RoleType.Member
        };

        var refreshTokenDto = new TokenRefreshRequestDto
        {
            RefreshToken = refreshToken
        };

        _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.RefreshTokenAsync(refreshTokenDto, _mockConfiguration.Object);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        Assert.NotEqual(refreshToken, result.RefreshToken);

        _mockUserRepository.Verify(repo => repo.Update(It.Is<User>(u =>
            u.Id == userId &&
            u.RefreshToken != refreshToken &&
            u.RefreshTokenExpiryTime != null)),
            Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithEmptyToken_ThrowsException()
    {
        // Arrange
        var refreshTokenDto = new TokenRefreshRequestDto
        {
            RefreshToken = string.Empty
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _authService.RefreshTokenAsync(refreshTokenDto, _mockConfiguration.Object));

        Assert.Contains("Missing tokens", exception.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshToken = "expired-refresh-token";
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            RefreshToken = refreshToken,
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1) // Expired
        };

        var refreshTokenDto = new TokenRefreshRequestDto
        {
            RefreshToken = refreshToken
        };

        _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _authService.RefreshTokenAsync(refreshTokenDto, _mockConfiguration.Object));

        Assert.Contains("Refresh token has expired", exception.Message);
    }

    [Fact]
    public async Task RegisterUserAsync_WithValidData_ReturnsUserDto()
    {
        // Arrange
        var registrationDto = new UserRegistrationDto
        {
            Email = "new@example.com",
            Password = "Password123!",
            FullName = "New User",
            PhoneNumber = "0123456789",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User)null);

        _mockUserRepository.Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _authService.RegisterUserAsync(registrationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(registrationDto.Email, result.Email);
        Assert.Equal(registrationDto.FullName, result.FullName);

        _mockUserRepository.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Exactly(2)); // Once for user, once for OTP
        _mockEmailService.Verify(email => email.SendOtpVerificationEmailAsync(It.IsAny<EmailRequestDto>()), Times.Once);
    }

    [Fact]
    public async Task RegisterUserAsync_WithExistingEmail_ThrowsException()
    {
        // Arrange
        var registrationDto = new UserRegistrationDto
        {
            Email = "existing@example.com",
            Password = "Password123!",
            FullName = "Existing User",
            PhoneNumber = "0123456789"
        };

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = registrationDto.Email
        };

        _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _authService.RegisterUserAsync(registrationDto));

        Assert.Contains("Email have been used", exception.Message);
    }

    [Fact]
    public async Task VerifyEmailOtpAsync_WithValidOtp_ReturnsTrue()
    {
        // Arrange
        var email = "unverified@example.com";
        var otp = "123456";

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            IsEmailVerified = false,
            UserStatus = UserStatus.Pending,
            Role = RoleType.Customer
        };

        var otpStorage = new OtpStorage
        {
            Id = Guid.NewGuid(),
            Target = email,
            OtpCode = otp,
            IsUsed = false,
            Purpose = OtpPurpose.Register,
            ExpiredAt = DateTime.UtcNow.AddMinutes(5)
        };

        _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(uow => uow.OtpStorages.FirstOrDefaultAsync(It.IsAny<Expression<Func<OtpStorage, bool>>>()))
            .ReturnsAsync(otpStorage);

        // Act
        var result = await _authService.VerifyEmailOtpAsync(email, otp);

        // Assert
        Assert.True(result);

        _mockUserRepository.Verify(repo => repo.Update(It.Is<User>(u =>
            u.Email == email &&
            u.IsEmailVerified == true &&
            u.UserStatus == UserStatus.Active &&
            u.Role == RoleType.Member)),
            Times.Once);

        _mockUnitOfWork.Verify(uow => uow.OtpStorages.Update(It.Is<OtpStorage>(o =>
            o.Target == email &&
            o.IsUsed == true)),
            Times.Once);

        _mockEmailService.Verify(email => email.SendRegistrationSuccessEmailAsync(
            It.Is<EmailRequestDto>(dto => dto.To == user.Email)),
            Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidOtp_ReturnsTrue()
    {
        // Arrange
        var email = "user@example.com";
        var otp = "123456";
        var newPassword = "NewPassword123!";

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            IsEmailVerified = true,
            UserStatus = UserStatus.Active
        };

        var otpStorage = new OtpStorage
        {
            Id = Guid.NewGuid(),
            Target = email,
            OtpCode = otp,
            IsUsed = false,
            Purpose = OtpPurpose.ForgotPassword,
            ExpiredAt = DateTime.UtcNow.AddMinutes(5)
        };

        _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(uow => uow.OtpStorages.FirstOrDefaultAsync(It.IsAny<Expression<Func<OtpStorage, bool>>>()))
            .ReturnsAsync(otpStorage);

        // Act
        var result = await _authService.ResetPasswordAsync(email, otp, newPassword);

        // Assert
        Assert.True(result);

        _mockUserRepository.Verify(repo => repo.Update(It.Is<User>(u =>
            u.Email == email &&
            u.Password != null)),
            Times.Once);

        _mockEmailService.Verify(email => email.SendPasswordChangeSuccessAsync(
            It.Is<EmailRequestDto>(dto => dto.To == user.Email)),
            Times.Once);
    }

    [Fact]
    public async Task ResendOtpAsync_ForRegistration_ReturnsTrue()
    {
        // Arrange
        var email = "unverified@example.com";

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            IsEmailVerified = false,
            UserStatus = UserStatus.Pending,
            FullName = "Unverified User"
        };

        _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.ResendOtpAsync(email, OtpPurpose.Register);

        // Assert
        Assert.True(result);
        _mockUnitOfWork.Verify(uow => uow.OtpStorages.AddAsync(It.IsAny<OtpStorage>()), Times.Once);
        _mockEmailService.Verify(email => email.SendOtpVerificationEmailAsync(It.Is<EmailRequestDto>(dto =>
            dto.To == user.Email && dto.UserName == user.FullName)), Times.Once);
    }

    [Fact]
    public async Task EmployeeCreateCustomerAsync_WithValidData_ReturnsUserDto()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var customerDto = new AddCustomerDto
        {
            Email = "customer@example.com",
            FullName = "New Customer",
            PhoneNumber = "0123456789"
        };

        _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User)null);

        _mockUserRepository.Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _authService.EmployeeCreateCustomerAsync(customerDto, employeeId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(customerDto.Email, result.Email);
        Assert.Equal(customerDto.FullName, result.FullName);

        _mockUserRepository.Verify(repo => repo.AddAsync(It.Is<User>(u =>
            u.Email == customerDto.Email &&
            u.UserStatus == UserStatus.Active &&
            u.Role == RoleType.Customer &&
            u.IsEmailVerified == true &&
            u.CreatedBy == employeeId)),
            Times.Once);

        _mockEmailService.Verify(email => email.SendEmployeeCredentialsEmailAsync(
            It.IsAny<EmployeeCredentialsEmailDto>()), Times.Once);
    }
}

