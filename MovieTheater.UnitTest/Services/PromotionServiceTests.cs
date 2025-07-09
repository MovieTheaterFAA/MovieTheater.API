using Moq;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.DTOs.PromotionDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System.Linq.Expressions;

namespace MovieTheater.UnitTest.Services
{
    public class PromotionServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILoggerService> _mockLoggerService;
        private readonly Mock<IClaimsService> _mockClaimsService;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<IRedisService> _mockRedisService;
        private readonly Mock<IGenericRepository<Promotion>> _mockPromotionRepository;
        private readonly Mock<IGenericRepository<Event>> _mockEventRepository;
        private readonly IPromotionService _promotionService;

        public PromotionServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLoggerService = new Mock<ILoggerService>();
            _mockClaimsService = new Mock<IClaimsService>();
            _mockAuditLogService = new Mock<IAuditLogService>();
            _mockRedisService = new Mock<IRedisService>();
            _mockPromotionRepository = new Mock<IGenericRepository<Promotion>>();
            _mockEventRepository = new Mock<IGenericRepository<Event>>();

            _mockUnitOfWork.Setup(uow => uow.Promotions).Returns(_mockPromotionRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.Events).Returns(_mockEventRepository.Object);

            _promotionService = new PromotionService(
                _mockUnitOfWork.Object,
                _mockLoggerService.Object,
                _mockClaimsService.Object,
                _mockAuditLogService.Object,
                _mockRedisService.Object);
        }

        [Fact]
        public async Task AddPromotionAsync_WithValidData_ReturnsPromotionResponseDto()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var promotionId = Guid.NewGuid();

            var requestDto = new PromotionRequestDto
            {
                Title = "Test Promotion",
                DiscountValue = 0.2m,
                Detail = "Test promotion detail",
                EventId = eventId
            };

            var eventEntity = new Event { Id = eventId };

            _mockClaimsService.Setup(cs => cs.GetCurrentUserId).Returns(adminId);
            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId, It.IsAny<Expression<Func<Event, object>>[]>()))
                .ReturnsAsync(eventEntity);

            _mockPromotionRepository.Setup(repo => repo.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Promotion, bool>>>(), It.IsAny<Expression<Func<Promotion, object>>[]>()))
                .ReturnsAsync((Promotion)null!);

            _mockPromotionRepository.Setup(repo => repo.AddAsync(It.IsAny<Promotion>()))
                .ReturnsAsync((Promotion p) => { p.Id = promotionId; return p; });

            // Act
            var result = await _promotionService.AddPromotionAsync(requestDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(promotionId, result.Id);
            Assert.Equal(requestDto.Title, result.Title);
            Assert.Equal(requestDto.DiscountValue, result.DiscountValue);
            Assert.Equal(requestDto.Detail, result.Detail);
            Assert.Equal(requestDto.EventId, result.EventId);

            _mockPromotionRepository.Verify(repo => repo.AddAsync(It.Is<Promotion>(p =>
                p.Title == requestDto.Title &&
                p.DiscountValue == requestDto.DiscountValue &&
                p.Detail == requestDto.Detail &&
                p.EventId == requestDto.EventId)), Times.Once);

            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
            _mockRedisService.Verify(redis => redis.RemoveByPatternAsync("event:list:"), Times.Once);

            _mockAuditLogService.Verify(audit => audit.LogAsync(
                adminId,
                AuditActionType.Create,
                "Promotion",
                promotionId,
                null!,
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task AddPromotionAsync_WithExistingTitle_ThrowsInvalidOperationException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var requestDto = new PromotionRequestDto
            {
                Title = "Existing Promotion",
                DiscountValue = 0.2m,
                Detail = "Test promotion detail",
                EventId = eventId
            };

            var existingPromotion = new Promotion
            {
                Id = Guid.NewGuid(),
                Title = "Existing Promotion"
            };

            _mockPromotionRepository.Setup(repo => repo.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Promotion, bool>>>(), It.IsAny<Expression<Func<Promotion, object>>[]>()))
                .ReturnsAsync(existingPromotion);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _promotionService.AddPromotionAsync(requestDto));

            _mockPromotionRepository.Verify(repo => repo.AddAsync(It.IsAny<Promotion>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task AddPromotionAsync_WithInvalidEventId_ThrowsKeyNotFoundException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var requestDto = new PromotionRequestDto
            {
                Title = "Test Promotion",
                DiscountValue = 0.2m,
                Detail = "Test promotion detail",
                EventId = eventId
            };

            _mockPromotionRepository.Setup(repo => repo.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Promotion, bool>>>(), It.IsAny<Expression<Func<Promotion, object>>[]>()))
                .ReturnsAsync((Promotion)null!);

            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId, It.IsAny<Expression<Func<Event, object>>[]>()))
                .ReturnsAsync((Event)null!);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _promotionService.AddPromotionAsync(requestDto));

            _mockPromotionRepository.Verify(repo => repo.AddAsync(It.IsAny<Promotion>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeletePromotionAsync_WithExistingId_ReturnsTrue()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var promotionId = Guid.NewGuid();

            var promotion = new Promotion
            {
                Id = promotionId,
                Title = "Test Promotion",
                IsDeleted = false
            };

            _mockClaimsService.Setup(cs => cs.GetCurrentUserId).Returns(adminId);
            _mockPromotionRepository.Setup(repo => repo.GetByIdAsync(promotionId, It.IsAny<Expression<Func<Promotion, object>>[]>()))
                .ReturnsAsync(promotion);
            _mockPromotionRepository.Setup(repo => repo.SoftRemove(It.IsAny<Promotion>()))
                .ReturnsAsync(true);

            // Act
            var result = await _promotionService.DeletePromotionAsync(promotionId);

            // Assert
            Assert.True(result);

            _mockPromotionRepository.Verify(repo => repo.SoftRemove(It.Is<Promotion>(p => p.Id == promotionId)), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
            _mockRedisService.Verify(redis => redis.RemoveByPatternAsync("event:list:"), Times.Once);

            _mockAuditLogService.Verify(audit => audit.LogAsync(
                adminId,
                AuditActionType.Delete,
                "Promotion",
                promotionId,
                It.IsAny<object>(),
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdatePromotionAsync_WithExistingTitle_ThrowsConflictException()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var updateDto = new PromotionUpdateDto
            {
                Title = "Existing Title"
            };

            var promotion = new Promotion
            {
                Id = promotionId,
                Title = "Original Title",
                IsDeleted = false
            };

            var existingPromotion = new Promotion
            {
                Id = Guid.NewGuid(),
                Title = "Existing Title",
                IsDeleted = false
            };

            _mockPromotionRepository.Setup(repo => repo.GetByIdAsync(promotionId, It.IsAny<Expression<Func<Promotion, object>>[]>()))
                .ReturnsAsync(promotion);

            _mockPromotionRepository.Setup(repo => repo.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Promotion, bool>>>(), It.IsAny<Expression<Func<Promotion, object>>[]>()))
                .ReturnsAsync(existingPromotion);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _promotionService.UpdatePromotionAsync(promotionId, updateDto));

            _mockPromotionRepository.Verify(repo => repo.Update(It.IsAny<Promotion>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeletePromotionAsync_WithNonExistingId_ReturnsFalse()
        {
            // Arrange
            var promotionId = Guid.NewGuid();

            _mockPromotionRepository.Setup(repo => repo.GetByIdAsync(promotionId, It.IsAny<Expression<Func<Promotion, object>>[]>()))
                .ReturnsAsync((Promotion)null!);

            // Act
            var result = await _promotionService.DeletePromotionAsync(promotionId);

            // Assert
            Assert.False(result);

            _mockPromotionRepository.Verify(repo => repo.SoftRemove(It.IsAny<Promotion>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdatePromotionAsync_WithValidData_ReturnsUpdatedPromotion()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var promotionId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var newEventId = Guid.NewGuid();

            var promotion = new Promotion
            {
                Id = promotionId,
                Title = "Original Title",
                DiscountValue = 0.1m,
                Detail = "Original Detail",
                EventId = eventId,
                IsDeleted = false
            };

            var updateDto = new PromotionUpdateDto
            {
                Title = "Updated Title",
                DiscountValue = 0.2m,
                Detail = "Updated Detail",
                EventId = newEventId
            };

            var eventEntity = new Event { Id = newEventId };

            _mockClaimsService.Setup(cs => cs.GetCurrentUserId).Returns(adminId);
            _mockPromotionRepository.Setup(repo => repo.GetByIdAsync(promotionId, It.IsAny<Expression<Func<Promotion, object>>[]>()))
                .ReturnsAsync(promotion);
            _mockEventRepository.Setup(repo => repo.GetByIdAsync(newEventId, It.IsAny<Expression<Func<Event, object>>[]>()))
                .ReturnsAsync(eventEntity);
            _mockPromotionRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<Promotion, bool>>>(), It.IsAny<Expression<Func<Promotion, object>>[]>()))
                .ReturnsAsync((Promotion)null!);
            _mockPromotionRepository.Setup(repo => repo.Update(It.IsAny<Promotion>()))
                .ReturnsAsync(true);

            // Act
            var result = await _promotionService.UpdatePromotionAsync(promotionId, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(promotionId, result.Id);
            Assert.Equal(updateDto.Title, result.Title);
            Assert.Equal(updateDto.DiscountValue, result.DiscountValue);
            Assert.Equal(updateDto.Detail, result.Detail);
            Assert.Equal(updateDto.EventId, result.EventId);

            _mockPromotionRepository.Verify(repo => repo.Update(It.Is<Promotion>(p =>
                p.Id == promotionId &&
                p.Title == updateDto.Title &&
                p.DiscountValue == updateDto.DiscountValue.Value &&
                p.Detail == updateDto.Detail &&
                p.EventId == updateDto.EventId.Value)), Times.Once);

            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
            _mockRedisService.Verify(redis => redis.RemoveByPatternAsync("event:list:"), Times.Once);

            _mockAuditLogService.Verify(audit => audit.LogAsync(
                adminId,
                AuditActionType.Update,
                "Promotion",
                promotionId,
                It.IsAny<object>(),
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdatePromotionAsync_WithInvalidPromotionId_ThrowsException()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var updateDto = new PromotionUpdateDto
            {
                Title = "Updated Title"
            };

            _mockPromotionRepository.Setup(repo => repo.GetByIdAsync(promotionId, It.IsAny<Expression<Func<Promotion, object>>[]>()))
                .ReturnsAsync((Promotion)null!);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _promotionService.UpdatePromotionAsync(promotionId, updateDto));

            _mockPromotionRepository.Verify(repo => repo.Update(It.IsAny<Promotion>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Never);
        }
    }
}