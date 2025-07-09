using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.API.Controllers;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.Entities;
using MovieTheater.Infrastructure.Interfaces;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MovieTheater.UnitTest.Controllers
{
    public class ImpersonationControllerTests
    {
        private readonly Mock<IImpersonationService> _mockImpersonationService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IClaimsService> _mockClaimsService;
        private readonly ImpersonationController _controller;
        private readonly Guid _currentUserId = Guid.NewGuid();
        private readonly Guid _targetUserId = Guid.NewGuid();

        public ImpersonationControllerTests()
        {
            _mockImpersonationService = new Mock<IImpersonationService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockClaimsService = new Mock<IClaimsService>();

            var mockUserRepo = new Mock<IGenericRepository<User>>();
            _mockUnitOfWork.Setup(u => u.Users).Returns(mockUserRepo.Object);
            _mockClaimsService.Setup(c => c.GetCurrentUserId).Returns(_currentUserId);

            _controller = new ImpersonationController(
                _mockImpersonationService.Object,
                _mockUnitOfWork.Object,
                _mockClaimsService.Object);
        }

        [Fact]
        public async Task Start_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var user = new User { Id = _currentUserId };
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(_currentUserId, It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>>()))
                .ReturnsAsync(user);
            _mockImpersonationService.Setup(s => s.StartImpersonationAsync(_targetUserId, "Testing"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Start(_targetUserId, "Testing");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(okResult.Value);
            Assert.Equal("200", apiResult.Value.Code);
            Assert.Equal("Impersonation started.", apiResult.Value.Message);
        }

        [Fact]
        public async Task Start_FailedImpersonation_ReturnsBadRequest()
        {
            // Arrange
            var user = new User { Id = _currentUserId };
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(_currentUserId, It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>>()))
                .ReturnsAsync(user);
            _mockImpersonationService.Setup(s => s.StartImpersonationAsync(_targetUserId, "Testing"))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Start(_targetUserId, "Testing");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.Equal("400", apiResult.Error.Code);
            Assert.Equal("Failed to impersonate.", apiResult.Error.Message);
        }

        [Fact]
        public async Task Start_ExceptionThrown_ReturnsErrorResponse()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(_currentUserId, It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.Start(_targetUserId, "Testing");

            // Assert
            var objectResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, objectResult.StatusCode);
        }

        [Fact]
        public async Task Stop_SuccessfulStop_ReturnsOkResult()
        {
            // Arrange
            _mockImpersonationService.Setup(s => s.StopImpersonationAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Stop();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(okResult.Value);
            Assert.Equal("200", apiResult.Value.Code);
            Assert.Equal("Impersonation stopped.", apiResult.Value.Message);
        }

        [Fact]
        public async Task Stop_NotImpersonating_ReturnsBadRequest()
        {
            // Arrange
            _mockImpersonationService.Setup(s => s.StopImpersonationAsync())
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Stop();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.Equal("400", apiResult.Error.Code);
            Assert.Equal("Not impersonating.", apiResult.Error.Message);
        }

        [Fact]
        public async Task Stop_ExceptionThrown_ReturnsErrorResponse()
        {
            // Arrange
            _mockImpersonationService.Setup(s => s.StopImpersonationAsync())
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.Stop();

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public void Status_ReturnsCorrectStatusInfo()
        {
            // Arrange
            _mockImpersonationService.Setup(s => s.IsImpersonating()).Returns(true);
            _mockImpersonationService.Setup(s => s.GetEffectiveUserId()).Returns(_targetUserId);
            _mockImpersonationService.Setup(s => s.GetImpersonatedBy()).Returns(_currentUserId);

            // Act
            var result = _controller.Status();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(okResult.Value);
            Assert.Equal("200", apiResult.Value.Code);

            // Check the status object properties using reflection
            var status = apiResult.Value.Data;
            var type = status.GetType();

            Assert.True((bool)type.GetProperty("isImpersonating")!.GetValue(status)!);
            Assert.Equal(_targetUserId, (Guid)type.GetProperty("effectiveUserId")!.GetValue(status)!);
            Assert.Equal(_currentUserId, (Guid)type.GetProperty("impersonatedBy")!.GetValue(status)!);
        }

        [Fact]
        public void Status_ExceptionThrown_ReturnsErrorResponse()
        {
            // Arrange
            _mockImpersonationService.Setup(s => s.IsImpersonating())
                .Throws(new Exception("Test exception"));

            // Act
            var result = _controller.Status();

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }
    }
}