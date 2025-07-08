using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using MovieTheater.API.Controllers;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.AuthenDTOs;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
namespace MovieTheater.UnitTest.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;    
        private readonly Mock<IClaimsService> _mockClaimsService;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockClaimsService = new Mock<IClaimsService>();
            _mockConfig = new Mock<IConfiguration>();
            _controller = new AuthController(
                _mockAuthService.Object,
                _mockClaimsService.Object,
                _mockConfig.Object
            );
        }

        [Fact]
        public async Task Register_ReturnsOkResult_WhenSuccess()
        {
            // Arrange
            var dto = new UserRegistrationDto { Email = "test@example.com", Password = "123", FullName = "Test", PhoneNumber = "0786315267" };
            var user = new UserDto { Email = dto.Email, FullName = dto.FullName };
            _mockAuthService.Setup(s => s.RegisterUserAsync(dto)).ReturnsAsync(user);

            // Act
            var result = await _controller.Register(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<UserDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(user, apiResult.Value.Data);
        }

        [Fact]
        public async Task Register_ReturnsError_WhenException()
        {
            // Arrange
            var dto = new UserRegistrationDto { Email = "test@example.com", Password = "123", FullName = "Test", PhoneNumber = "0786315267" };
            _mockAuthService.Setup(s => s.RegisterUserAsync(dto)).ThrowsAsync(new Exception("fail"));

            // Act
            var result = await _controller.Register(dto);

            // Assert
            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task Login_ReturnsOkResult_WhenSuccess()
        {
            // Arrange
            var dto = new LoginRequestDto { Email = "test@example.com", Password = "123" };
            var loginResponse = new LoginResponseDto { AccessToken = "token", RefreshToken = "refresh" };
            _mockAuthService.Setup(s => s.LoginAsync(dto, _mockConfig.Object)).ReturnsAsync(loginResponse);

            // Act
            var result = await _controller.Login(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<LoginResponseDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(loginResponse, apiResult.Value.Data);
        }

        [Fact]
        public async Task Login_ReturnsError_WhenException()
        {
            // Arrange
            var dto = new LoginRequestDto { Email = "test@example.com", Password = "123" };
            _mockAuthService.Setup(s => s.LoginAsync(dto, _mockConfig.Object)).ThrowsAsync(new Exception("fail"));

            // Act
            var result = await _controller.Login(dto);

            // Assert
            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task Logout_ReturnsOkResult_WhenSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(userId);
            _mockAuthService.Setup(s => s.LogoutAsync(userId)).ReturnsAsync(true);

            // Act
            var result = await _controller.Logout();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
        }

        [Fact]
        public async Task Logout_ReturnsError_WhenException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(userId);
            _mockAuthService.Setup(s => s.LogoutAsync(userId)).ThrowsAsync(new Exception("fail"));

            // Act
            var result = await _controller.Logout();

            // Assert
            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task ResetPassword_ReturnsOkResult_WhenSuccess()
        {
            // Arrange
            var dto = new ResetPasswordDto { Email = "test@example.com", Otp = "123", NewPassword = "newpass" };
            _mockAuthService.Setup(s => s.ResetPasswordAsync(dto.Email, dto.Otp, dto.NewPassword)).ReturnsAsync(true);

            // Act
            var result = await _controller.ResetPassword(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsAssignableFrom<ApiResult>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
        }

        [Fact]
        public async Task ResetPassword_ReturnsBadRequest_WhenFail()
        {
            // Arrange
            var dto = new ResetPasswordDto { Email = "test@example.com", Otp = "123", NewPassword = "newpass" };
            _mockAuthService.Setup(s => s.ResetPasswordAsync(dto.Email, dto.Otp, dto.NewPassword)).ReturnsAsync(false);

            // Act
            var result = await _controller.ResetPassword(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult>(badRequest.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task ResetPassword_ReturnsError_WhenException()
        {
            // Arrange
            var dto = new ResetPasswordDto { Email = "test@example.com", Otp = "123", NewPassword = "newpass" };
            _mockAuthService.Setup(s => s.ResetPasswordAsync(dto.Email, dto.Otp, dto.NewPassword)).ThrowsAsync(new Exception("fail"));

            // Act
            var result = await _controller.ResetPassword(dto);

            // Assert
            Assert.IsType<ObjectResult>(result);
        }
        [Fact]
        public async Task RefreshToken_ReturnsOkResult_WhenSuccess()
        {
            // Arrange
            var dto = new TokenRefreshRequestDto { RefreshToken = "refresh" };
            var loginResponse = new LoginResponseDto { AccessToken = "token", RefreshToken = "refresh" };
            _mockAuthService.Setup(s => s.RefreshTokenAsync(dto, _mockConfig.Object)).ReturnsAsync(loginResponse);

            // Act
            var result = await _controller.RefreshToken(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
        }

        [Fact]
        public async Task RefreshToken_ReturnsError_WhenException()
        {
            // Arrange
            var dto = new TokenRefreshRequestDto { RefreshToken = "refresh" };
            _mockAuthService.Setup(s => s.RefreshTokenAsync(dto, _mockConfig.Object)).ThrowsAsync(new Exception("fail"));

            // Act
            var result = await _controller.RefreshToken(dto);

            // Assert
            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task ResendOtp_ReturnsOkResult_WhenSuccess()
        {
            // Arrange
            var dto = new ResendOtpRequestDto { Email = "test@example.com", Purpose = OtpPurpose.Register };
            _mockAuthService.Setup(s => s.ResendOtpAsync(dto.Email, dto.Purpose)).ReturnsAsync(true);

            // Act
            var result = await _controller.ResendOtp(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsAssignableFrom<ApiResult>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
        }

        [Fact]
        public async Task ResendOtp_ReturnsBadRequest_WhenFail()
        {
            // Arrange
            var dto = new ResendOtpRequestDto { Email = "test@example.com", Purpose = OtpPurpose.Register };
            _mockAuthService.Setup(s => s.ResendOtpAsync(dto.Email, dto.Purpose)).ReturnsAsync(false);

            // Act
            var result = await _controller.ResendOtp(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult>(badRequest.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task ResendOtp_ReturnsError_WhenException()
        {
            // Arrange
            var dto = new ResendOtpRequestDto { Email = "test@example.com", Purpose = OtpPurpose.Register };
            _mockAuthService.Setup(s => s.ResendOtpAsync(dto.Email, dto.Purpose)).ThrowsAsync(new Exception("fail"));

            // Act
            var result = await _controller.ResendOtp(dto);

            // Assert
            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task VerifyOtp_ReturnsOkResult_WhenSuccess()
        {
            // Arrange
            var dto = new VerifyOtpDto { Email = "test@example.com", Otp = "123456" };
            _mockAuthService.Setup(s => s.VerifyEmailOtpAsync(dto.Email, dto.Otp)).ReturnsAsync(true);

            // Act
            var result = await _controller.VerifyOtp(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsAssignableFrom<ApiResult>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
        }

        [Fact]
        public async Task VerifyOtp_ReturnsBadRequest_WhenFail()
        {
            // Arrange
            var dto = new VerifyOtpDto { Email = "test@example.com", Otp = "123456" };
            _mockAuthService.Setup(s => s.VerifyEmailOtpAsync(dto.Email, dto.Otp)).ReturnsAsync(false);

            // Act
            var result = await _controller.VerifyOtp(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult>(badRequest.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task VerifyOtp_ReturnsError_WhenException()
        {
            // Arrange
            var dto = new VerifyOtpDto { Email = "test@example.com", Otp = "123456" };
            _mockAuthService.Setup(s => s.VerifyEmailOtpAsync(dto.Email, dto.Otp)).ThrowsAsync(new Exception("fail"));

            // Act
            var result = await _controller.VerifyOtp(dto);

            // Assert
            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task EmployeeCreateCustomer_ReturnsOkResult_WhenSuccess()
        {
            // Arrange
            var dto = new AddCustomerDto { Email = "test@example.com", FullName = "Test", PhoneNumber = "0123456789" };
            var user = new UserDto { Email = dto.Email, FullName = dto.FullName };
            var employeeId = Guid.NewGuid();
            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(employeeId);
            _mockAuthService.Setup(s => s.EmployeeCreateCustomerAsync(dto, employeeId)).ReturnsAsync(user);

            // Act
            var result = await _controller.EmployeeCreateCustomer(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<UserDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(user, apiResult.Value.Data);
        }

        [Fact]
        public async Task EmployeeCreateCustomer_ReturnsError_WhenException()
        {
            // Arrange
            var dto = new AddCustomerDto { Email = "test@example.com", FullName = "Test", PhoneNumber = "0123456789" };
            var employeeId = Guid.NewGuid();
            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(employeeId);
            _mockAuthService.Setup(s => s.EmployeeCreateCustomerAsync(dto, employeeId)).ThrowsAsync(new Exception("fail"));

            // Act
            var result = await _controller.EmployeeCreateCustomer(dto);

            // Assert
            Assert.IsType<ObjectResult>(result);
        }
    }
}
