using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.API.Controllers.Cronjob;
using MovieTheater.Application.Interfaces.Cronjob;
using MovieTheater.Application.Utils;

namespace MovieTheater.UnitTest.Controllers.Cronjob
{
    public class TaskCleanupControllerTests
    {
        private readonly Mock<ITaskCleanupService> _cleanupServiceMock;
        private readonly TaskCleanupController _controller;

        public TaskCleanupControllerTests()
        {
            _cleanupServiceMock = new Mock<ITaskCleanupService>();
            _controller = new TaskCleanupController(_cleanupServiceMock.Object);
        }

        [Fact]
        public async Task CleanupPastShowTimes_ReturnsOk_WithCount()
        {
            // Arrange
            int deletedCount = 5;
            _cleanupServiceMock.Setup(s => s.CleanupPastShowTimesAsync()).ReturnsAsync(deletedCount);

            // Act
            var result = await _controller.CleanupPastShowTimes();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<int>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(deletedCount, apiResult.Value.Data);
            Assert.Equal("200", apiResult.Value.Code);
        }

        [Fact]
        public async Task CleanupPastShowTimes_Exception_ReturnsErrorResponse()
        {
            // Arrange
            _cleanupServiceMock.Setup(s => s.CleanupPastShowTimesAsync())
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.CleanupPastShowTimes();

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("500", apiResult.Error.Code);
        }
    }
}