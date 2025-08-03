using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.API.Controllers;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.StatisticDTOs;

namespace MovieTheater.UnitTest.Controllers
{
    public class StatisticControllerTests
    {
        private readonly Mock<IStatisticService> _statisticServiceMock;
        private readonly StatisticController _controller;

        public StatisticControllerTests()
        {
            _statisticServiceMock = new Mock<IStatisticService>();
            _controller = new StatisticController(_statisticServiceMock.Object);
        }

        [Fact]
        public async Task GetRegisterPerMonthAsync_ReturnsOk_WhenDataExists()
        {
            // Arrange
            var expectedData = new List<MonthlyRegisterDto>
            {
                new() { Month = 1, Year = 2023, TotalRegisters = 10 },
                new() { Month = 2, Year = 2023, TotalRegisters = 15 }
            };
            _statisticServiceMock.Setup(s => s.GetRegisterPerMonthAsync()).ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetRegisterPerMonthAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<List<MonthlyRegisterDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal("200", apiResult.Value.Code);
            Assert.Equal("Monthly registration statistics retrieved successfully.", apiResult.Value.Message);
            Assert.Equal(expectedData, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetRegisterPerMonthAsync_ReturnsStatusCode_OnException()
        {
            // Arrange
            _statisticServiceMock.Setup(s => s.GetRegisterPerMonthAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetRegisterPerMonthAsync();

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetMonthlyRevenueAsync_ReturnsOk_WhenDataExists()
        {
            // Arrange
            var expectedData = new List<MonthlyRevenueDto>
            {
                new() { Month = 1, Year = 2023, TotalRevenue = 1000.50m },
                new() { Month = 2, Year = 2023, TotalRevenue = 1500.75m }
            };
            _statisticServiceMock.Setup(s => s.GetMonthlyRevenueAsync()).ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetMonthlyRevenueAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<List<MonthlyRevenueDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal("200", apiResult.Value.Code);
            Assert.Equal("Monthly revenue statistics retrieved successfully.", apiResult.Value.Message);
            Assert.Equal(expectedData, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetMonthlyRevenueAsync_ReturnsStatusCode_OnException()
        {
            // Arrange
            _statisticServiceMock.Setup(s => s.GetMonthlyRevenueAsync())
                .ThrowsAsync(new Exception("Service unavailable"));

            // Act
            var result = await _controller.GetMonthlyRevenueAsync();

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetMonthlyRevenueMovieAsync_ReturnsOk_WhenValidParameters()
        {
            // Arrange
            var monthYear = new MonthYearDto { Month = 6, Year = 2023 };
            var expectedData = new List<MonthlyMovieRevenueDto>
            {
                new() { MovieId = Guid.NewGuid(), MovieName = "Movie 1", TotalRevenue = 500.25m, TotalTickets = 10 },
                new() { MovieId = Guid.NewGuid(), MovieName = "Movie 2", TotalRevenue = 750.50m, TotalTickets = 15 }
            };
            _statisticServiceMock.Setup(s => s.GetMonthlyRevenueMovieAsync(monthYear)).ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetMonthlyRevenueMovieAsync(monthYear);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<List<MonthlyMovieRevenueDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal("200", apiResult.Value.Code);
            Assert.Equal("Monthly movie revenue statistics retrieved successfully.", apiResult.Value.Message);
            Assert.Equal(expectedData, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetMonthlyRevenueMovieAsync_ReturnsBadRequest_WhenMonthIsInvalid()
        {
            // Arrange
            var monthYear = new MonthYearDto { Month = 13, Year = 2023 }; // Invalid month

            // Act
            var result = await _controller.GetMonthlyRevenueMovieAsync(monthYear);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("400", apiResult.Error.Code);
            Assert.Equal("Month must be between 1 and 12.", apiResult.Error.Message);
        }

        [Fact]
        public async Task GetMonthlyRevenueMovieAsync_ReturnsBadRequest_WhenMonthIsZero()
        {
            // Arrange
            var monthYear = new MonthYearDto { Month = 0, Year = 2023 }; // Invalid month

            // Act
            var result = await _controller.GetMonthlyRevenueMovieAsync(monthYear);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("400", apiResult.Error.Code);
            Assert.Equal("Month must be between 1 and 12.", apiResult.Error.Message);
        }

        [Fact]
        public async Task GetMonthlyRevenueMovieAsync_ReturnsBadRequest_WhenYearIsTooOld()
        {
            // Arrange
            var monthYear = new MonthYearDto { Month = 6, Year = 1999 }; // Year too old

            // Act
            var result = await _controller.GetMonthlyRevenueMovieAsync(monthYear);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("400", apiResult.Error.Code);
            Assert.Equal("Year is out of valid range.", apiResult.Error.Message);
        }

        [Fact]
        public async Task GetMonthlyRevenueMovieAsync_ReturnsBadRequest_WhenYearIsTooNew()
        {
            // Arrange
            var monthYear = new MonthYearDto { Month = 6, Year = DateTime.UtcNow.Year + 1 }; // Year too new

            // Act
            var result = await _controller.GetMonthlyRevenueMovieAsync(monthYear);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("400", apiResult.Error.Code);
            Assert.Equal("Year is out of valid range.", apiResult.Error.Message);
        }

        [Fact]
        public async Task GetMonthlyRevenueMovieAsync_ReturnsOk_WhenYearIsCurrentYear()
        {
            // Arrange
            var monthYear = new MonthYearDto { Month = 6, Year = DateTime.UtcNow.Year }; // Current year should be valid
            var expectedData = new List<MonthlyMovieRevenueDto>();
            _statisticServiceMock.Setup(s => s.GetMonthlyRevenueMovieAsync(monthYear)).ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetMonthlyRevenueMovieAsync(monthYear);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<List<MonthlyMovieRevenueDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetMonthlyRevenueMovieAsync_ReturnsStatusCode_OnException()
        {
            // Arrange
            var monthYear = new MonthYearDto { Month = 6, Year = 2023 };
            _statisticServiceMock.Setup(s => s.GetMonthlyRevenueMovieAsync(monthYear))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.GetMonthlyRevenueMovieAsync(monthYear);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetMonthlyTicketTypeStatisticsAsync_ReturnsOk_WhenValidParameters()
        {
            // Arrange
            var monthYear = new MonthYearDto { Month = 6, Year = 2023 };
            var expectedData = new MonthlyTicketTypeStatisticDto
            {
                OnlineTicketCount = 25,
                OfflineTicketCount = 15,
                GuestTicketCount = 10,
                TicketCount = 50
            };
            _statisticServiceMock.Setup(s => s.GetMonthlyTicketTypeStatisticsAsync(monthYear)).ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetMonthlyTicketTypeStatisticsAsync(monthYear);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<MonthlyTicketTypeStatisticDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal("200", apiResult.Value.Code);
            Assert.Equal("Monthly ticket type statistics retrieved successfully.", apiResult.Value.Message);
            Assert.Equal(expectedData, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetMonthlyTicketTypeStatisticsAsync_ReturnsBadRequest_WhenMonthIsInvalid()
        {
            // Arrange
            var monthYear = new MonthYearDto { Month = 15, Year = 2023 }; // Invalid month

            // Act
            var result = await _controller.GetMonthlyTicketTypeStatisticsAsync(monthYear);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("400", apiResult.Error.Code);
            Assert.Equal("Month must be between 1 and 12.", apiResult.Error.Message);
        }

        [Fact]
        public async Task GetMonthlyTicketTypeStatisticsAsync_ReturnsBadRequest_WhenYearIsInvalid()
        {
            // Arrange
            var monthYear = new MonthYearDto { Month = 6, Year = 1900 }; // Invalid year

            // Act
            var result = await _controller.GetMonthlyTicketTypeStatisticsAsync(monthYear);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("400", apiResult.Error.Code);
            Assert.Equal("Year is out of valid range.", apiResult.Error.Message);
        }

        [Fact]
        public async Task GetMonthlyTicketTypeStatisticsAsync_ReturnsStatusCode_OnException()
        {
            // Arrange
            var monthYear = new MonthYearDto { Month = 6, Year = 2023 };
            _statisticServiceMock.Setup(s => s.GetMonthlyTicketTypeStatisticsAsync(monthYear))
                .ThrowsAsync(new Exception("Network timeout"));

            // Act
            var result = await _controller.GetMonthlyTicketTypeStatisticsAsync(monthYear);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetMonthlyFoodAndDrinkRevenueAsync_ReturnsOk_WhenValidParameters()
        {
            // Arrange
            var monthYear = new MonthYearDto { Month = 6, Year = 2023 };
            var expectedData = new List<MonthlyFoodAndDrinkRevenueDto>
            {
                new() { FoodAndDrinkId = Guid.NewGuid(), FoodAndDrinkName = "Popcorn", TotalRevenue = 200.50m, TotalSold = 25 },
                new() { FoodAndDrinkId = Guid.NewGuid(), FoodAndDrinkName = "Soda", TotalRevenue = 150.75m, TotalSold = 30 }
            };
            _statisticServiceMock.Setup(s => s.GetMonthlyFoodAndDrinkRevenueAsync(monthYear)).ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetMonthlyFoodAndDrinkRevenueAsync(monthYear);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<List<MonthlyFoodAndDrinkRevenueDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal("200", apiResult.Value.Code);
            Assert.Equal("Monthly food and drink revenue statistics retrieved successfully.", apiResult.Value.Message);
            Assert.Equal(expectedData, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetMonthlyFoodAndDrinkRevenueAsync_ReturnsBadRequest_WhenMonthIsInvalid()
        {
            // Arrange
            var monthYear = new MonthYearDto { Month = -1, Year = 2023 }; // Invalid month

            // Act
            var result = await _controller.GetMonthlyFoodAndDrinkRevenueAsync(monthYear);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("400", apiResult.Error.Code);
            Assert.Equal("Month must be between 1 and 12.", apiResult.Error.Message);
        }

        [Fact]
        public async Task GetMonthlyFoodAndDrinkRevenueAsync_ReturnsBadRequest_WhenYearIsInvalid()
        {
            // Arrange
            var monthYear = new MonthYearDto { Month = 6, Year = 3000 }; // Invalid year

            // Act
            var result = await _controller.GetMonthlyFoodAndDrinkRevenueAsync(monthYear);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("400", apiResult.Error.Code);
            Assert.Equal("Year is out of valid range.", apiResult.Error.Message);
        }

        [Fact]
        public async Task GetMonthlyFoodAndDrinkRevenueAsync_ReturnsOk_WhenYearIsMinimumValid()
        {
            // Arrange
            var monthYear = new MonthYearDto { Month = 6, Year = 2000 }; // Minimum valid year
            var expectedData = new List<MonthlyFoodAndDrinkRevenueDto>();
            _statisticServiceMock.Setup(s => s.GetMonthlyFoodAndDrinkRevenueAsync(monthYear)).ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetMonthlyFoodAndDrinkRevenueAsync(monthYear);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<List<MonthlyFoodAndDrinkRevenueDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetMonthlyFoodAndDrinkRevenueAsync_ReturnsStatusCode_OnException()
        {
            // Arrange
            var monthYear = new MonthYearDto { Month = 6, Year = 2023 };
            _statisticServiceMock.Setup(s => s.GetMonthlyFoodAndDrinkRevenueAsync(monthYear))
                .ThrowsAsync(new Exception("Internal server error"));

            // Act
            var result = await _controller.GetMonthlyFoodAndDrinkRevenueAsync(monthYear);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Theory]
        [InlineData(1, 2000)] // Minimum valid values
        [InlineData(12, 2000)] // Maximum month, minimum year
        [InlineData(1, 2023)] // Minimum month, valid year
        [InlineData(12, 2023)] // Maximum valid values
        public async Task GetMonthlyRevenueMovieAsync_ReturnsOk_ForValidBoundaryValues(int month, int year)
        {
            // Arrange
            var monthYear = new MonthYearDto { Month = month, Year = year };
            var expectedData = new List<MonthlyMovieRevenueDto>();
            _statisticServiceMock.Setup(s => s.GetMonthlyRevenueMovieAsync(monthYear)).ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetMonthlyRevenueMovieAsync(monthYear);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult);
        }

        [Theory]
        [InlineData(0, 2023)] // Month too low
        [InlineData(13, 2023)] // Month too high
        [InlineData(6, 1999)] // Year too low
        public async Task GetMonthlyTicketTypeStatisticsAsync_ReturnsBadRequest_ForInvalidBoundaryValues(int month, int year)
        {
            // Arrange
            var monthYear = new MonthYearDto { Month = month, Year = year };

            // Act
            var result = await _controller.GetMonthlyTicketTypeStatisticsAsync(monthYear);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("400", apiResult.Error.Code);
        }

    }
}