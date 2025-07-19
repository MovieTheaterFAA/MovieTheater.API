using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.API.Controllers;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.Entities;

namespace MovieTheater.UnitTest.Controllers
{
    public class ScoreControllerTests
    {
        private readonly Mock<IScoreService> _scoreServiceMock;
        private readonly ScoreController _controller;

        public ScoreControllerTests()
        {
            _scoreServiceMock = new Mock<IScoreService>();
            _controller = new ScoreController(_scoreServiceMock.Object);
        }

        [Fact]
        public async Task GetCurrentScoreAsync_ReturnsOkResult_WithScore()
        {
            // Arrange
            int expectedScore = 100;
            _scoreServiceMock.Setup(s => s.GetCurrentScoreAsync())
                .ReturnsAsync(expectedScore);

            // Act
            var result = await _controller.GetCurrentScoreAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<int>>(okResult.Value);
            Assert.Equal(expectedScore, apiResult.Value.Data);
            Assert.Equal("200", apiResult.Value.Code);
        }

        [Fact]
        public async Task GetCurrentScoreAsync_WhenException_ReturnsErrorResult()
        {
            // Arrange
            _scoreServiceMock.Setup(s => s.GetCurrentScoreAsync())
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetCurrentScoreAsync();

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.True(objectResult.StatusCode >= 400);
        }

        [Fact]
        public async Task GetScoreHistoryAsync_ReturnsOkResult_WithHistory()
        {
            // Arrange
            var expectedHistory = new List<ScoreHistory>
            {
                new ScoreHistory { Id = new Guid(), ScoreValue = 50 },
                new ScoreHistory { Id = new Guid(), ScoreValue = 60 }
            };
            _scoreServiceMock.Setup(s => s.GetScoreHistoryAsync())
                .ReturnsAsync(expectedHistory);

            // Act
            var result = await _controller.GetScoreHistoryAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<List<ScoreHistory>>>(okResult.Value);
            Assert.Equal(expectedHistory, apiResult.Value.Data);
            Assert.Equal("200", apiResult.Value.Code);
        }

        [Fact]
        public async Task GetScoreHistoryAsync_WhenException_ReturnsErrorResult()
        {
            // Arrange
            _scoreServiceMock.Setup(s => s.GetScoreHistoryAsync())
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetScoreHistoryAsync();

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.True(objectResult.StatusCode >= 400);
        }
        [Fact]
        public async Task GetCurrentScoreAsync_ReturnsOkResult_WithZeroScore()
        {
            // Arrange
            int expectedScore = 0;
            _scoreServiceMock.Setup(s => s.GetCurrentScoreAsync())
                .ReturnsAsync(expectedScore);

            // Act
            var result = await _controller.GetCurrentScoreAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<int>>(okResult.Value);
            Assert.Equal(expectedScore, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetScoreHistoryAsync_ReturnsOkResult_WithEmptyHistory()
        {
            // Arrange
            var expectedHistory = new List<ScoreHistory>();
            _scoreServiceMock.Setup(s => s.GetScoreHistoryAsync())
                .ReturnsAsync(expectedHistory);

            // Act
            var result = await _controller.GetScoreHistoryAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<List<ScoreHistory>>>(okResult.Value);
            Assert.Empty(apiResult.Value.Data);
        }

        [Fact]
        public async Task GetCurrentScoreAsync_ReturnsOkResult_WithNullScore()
        {
            // Arrange
            _scoreServiceMock.Setup(s => s.GetCurrentScoreAsync())
                .ReturnsAsync((int)default);

            // Act
            var result = await _controller.GetCurrentScoreAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<int>>(okResult.Value);
            Assert.Equal(0, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetScoreHistoryAsync_ReturnsOkResult_WithNullHistory()
        {
            // Arrange
            _scoreServiceMock.Setup(s => s.GetScoreHistoryAsync())
                .ReturnsAsync((List<ScoreHistory>)null!);

            // Act
            var result = await _controller.GetScoreHistoryAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<List<ScoreHistory>>>(okResult.Value);
            Assert.Null(apiResult.Value.Data);
        }
    }
}