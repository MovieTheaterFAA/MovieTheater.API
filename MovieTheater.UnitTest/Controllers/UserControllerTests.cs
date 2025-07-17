using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.API.Controllers;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.UnitTest.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IImpersonationService> _mockImpersonationService;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockImpersonationService = new Mock<IImpersonationService>();
            _controller = new UserController(
                _mockUserService.Object,
                _mockImpersonationService.Object
            );
        }

        [Fact]
        public async Task GetUserProfile_ReturnsOkResult_WithUserData()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userDto = new CurrentUserDto
            {
                FullName = "Test User",
                Email = "test@example.com",
                PhoneNumber = "123456789",
                Role = RoleType.Customer
            };

            _mockImpersonationService.Setup(s => s.GetEffectiveUserId()).Returns(userId);
            _mockUserService.Setup(s => s.GetUserDetails(userId)).ReturnsAsync(userDto);

            // Act
            var result = await _controller.GetUserProfile();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(okResult.Value);
            Assert.Equal(200, okResult.StatusCode);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal("User profile retrieved successfully.", apiResult.Value.Message);
            Assert.Equal(userDto, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetUserProfile_HandlesException_ReturnsErrorResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var exception = new Exception("Test exception");

            _mockImpersonationService.Setup(s => s.GetEffectiveUserId()).Returns(userId);
            _mockUserService.Setup(s => s.GetUserDetails(userId)).ThrowsAsync(exception);

            // Act
            var result = await _controller.GetUserProfile();

            // Assert
            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task UpdateUserProfile_ValidData_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var updateDto = new UserUpdateDto
            {
                FullName = "Updated Name",
                PhoneNumber = "987654321"
            };

            _mockImpersonationService.Setup(s => s.GetEffectiveUserId()).Returns(userId);
            _mockUserService.Setup(s => s.UpdateUserInfo(userId, updateDto)).ReturnsAsync(updateDto);

            // Act
            var result = await _controller.UpdateUserProfile(updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<UserUpdateDto>>(okResult.Value);
            Assert.Equal(200, okResult.StatusCode);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal("User profile updated successfully.", apiResult.Value.Message);
            Assert.Equal(updateDto, apiResult.Value.Data);
        }

        [Fact]
        public async Task UpdateUserProfile_NullData_ReturnsBadRequest()
        {
            // Arrange
            UserUpdateDto? nullDto = null;

            // Act
            var result = await _controller.UpdateUserProfile(nullDto!);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.Equal(400, badRequestResult.StatusCode);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("User update data is required.", apiResult.Error.Message);
        }

        [Fact]
        public async Task UpdateUserProfile_EmptyUserId_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new UserUpdateDto { FullName = "Test Name" };
            _mockImpersonationService.Setup(s => s.GetEffectiveUserId()).Returns(Guid.Empty);

            // Act
            var result = await _controller.UpdateUserProfile(updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.Equal(400, badRequestResult.StatusCode);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("Invalid or missing user ID.", apiResult.Error.Message);
        }

        [Fact]
        public async Task UpdateUserProfile_HandlesException_ReturnsErrorResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var updateDto = new UserUpdateDto { FullName = "Test Name" };
            var exception = new Exception("Test exception");

            _mockImpersonationService.Setup(s => s.GetEffectiveUserId()).Returns(userId);
            _mockUserService.Setup(s => s.UpdateUserInfo(userId, updateDto)).ThrowsAsync(exception);

            // Act
            var result = await _controller.UpdateUserProfile(updateDto);

            // Assert
            Assert.IsType<ObjectResult>(result);
        }
    }
}