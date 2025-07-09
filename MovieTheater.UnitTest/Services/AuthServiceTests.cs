using Microsoft.Extensions.Configuration;
using Moq;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.AuthenDTOs;
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
}

