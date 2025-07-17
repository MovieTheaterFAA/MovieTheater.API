using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.API.Controllers;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.ShowTimeDTOs;
using static MovieTheater.Domain.DTOs.ShowTimeDTOs.BatchShowtimeRequestDto;


namespace MovieTheater.UnitTest.Controllers
{
    public class ShowTimeControllerTests
    {
        private readonly Mock<IShowTimeService> _mockService;
        private readonly ShowTimeController _controller;

        public ShowTimeControllerTests()
        {
            _mockService = new Mock<IShowTimeService>();
            _controller = new ShowTimeController(_mockService.Object);
        }

        [Fact]
        public async Task AddBatchShowTimesAsync_ReturnsOk_WhenSuccess()
        {
            // Arrange
            var request = new BatchShowTimeRequestDto();
            var response = new List<ShowtimeResponseDTO>();
            _mockService.Setup(s => s.AddBatchShowTimesAsync(request)).ReturnsAsync(response);

            // Act
            var result = await _controller.AddBatchShowTimesAsync(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<List<ShowtimeResponseDTO>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetShowTimesByMovieAndDate_ReturnsOk_WhenSuccess()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var date = DateTime.UtcNow.Date;
            var response = new List<ShowtimeResponseDTO>();
            _mockService.Setup(s => s.GetShowTimesByMovieAndDateAsync(movieId, date)).ReturnsAsync(response);

            // Act
            var result = await _controller.GetShowTimesByMovieAndDate(movieId, date);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<List<ShowtimeResponseDTO>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetShowTimesByDate_ReturnsOk_WhenSuccess()
        {
            // Arrange
            var date = DateTime.UtcNow.Date;
            var response = new List<ShowtimeResponseDTO>();
            _mockService.Setup(s => s.GetShowTimesByDateAsync(date, null, null)).ReturnsAsync(response);

            // Act
            var result = await _controller.GetShowTimesByDate(date, null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<List<ShowtimeResponseDTO>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
        }

        [Fact]
        public async Task DeleteShowTimesByDate_ReturnsOk_WhenDeleted()
        {
            // Arrange
            var date = DateTime.UtcNow.Date;
            _mockService.Setup(s => s.DeleteShowTimesByDateAsync(date)).ReturnsAsync(2);

            // Act
            var result = await _controller.DeleteShowTimesByDate(date);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
        }

        [Fact]
        public async Task DeleteShowTimesByDate_ReturnsNotFound_WhenNoneDeleted()
        {
            // Arrange
            var date = DateTime.UtcNow.Date;
            _mockService.Setup(s => s.DeleteShowTimesByDateAsync(date)).ReturnsAsync(0);

            // Act
            var result = await _controller.DeleteShowTimesByDate(date);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(notFoundResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task UpdateShowTime_ReturnsOk_WhenSuccess()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new UpdateShowtimeDto();
            var response = new ShowtimeResponseDTO();
            _mockService.Setup(s => s.UpdateShowTimeAsync(id, dto)).ReturnsAsync(response);

            // Act
            var result = await _controller.UpdateShowTime(id, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<ShowtimeResponseDTO>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
        }

        [Fact]
        public async Task UpdateShowTime_ReturnsNotFound_WhenKeyNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new UpdateShowtimeDto();
            _mockService.Setup(s => s.UpdateShowTimeAsync(id, dto)).ThrowsAsync(new KeyNotFoundException("Not found"));

            // Act
            var result = await _controller.UpdateShowTime(id, dto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(notFoundResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task UpdateShowTime_ReturnsBadRequest_WhenInvalidOperation()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new UpdateShowtimeDto();
            _mockService.Setup(s => s.UpdateShowTimeAsync(id, dto)).ThrowsAsync(new InvalidOperationException("Invalid"));

            // Act
            var result = await _controller.UpdateShowTime(id, dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task SoftDeleteShowTime_ReturnsOk_WhenSuccess()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.SoftDeleteShowTimeAsync(id)).ReturnsAsync(true);

            // Act
            var result = await _controller.SoftDeleteShowTime(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
        }

        [Fact]
        public async Task SoftDeleteShowTime_ReturnsNotFound_WhenNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.SoftDeleteShowTimeAsync(id)).ReturnsAsync(false);

            // Act
            var result = await _controller.SoftDeleteShowTime(id);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(notFoundResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task AddBatchShowTimesAsync_ReturnsServerError_WhenException()
        {
            // Arrange
            var request = new BatchShowTimeRequestDto();
            _mockService.Setup(s => s.AddBatchShowTimesAsync(request)).ThrowsAsync(new Exception("Unexpected"));

            // Act
            var result = await _controller.AddBatchShowTimesAsync(request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetShowTimesByMovieAndDate_ReturnsServerError_WhenException()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var date = DateTime.UtcNow.Date;
            _mockService.Setup(s => s.GetShowTimesByMovieAndDateAsync(movieId, date)).ThrowsAsync(new Exception("Unexpected"));

            // Act
            var result = await _controller.GetShowTimesByMovieAndDate(movieId, date);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetShowTimesByDate_ReturnsServerError_WhenException()
        {
            // Arrange
            var date = DateTime.UtcNow.Date;
            _mockService.Setup(s => s.GetShowTimesByDateAsync(date, null, null)).ThrowsAsync(new Exception("Unexpected"));

            // Act
            var result = await _controller.GetShowTimesByDate(date, null, null);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task DeleteShowTimesByDate_ReturnsServerError_WhenException()
        {
            // Arrange
            var date = DateTime.UtcNow.Date;
            _mockService.Setup(s => s.DeleteShowTimesByDateAsync(date)).ThrowsAsync(new Exception("Unexpected"));

            // Act
            var result = await _controller.DeleteShowTimesByDate(date);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task UpdateShowTime_ReturnsServerError_WhenException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new UpdateShowtimeDto();
            _mockService.Setup(s => s.UpdateShowTimeAsync(id, dto)).ThrowsAsync(new Exception("Unexpected"));

            // Act
            var result = await _controller.UpdateShowTime(id, dto);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task SoftDeleteShowTime_ReturnsServerError_WhenException()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.SoftDeleteShowTimeAsync(id)).ThrowsAsync(new Exception("Unexpected"));

            // Act
            var result = await _controller.SoftDeleteShowTime(id);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetShowTimesByDate_SpecifiesKind_WhenDateKindIsUnspecified()
        {
            // Arrange
            var date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var response = new List<ShowtimeResponseDTO>();
            _mockService.Setup(s => s.GetShowTimesByDateAsync(
                It.Is<DateTime>(d => d.Kind == DateTimeKind.Utc), null, null))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.GetShowTimesByDate(date, null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<List<ShowtimeResponseDTO>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            _mockService.Verify(s => s.GetShowTimesByDateAsync(
                It.Is<DateTime>(d => d.Kind == DateTimeKind.Utc), null, null), Times.Once);
        }

        [Fact]
        public async Task DeleteShowTimesByDate_SpecifiesKind_WhenDateKindIsUnspecified()
        {
            // Arrange
            var date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            _mockService.Setup(s => s.DeleteShowTimesByDateAsync(
                It.Is<DateTime>(d => d.Kind == DateTimeKind.Utc)))
                .ReturnsAsync(1);

            // Act
            var result = await _controller.DeleteShowTimesByDate(date);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            _mockService.Verify(s => s.DeleteShowTimesByDateAsync(
                It.Is<DateTime>(d => d.Kind == DateTimeKind.Utc)), Times.Once);
        }

        [Fact]
        public async Task GetShowTimesByMovieAndDate_SpecifiesKind_WhenDateKindIsUnspecified()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var response = new List<ShowtimeResponseDTO>();
            _mockService.Setup(s => s.GetShowTimesByMovieAndDateAsync(
                movieId, It.Is<DateTime>(d => d.Kind == DateTimeKind.Utc)))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.GetShowTimesByMovieAndDate(movieId, date);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<List<ShowtimeResponseDTO>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            _mockService.Verify(s => s.GetShowTimesByMovieAndDateAsync(
                movieId, It.Is<DateTime>(d => d.Kind == DateTimeKind.Utc)), Times.Once);
        }
    }
}