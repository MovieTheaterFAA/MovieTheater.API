using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.API.Controllers;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.EventDTOs;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.UnitTest.Controllers
{
    public class EventControllerTests
    {
        private readonly Mock<IEventService> _mockEventService;
        private readonly Mock<IClaimsService> _mockClaimsService;
        private readonly EventController _controller;

        public EventControllerTests()
        {
            _mockEventService = new Mock<IEventService>();
            _mockClaimsService = new Mock<IClaimsService>();
            _controller = new EventController(
                _mockEventService.Object,
                _mockClaimsService.Object
            );
        }

        [Fact]
        public async Task GetAllEventsAsync_ValidParameters_ReturnsOkResult()
        {
            // Arrange
            var eventsList = new Pagination<EventResponseDto> { Items = new List<EventResponseDto>() };
            _mockEventService.Setup(s => s.GetAllEventsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(eventsList);

            // Act
            var result = await _controller.GetAllEventsAsync("test", "StartTime", true, 1, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<Pagination<EventResponseDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(eventsList, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetAllEventsAsync_InvalidPagination_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetAllEventsAsync(null, null, false, 0, 0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetAllEventsAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            _mockEventService.Setup(s => s.GetAllEventsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetAllEventsAsync("name", null, false, 1, 10);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Theory]
        [InlineData("StartTime", false)]
        [InlineData("EndTime", true)]
        [InlineData("", false)]
        public async Task GetAllEventsAsync_DifferentSortParameters_ReturnsOkResult(string sortBy, bool isDescending)
        {
            // Arrange
            var eventsList = new Pagination<EventResponseDto> { Items = new List<EventResponseDto>() };
            _mockEventService.Setup(s => s.GetAllEventsAsync(It.IsAny<string>(), sortBy, isDescending, It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(eventsList);

            // Act
            var result = await _controller.GetAllEventsAsync(null, sortBy, isDescending, 1, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<ApiResult<Pagination<EventResponseDto>>>(okResult.Value);
        }

        [Fact]
        public async Task AddEventAsync_ValidData_ReturnsOkResult()
        {
            // Arrange
            var eventRequest = new EventRequestDto
            {
                Name = "Test Event",
                StartTime = DateTime.Now.AddDays(1),
                EndTime = DateTime.Now.AddDays(2),
                Detail = "Test event details"
            };
            var eventResponse = new EventResponseDto
            {
                Id = Guid.NewGuid(),
                Name = "Test Event",
                StartTime = DateTime.Now.AddDays(1),
                EndTime = DateTime.Now.AddDays(2),
                Detail = "Test event details"
            };
            _mockEventService.Setup(s => s.AddEventAsync(eventRequest)).ReturnsAsync(eventResponse);

            // Act
            var result = await _controller.AddEventAsync(eventRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<EventResponseDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(eventResponse, apiResult.Value.Data);
        }

        [Fact]
        public async Task AddEventAsync_KeyNotFoundException_ReturnsNotFound()
        {
            // Arrange
            var eventRequest = new EventRequestDto { Name = "Test Event" };
            _mockEventService.Setup(s => s.AddEventAsync(eventRequest)).ThrowsAsync(new KeyNotFoundException("Test not found"));

            // Act
            var result = await _controller.AddEventAsync(eventRequest);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(notFoundResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task AddEventAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var eventRequest = new EventRequestDto { Name = "Test Event" };
            _mockEventService.Setup(s => s.AddEventAsync(eventRequest)).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.AddEventAsync(eventRequest);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<EventResponseDto>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task UpdateEventAsync_ValidData_ReturnsOkResult()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventUpdateDto = new EventUpdateDto { Name = "Updated Event" };
            var eventResponse = new EventResponseDto
            {
                Id = eventId,
                Name = "Updated Event",
                StartTime = DateTime.Now.AddDays(1),
                EndTime = DateTime.Now.AddDays(2),
                Detail = "Updated event details"
            };
            _mockEventService.Setup(s => s.UpdateEventAsync(eventId, eventUpdateDto)).ReturnsAsync(eventResponse);

            // Act
            var result = await _controller.UpdateEventAsync(eventId, eventUpdateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<EventResponseDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(eventResponse, apiResult.Value.Data);
        }

        [Fact]
        public async Task UpdateEventAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventUpdateDto = new EventUpdateDto { Name = "Updated Event" };
            _mockEventService.Setup(s => s.UpdateEventAsync(eventId, eventUpdateDto)).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.UpdateEventAsync(eventId, eventUpdateDto);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<EventResponseDto>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task DeleteEventAsync_ValidId_ReturnsOkResult()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            _mockEventService.Setup(s => s.DeleteEventByIdAsync(eventId)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteEventAsync(eventId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<bool>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.True((bool)apiResult.Value.Data);
        }

        [Fact]
        public async Task DeleteEventAsync_EventNotFound_ReturnsNotFound()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            _mockEventService.Setup(s => s.DeleteEventByIdAsync(eventId)).ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteEventAsync(eventId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(notFoundResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task DeleteEventAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            _mockEventService.Setup(s => s.DeleteEventByIdAsync(eventId)).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.DeleteEventAsync(eventId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }
    }
}