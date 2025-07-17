using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.API.Controllers;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.PromotionDTOs;
using MovieTheater.Infrastructure.Interfaces;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MovieTheater.UnitTest.Controllers
{
    public class PromotionControllerTests
    {
        private readonly Mock<IPromotionService> _mockPromotionService;
        private readonly Mock<IClaimsService> _mockClaimsService;
        private readonly PromotionController _controller;

        public PromotionControllerTests()
        {
            _mockPromotionService = new Mock<IPromotionService>();
            _mockClaimsService = new Mock<IClaimsService>();
            _controller = new PromotionController(
                _mockPromotionService.Object,
                _mockClaimsService.Object
            );
        }

        [Fact]
        public async Task AddPromotionAsync_ValidData_ReturnsOkResult()
        {
            // Arrange
            var promotionDto = new PromotionRequestDto
            {
                Title = "Test Promotion",
                DiscountValue = 10.5m,
                Detail = "Promotion detail",
                EventId = Guid.NewGuid()
            };

            var responseDto = new PromotionResponseDto
            {
                Id = Guid.NewGuid(),
                Title = promotionDto.Title,
                DiscountValue = promotionDto.DiscountValue,
                Detail = promotionDto.Detail,
                EventId = promotionDto.EventId
            };

            _mockPromotionService.Setup(s => s.AddPromotionAsync(promotionDto))
                .ReturnsAsync(responseDto);

            // Act
            var result = await _controller.AddPromotionAsync(promotionDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<PromotionResponseDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(responseDto, apiResult.Value.Data);
            Assert.Equal("Added promotion successfully.", apiResult.Value.Message);
        }

        [Fact]
        public async Task AddPromotionAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var promotionDto = new PromotionRequestDto
            {
                Title = "Test Promotion",
                DiscountValue = 10.5m,
                Detail = "Promotion detail",
                EventId = Guid.NewGuid()
            };

            _mockPromotionService.Setup(s => s.AddPromotionAsync(promotionDto))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.AddPromotionAsync(promotionDto);

            // Assert
            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task UpdatePromotionAsync_ValidData_ReturnsOkResult()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var updateDto = new PromotionUpdateDto
            {
                Title = "Updated Promotion",
                DiscountValue = 15.5m,
                Detail = "Updated detail",
                EventId = Guid.NewGuid()
            };

            var responseDto = new PromotionResponseDto
            {
                Id = promotionId,
                Title = updateDto.Title,
                DiscountValue = updateDto.DiscountValue.Value,
                Detail = updateDto.Detail,
                EventId = updateDto.EventId.Value
            };

            _mockPromotionService.Setup(s => s.UpdatePromotionAsync(promotionId, updateDto))
                .ReturnsAsync(responseDto);

            // Act
            var result = await _controller.UpdatePromotionAsync(promotionId, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<PromotionResponseDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(responseDto, apiResult.Value.Data);
            Assert.Equal("Updated promotion successfully.", apiResult.Value.Message);
        }

        [Fact]
        public async Task UpdatePromotionAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var updateDto = new PromotionUpdateDto { Title = "Updated Promotion" };

            _mockPromotionService.Setup(s => s.UpdatePromotionAsync(promotionId, updateDto))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.UpdatePromotionAsync(promotionId, updateDto);

            // Assert
            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task DeletePromotion_Success_ReturnsOkResult()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            _mockPromotionService.Setup(s => s.DeletePromotionAsync(promotionId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeletePromotion(promotionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<bool>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.True((bool)apiResult.Value.Data);
            Assert.Equal("Promotion deleted successfully.", apiResult.Value.Message);
        }

        [Fact]
        public async Task DeletePromotion_Failure_ReturnsBadRequest()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            _mockPromotionService.Setup(s => s.DeletePromotionAsync(promotionId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeletePromotion(promotionId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("Promotion not found or could not be deleted.", apiResult.Error.Message);
        }

        [Fact]
        public async Task DeletePromotion_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            _mockPromotionService.Setup(s => s.DeletePromotionAsync(promotionId))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.DeletePromotion(promotionId);

            // Assert
            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task ClaimPromotion_Success_ReturnsOkResult()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _mockClaimsService.Setup(s => s.GetCurrentUserId)
                .Returns(userId);

            _mockPromotionService.Setup(s => s.ClaimPromotionAsync(promotionId, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ClaimPromotion(promotionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<bool>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.True((bool)apiResult.Value.Data);
            Assert.Equal("Promotion claimed successfully.", apiResult.Value.Message);
        }

        [Fact]
        public async Task ClaimPromotion_AlreadyClaimed_ReturnsBadRequest()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _mockClaimsService.Setup(s => s.GetCurrentUserId)
                .Returns(userId);

            _mockPromotionService.Setup(s => s.ClaimPromotionAsync(promotionId, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ClaimPromotion(promotionId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("Promotion already claimed or not found.", apiResult.Error.Message);
        }

        [Fact]
        public async Task ClaimPromotion_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _mockClaimsService.Setup(s => s.GetCurrentUserId)
                .Returns(userId);

            _mockPromotionService.Setup(s => s.ClaimPromotionAsync(promotionId, userId))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.ClaimPromotion(promotionId);

            // Assert
            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task UseClaimedPromotion_Success_ReturnsOkResult()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _mockClaimsService.Setup(s => s.GetCurrentUserId)
                .Returns(userId);

            _mockPromotionService.Setup(s => s.UseClaimedPromotionAsync(promotionId, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UseClaimedPromotion(promotionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<bool>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.True((bool)apiResult.Value.Data);
            Assert.Equal("Promotion used successfully.", apiResult.Value.Message);
        }

        [Fact]
        public async Task UseClaimedPromotion_NotClaimedOrAlreadyUsed_ReturnsBadRequest()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _mockClaimsService.Setup(s => s.GetCurrentUserId)
                .Returns(userId);

            _mockPromotionService.Setup(s => s.UseClaimedPromotionAsync(promotionId, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UseClaimedPromotion(promotionId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("Promotion not claimed or already used.", apiResult.Error.Message);
        }

        [Fact]
        public async Task UseClaimedPromotion_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _mockClaimsService.Setup(s => s.GetCurrentUserId)
                .Returns(userId);

            _mockPromotionService.Setup(s => s.UseClaimedPromotionAsync(promotionId, userId))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.UseClaimedPromotion(promotionId);

            // Assert
            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task GetClaimedPromotionsByUser_Success_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var promotions = new List<PromotionResponseDto>
    {
        new PromotionResponseDto
        {
            Id = Guid.NewGuid(),
            Title = "Promotion 1",
            DiscountValue = 0.1m,
            Detail = "Detail 1",
            EventId = Guid.NewGuid(),
            IsUsed = false
        },
        new PromotionResponseDto
        {
            Id = Guid.NewGuid(),
            Title = "Promotion 2",
            DiscountValue = 0.2m,
            Detail = "Detail 2",
            EventId = Guid.NewGuid(),
            IsUsed = true
        }
    };

            _mockClaimsService.Setup(s => s.GetCurrentUserId)
                .Returns(userId);

            _mockPromotionService.Setup(s => s.GetClaimedPromotionsByUserAsync(userId))
                .ReturnsAsync(promotions);

            // Act
            var result = await _controller.GetClaimedPromotionsByUser();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<IEnumerable<PromotionResponseDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            var resultList = apiResult.Value.Data.ToList();
            Assert.Equal(2, resultList.Count());
            Assert.Equal("Promotion 1", resultList[0].Title);
            Assert.Equal("Promotion 2", resultList[1].Title);
            Assert.Equal("Claimed promotions retrieved successfully.", apiResult.Value.Message);
        }

        [Fact]
        public async Task GetClaimedPromotionsByUser_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockClaimsService.Setup(s => s.GetCurrentUserId)
                .Returns(userId);

            _mockPromotionService.Setup(s => s.GetClaimedPromotionsByUserAsync(userId))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetClaimedPromotionsByUser();

            // Assert
            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task GetPromotionAsync_ValidId_ReturnsOkResult()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var promotion = new PromotionResponseDto
            {
                Id = promotionId,
                Title = "Test Promotion",
                DiscountValue = 0.1m,
                Detail = "Test detail",
                EventId = Guid.NewGuid(),
                IsUsed = false
            };
            _mockPromotionService.Setup(s => s.GetPromotionAsync(promotionId))
                .ReturnsAsync(promotion);

            // Act
            var result = await _controller.GetPromotionAsync(promotionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<PromotionResponseDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(promotion, apiResult.Value.Data);
            Assert.Equal("Promotion retrieved successfully.", apiResult.Value.Message);
        }

        [Fact]
        public async Task GetPromotionAsync_NotFound_ReturnsNotFound()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            _mockPromotionService.Setup(s => s.GetPromotionAsync(promotionId))
                .ReturnsAsync((PromotionResponseDto)null!);

            // Act
            var result = await _controller.GetPromotionAsync(promotionId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(notFoundResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("Promotion not found.", apiResult.Error.Message);
        }

        [Fact]
        public async Task GetPromotionAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            _mockPromotionService.Setup(s => s.GetPromotionAsync(promotionId))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetPromotionAsync(promotionId);

            // Assert
            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task GetAllPromotionsAsync_Success_ReturnsOkResult()
        {
            // Arrange
            var promotions = new List<PromotionResponseDto>
            {
                new PromotionResponseDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Promotion 1",
                    DiscountValue = 0.1m,
                    Detail = "Detail 1",
                    EventId = Guid.NewGuid(),
                    IsUsed = false
                },
                new PromotionResponseDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Promotion 2",
                    DiscountValue = 0.2m,
                    Detail = "Detail 2",
                    EventId = Guid.NewGuid(),
                    IsUsed = true
                }
            };
            _mockPromotionService.Setup(s => s.GetAllPromotionsAsync())
                .ReturnsAsync(promotions);

            // Act
            var result = await _controller.GetAllPromotionsAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<IEnumerable<PromotionResponseDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(2, apiResult.Value.Data.Count());
            Assert.Equal("Promotions retrieved successfully.", apiResult.Value.Message);
        }

        [Fact]
        public async Task GetAllPromotionsAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            _mockPromotionService.Setup(s => s.GetAllPromotionsAsync())
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetAllPromotionsAsync();

            // Assert
            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task AdminClaimPromotionForMember_Success_ReturnsOkResult()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _mockPromotionService.Setup(s => s.ClaimPromotionAsync(promotionId, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.AdminClaimPromotionForMember(promotionId, userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<bool>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.True((bool)apiResult.Value.Data);
            Assert.Equal("Promotion claimed for member successfully.", apiResult.Value.Message);
        }

        [Fact]
        public async Task AdminClaimPromotionForMember_AlreadyClaimedOrNotFound_ReturnsBadRequest()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _mockPromotionService.Setup(s => s.ClaimPromotionAsync(promotionId, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.AdminClaimPromotionForMember(promotionId, userId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("Promotion already claimed or not found for this user.", apiResult.Error.Message);
        }

        [Fact]
        public async Task AdminClaimPromotionForMember_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _mockPromotionService.Setup(s => s.ClaimPromotionAsync(promotionId, userId))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.AdminClaimPromotionForMember(promotionId, userId);

            // Assert
            Assert.IsType<ObjectResult>(result);
        }
    }
}