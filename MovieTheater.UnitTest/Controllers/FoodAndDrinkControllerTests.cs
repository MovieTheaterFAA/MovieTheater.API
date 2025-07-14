using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.API.Controllers;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.FoodAndDrinkDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.UnitTest.Controllers
{
    public class FoodAndDrinkControllerTests
    {
        private readonly Mock<IFoodAndDrinkService> _mockFoodAndDrinkService;
        private readonly Mock<IClaimsService> _mockClaimsService;
        private readonly Mock<ILoggerService> _mockLoggerService;
        private readonly FoodAndDrinkController _controller;

        public FoodAndDrinkControllerTests()
        {
            _mockFoodAndDrinkService = new Mock<IFoodAndDrinkService>();
            _mockClaimsService = new Mock<IClaimsService>();
            _mockLoggerService = new Mock<ILoggerService>();
            _controller = new FoodAndDrinkController(
                _mockFoodAndDrinkService.Object,
                _mockClaimsService.Object,
                _mockLoggerService.Object
            );
        }

        [Fact]
        public async Task GetAllFoodAndDrinksAsync_ValidParameters_ReturnsOkResult()
        {
            // Arrange
            var foodList = new Pagination<FoodAndDrinkResponseDto> { Items = new List<FoodAndDrinkResponseDto>() };
            _mockFoodAndDrinkService.Setup(s => s.GetAllFoodAndDrinkAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<FoodType?>()))
                .ReturnsAsync(foodList);

            // Act
            var result = await _controller.GetAllFoodAndDrinksAsync("pizza", "Name", true, 1, 10, FoodType.Food);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<Pagination<FoodAndDrinkResponseDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(foodList, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetAllFoodAndDrinksAsync_InvalidPagination_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetAllFoodAndDrinksAsync(null, null, false, 0, 0, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetAllFoodAndDrinksAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            _mockFoodAndDrinkService.Setup(s => s.GetAllFoodAndDrinkAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<FoodType?>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetAllFoodAndDrinksAsync("pizza", "Name", false, 1, 10, FoodType.Food);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Theory]
        [InlineData("Name", false, FoodType.Food)]
        [InlineData("Price", true, FoodType.Drink)]
        [InlineData("", false, FoodType.Combo)]
        public async Task GetAllFoodAndDrinksAsync_DifferentFilters_ReturnsOkResult(string sortBy, bool isDescending, FoodType type)
        {
            // Arrange
            var foodList = new Pagination<FoodAndDrinkResponseDto> { Items = new List<FoodAndDrinkResponseDto>() };
            _mockFoodAndDrinkService.Setup(s => s.GetAllFoodAndDrinkAsync(
                    It.IsAny<string>(), sortBy, isDescending, It.IsAny<int>(), It.IsAny<int>(), type))
                .ReturnsAsync(foodList);

            // Act
            var result = await _controller.GetAllFoodAndDrinksAsync(null, sortBy, isDescending, 1, 10, type);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<ApiResult<Pagination<FoodAndDrinkResponseDto>>>(okResult.Value);
        }

        [Fact]
        public async Task UpdateFoodAndDrinkAsync_ValidData_ReturnsOkResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var updateDto = new FoodAndDrinkRequestDto
            {
                Name = "Updated Popcorn",
                Price = 5.99m,
                Type = FoodType.Food
            };
            var responseDto = new FoodAndDrinkResponseDto
            {
                Id = id,
                Name = "Updated Popcorn",
                Price = 5.99m,
                Type = FoodType.Food
            };

            _mockFoodAndDrinkService.Setup(s => s.UpdateFoodAndDrinkAsync(id, updateDto))
                .ReturnsAsync(responseDto);

            // Act
            var result = await _controller.UpdateFoodAndDrinkAsync(id, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<FoodAndDrinkResponseDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(responseDto, apiResult.Value.Data);
        }

        [Fact]
        public async Task UpdateFoodAndDrinkAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var id = Guid.NewGuid();
            var updateDto = new FoodAndDrinkRequestDto { Name = "Updated Popcorn" };
            _mockFoodAndDrinkService.Setup(s => s.UpdateFoodAndDrinkAsync(id, updateDto))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.UpdateFoodAndDrinkAsync(id, updateDto);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task AddFoodAndDrinkAsync_ValidData_ReturnsOkResult()
        {
            // Arrange
            var createDto = new FoodAndDrinkRequestDto
            {
                Name = "New Popcorn",
                Price = 4.99m,
                Type = FoodType.Food
            };
            var responseDto = new FoodAndDrinkResponseDto
            {
                Id = Guid.NewGuid(),
                Name = "New Popcorn",
                Price = 4.99m,
                Type = FoodType.Food
            };

            _mockFoodAndDrinkService.Setup(s => s.AddFoodAndDrinkAsync(createDto))
                .ReturnsAsync(responseDto);

            // Act
            var result = await _controller.AddFoodAndDrinkAsync(createDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<FoodAndDrinkResponseDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(responseDto, apiResult.Value.Data);
        }

        [Fact]
        public async Task AddFoodAndDrinkAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var createDto = new FoodAndDrinkRequestDto { Name = "New Popcorn" };
            _mockFoodAndDrinkService.Setup(s => s.AddFoodAndDrinkAsync(createDto))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.AddFoodAndDrinkAsync(createDto);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<FoodAndDrinkResponseDto>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task DeleteFoodAndDrink_ValidId_ReturnsOkResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockFoodAndDrinkService.Setup(s => s.DeleteFoodAndDrinkAsync(id))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteFoodAndDrink(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<bool>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.True(apiResult.Value.Data);
        }

        [Fact]
        public async Task DeleteFoodAndDrink_DeleteFails_ReturnsSuccessWithFalse()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockFoodAndDrinkService.Setup(s => s.DeleteFoodAndDrinkAsync(id))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteFoodAndDrink(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<bool>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);  // The API call itself succeeds
            Assert.False(apiResult.Value.Data); // But the deletion operation failed
        }

        [Fact]
        public async Task DeleteFoodAndDrink_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockFoodAndDrinkService.Setup(s => s.DeleteFoodAndDrinkAsync(id))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.DeleteFoodAndDrink(id);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }
    }
}