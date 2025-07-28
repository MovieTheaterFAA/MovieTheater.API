using MockQueryable;
using MockQueryable.Moq;
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
        private readonly Mock<IGenericRepository<ClaimedPromotion>> _mockClaimedPromotionRepository;
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
            _mockClaimedPromotionRepository = new Mock<IGenericRepository<ClaimedPromotion>>();

            _mockUnitOfWork.Setup(uow => uow.Promotions).Returns(_mockPromotionRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.Events).Returns(_mockEventRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.ClaimedPromotions).Returns(_mockClaimedPromotionRepository.Object);

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

        [Fact]
        public async Task AddPromotionAsync_DbUpdateException_LogsErrorAndThrows()
        {
            // Arrange
            var requestDto = new PromotionRequestDto
            {
                Title = "Test",
                DiscountValue = 0.1m,
                Detail = "Detail",
                EventId = Guid.NewGuid()
            };
            var eventEntity = new Event { Id = requestDto.EventId };
            _mockClaimsService.Setup(cs => cs.GetCurrentUserId).Returns(Guid.NewGuid());
            _mockEventRepository.Setup(repo => repo.GetByIdAsync(requestDto.EventId, It.IsAny<Expression<Func<Event, object>>[]>()))
                .ReturnsAsync(eventEntity);
            _mockPromotionRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<Promotion, bool>>>(), It.IsAny<Expression<Func<Promotion, object>>[]>()))
                .ReturnsAsync((Promotion)null!);
            _mockPromotionRepository.Setup(repo => repo.AddAsync(It.IsAny<Promotion>()))
                .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException());

            // Act & Assert
            await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(() => _promotionService.AddPromotionAsync(requestDto));
            _mockLoggerService.Verify(l => l.Error(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdatePromotionAsync_WithNullUpdateDto_ThrowsArgumentNullException()
        {
            // Arrange
            var promotionId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _promotionService.UpdatePromotionAsync(promotionId, null!));
        }

        [Fact]
        public async Task UpdatePromotionAsync_WithDeletedPromotion_ThrowsException()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var updateDto = new PromotionUpdateDto { Title = "Title" };
            var promotion = new Promotion { Id = promotionId, IsDeleted = true };
            _mockPromotionRepository.Setup(repo => repo.GetByIdAsync(promotionId, It.IsAny<Expression<Func<Promotion, object>>[]>()))
                .ReturnsAsync(promotion);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _promotionService.UpdatePromotionAsync(promotionId, updateDto));
        }

        [Fact]
        public async Task GetPromotionAsync_WithValidId_ReturnsPromotionResponseDto()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var eventId = Guid.NewGuid();

            var promotion = new Promotion
            {
                Id = promotionId,
                Title = "Promo",
                Detail = "Detail",
                DiscountValue = 20,
                EventId = eventId,
                IsDeleted = false
            };

            var claimedPromotion = new ClaimedPromotion
            {
                PromotionId = promotionId,
                IsUsed = true
            };

            _mockPromotionRepository
                .Setup(repo => repo.GetByIdAsync(promotionId, It.IsAny<Expression<Func<Promotion, object>>[]>()))
                .ReturnsAsync(promotion);

            var claimedPromotions = new List<ClaimedPromotion> { claimedPromotion }.AsQueryable().BuildMockDbSet();
            _mockClaimedPromotionRepository
                .Setup(repo => repo.GetQueryable())
                .Returns(claimedPromotions.Object);

            // Act
            var result = await _promotionService.GetPromotionAsync(promotionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(promotionId, result.Id);
            Assert.Equal("Promo", result.Title);
            Assert.Equal("Detail", result.Detail);
            Assert.Equal(20, result.DiscountValue);
            Assert.Equal(eventId, result.EventId);
            Assert.True(result.IsUsed);
        }

        [Fact]
        public async Task GetAllPromotionsAsync_ReturnsPromotionList()
        {
            // Arrange
            var promotionId1 = Guid.NewGuid();
            var promotionId2 = Guid.NewGuid();

            var promotions = new List<Promotion>
            {
                new Promotion { Id = promotionId1, Title = "Promo1", Detail = "Detail1", DiscountValue = 10, EventId = Guid.NewGuid(), IsDeleted = false },
                new Promotion { Id = promotionId2, Title = "Promo2", Detail = "Detail2", DiscountValue = 20, EventId = Guid.NewGuid(), IsDeleted = false }
            };

            var claimedPromotions = new List<ClaimedPromotion>
            {
                new ClaimedPromotion { PromotionId = promotionId1, IsUsed = true }
        // Promotion2 chưa được claim, nên sẽ có IsUsed = false
            };

            // Mock Promotions DbSet
            var mockPromotionDbSet = promotions.AsQueryable().BuildMockDbSet();
            _mockUnitOfWork.Setup(uow => uow.Promotions.GetQueryable()).Returns(mockPromotionDbSet.Object);

            // Mock ClaimedPromotions DbSet
            var mockClaimedDbSet = claimedPromotions.AsQueryable().BuildMockDbSet();
            _mockUnitOfWork.Setup(uow => uow.ClaimedPromotions.GetQueryable()).Returns(mockClaimedDbSet.Object);

            // Act
            var result = await _promotionService.GetAllPromotionsAsync();

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Equal(2, resultList.Count);

            Assert.Equal("Promo1", resultList[0].Title);
            Assert.True(resultList[0].IsUsed); // ClaimedPromotion có IsUsed = true

            Assert.Equal("Promo2", resultList[1].Title);
            Assert.False(resultList[1].IsUsed); // Không có ClaimedPromotion -> false
        }


        [Fact]
        public async Task UseClaimedPromotionAsync_MarksPromotionAsUsed_ReturnsTrue()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var claimedPromotion = new ClaimedPromotion
            {
                PromotionId = promotionId,
                UserId = userId,
                IsUsed = false
            };

            var claimedPromotions = new List<ClaimedPromotion> { claimedPromotion };

            var mockClaimedPromotions = claimedPromotions.AsQueryable().BuildMockDbSet();

            _mockClaimedPromotionRepository.Setup(r => r.GetQueryable())
                .Returns(mockClaimedPromotions.Object);

            _mockUnitOfWork.Setup(u => u.ClaimedPromotions).Returns(_mockClaimedPromotionRepository.Object);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _promotionService.UseClaimedPromotionAsync(promotionId, userId);

            // Assert
            Assert.True(result);
            Assert.True(claimedPromotion.IsUsed);
        }

        [Fact]
        public async Task GetClaimedPromotionsByUserAsync_ReturnsClaimedPromotions()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var promotion1 = new Promotion
            {
                Id = Guid.NewGuid(),
                Title = "Promo 1",
                DiscountValue = 10,
                Detail = "Discount 10%",
                EventId = Guid.NewGuid(),
                IsDeleted = false
            };

            var promotion2 = new Promotion
            {
                Id = Guid.NewGuid(),
                Title = "Promo 2",
                DiscountValue = 20,
                Detail = "Discount 20%",
                EventId = Guid.NewGuid(),
                IsDeleted = false
            };

            var claimedPromotions = new List<ClaimedPromotion>
    
            {
                new ClaimedPromotion { Promotion = promotion1, UserId = userId, IsUsed = false },
                new ClaimedPromotion { Promotion = promotion2, UserId = userId, IsUsed = true }
            };

            var mockQueryable = claimedPromotions.AsQueryable().BuildMockDbSet();

            _mockUnitOfWork.Setup(uow => uow.ClaimedPromotions.GetQueryable())
                .Returns(mockQueryable.Object);

            // Act
            var result = await _promotionService.GetClaimedPromotionsByUserAsync(userId);

            // Assert
            Assert.NotNull(result);
            var list = result.ToList();
            Assert.Equal(2, list.Count);
            Assert.Equal(promotion1.Id, list[0].Id);
            Assert.Equal(promotion2.Id, list[1].Id);
        }

        [Fact]
        public async Task GetUnclaimedPromotionsByUserAsync_ExcludesClaimedPromotions()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockClaimsService.Setup(s => s.GetCurrentUserId).Returns(userId);

            var promo1Id = Guid.NewGuid();
            var promo2Id = Guid.NewGuid();

            var allPromotionsList = new List<Promotion>
            {
                new Promotion { Id = promo1Id, Title = "Promo1", EventId = Guid.NewGuid(), IsDeleted = false },
                new Promotion { Id = promo2Id, Title = "Promo2", EventId = Guid.NewGuid(), IsDeleted = false }
            };

            var claimedPromotionsList = new List<ClaimedPromotion>
            {
                new ClaimedPromotion { PromotionId = promo1Id, UserId = userId }
            };

            // Mock IQueryable with async support
            var mockAllPromotions = allPromotionsList.AsQueryable().BuildMockDbSet();
            var mockClaimedPromotions = claimedPromotionsList.AsQueryable().BuildMockDbSet();

            _mockPromotionRepository.Setup(r => r.GetQueryable()).Returns(mockAllPromotions.Object);
            _mockClaimedPromotionRepository.Setup(r => r.GetQueryable()).Returns(mockClaimedPromotions.Object);

            _mockUnitOfWork.Setup(uow => uow.Promotions).Returns(_mockPromotionRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.ClaimedPromotions).Returns(_mockClaimedPromotionRepository.Object);

            // Act
            var result = await _promotionService.GetUnclaimedPromotionsByUserAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal(promo2Id, result.First().Id);
        }

        [Fact]
        public async Task HasUserClaimedPromotionAsync_ReturnsTrueIfClaimed()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var claimedPromotions = new List<ClaimedPromotion>
            {
                new ClaimedPromotion { PromotionId = promotionId, UserId = userId }
            }.AsQueryable().BuildMock();

            _mockClaimedPromotionRepository.Setup(repo => repo.GetQueryable())
                .Returns(claimedPromotions);

            _mockUnitOfWork.Setup(uow => uow.ClaimedPromotions)
                .Returns(_mockClaimedPromotionRepository.Object);

            // Act
            var result = await _promotionService.HasUserClaimedPromotionAsync(promotionId, userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdatePromotionAsync_WithInvalidEventId_ThrowsException()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var updateDto = new PromotionUpdateDto { EventId = Guid.NewGuid() };
            var promotion = new Promotion { Id = promotionId, IsDeleted = false };

            _mockPromotionRepository.Setup(r => r.GetByIdAsync(promotionId, It.IsAny<Expression<Func<Promotion, object>>[]>()))
                .ReturnsAsync(promotion);
            _mockEventRepository.Setup(r => r.GetByIdAsync(updateDto.EventId.Value, It.IsAny<Expression<Func<Event, object>>[]>()))
                .ReturnsAsync((Event)null!);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _promotionService.UpdatePromotionAsync(promotionId, updateDto));
        }

        [Fact]
        public async Task GetPromotionAsync_WithNullPromotion_ReturnsNull()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            _mockPromotionRepository.Setup(r => r.GetByIdAsync(promotionId, It.IsAny<Expression<Func<Promotion, object>>[]>()))
                .ReturnsAsync((Promotion)null!);

            // Act
            var result = await _promotionService.GetPromotionAsync(promotionId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetPromotionAsync_WithDeletedPromotion_ReturnsNull()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var promotion = new Promotion { Id = promotionId, IsDeleted = true };
            _mockPromotionRepository.Setup(r => r.GetByIdAsync(promotionId, It.IsAny<Expression<Func<Promotion, object>>[]>()))
                .ReturnsAsync(promotion);

            // Act
            var result = await _promotionService.GetPromotionAsync(promotionId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllPromotionsAsync_WithNoPromotions_ReturnsEmptyList()
        {
            // Arrange
            var emptyPromotions = new List<Promotion>().AsQueryable().BuildMockDbSet();
            var emptyClaimed = new List<ClaimedPromotion>().AsQueryable().BuildMockDbSet();

            _mockUnitOfWork.Setup(uow => uow.Promotions.GetQueryable()).Returns(emptyPromotions.Object);
            _mockUnitOfWork.Setup(uow => uow.ClaimedPromotions.GetQueryable()).Returns(emptyClaimed.Object);

            // Act
            var result = await _promotionService.GetAllPromotionsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllPromotionsAsync_FiltersDeletedPromotions()
        {
            // Arrange
            var deletedPromotion = new Promotion { Id = Guid.NewGuid(), Title = "Deleted", IsDeleted = true };
            var activePromotion = new Promotion { Id = Guid.NewGuid(), Title = "Active", IsDeleted = false };
            var promotions = new List<Promotion> { deletedPromotion, activePromotion }.AsQueryable().BuildMockDbSet();
            var claimedPromotions = new List<ClaimedPromotion>().AsQueryable().BuildMockDbSet();

            _mockUnitOfWork.Setup(uow => uow.Promotions.GetQueryable()).Returns(promotions.Object);
            _mockUnitOfWork.Setup(uow => uow.ClaimedPromotions.GetQueryable()).Returns(claimedPromotions.Object);

            // Act
            var result = await _promotionService.GetAllPromotionsAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal("Active", result.First().Title);
        }

        [Fact]
        public async Task ClaimPromotionAsync_WithValidData_ReturnsTrue()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var promotion = new Promotion
            {
                Id = promotionId,
                Title = "Promo",
                EventId = Guid.NewGuid()
            };

            var claimedPromotions = new List<ClaimedPromotion>().AsQueryable();

            // Mock Promotions.GetByIdAsync
            _mockUnitOfWork.Setup(uow => uow.Promotions.GetByIdAsync(
                    promotionId,
                    It.IsAny<Expression<Func<Promotion, object>>[]>()))
                .ReturnsAsync(promotion);

            // Mock ClaimedPromotions.GetQueryable().BuildMockDbSet()
            var mockClaimedPromotionsDbSet = claimedPromotions.BuildMockDbSet();
            _mockUnitOfWork.Setup(uow => uow.ClaimedPromotions.GetQueryable())
                .Returns(mockClaimedPromotionsDbSet.Object);

            // Mock AddAsync
            _mockUnitOfWork.Setup(uow => uow.ClaimedPromotions.AddAsync(It.IsAny<ClaimedPromotion>()))
                .ReturnsAsync((ClaimedPromotion cp) => cp);

            // Mock SaveChangesAsync
            _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _promotionService.ClaimPromotionAsync(promotionId, userId);

            // Assert
            Assert.True(result);
        }


        [Fact]
        public async Task ClaimPromotionAsync_WithDeletedPromotion_ThrowsException()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var promotion = new Promotion { Id = promotionId, IsDeleted = true };

            _mockPromotionRepository.Setup(r => r.GetByIdAsync(promotionId, It.IsAny<Expression<Func<Promotion, object>>[]>()))
                .ReturnsAsync(promotion);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _promotionService.ClaimPromotionAsync(promotionId, userId));
        }

        [Fact]
        public async Task ClaimPromotionAsync_AlreadyClaimed_ReturnsFalse()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var promotion = new Promotion { Id = promotionId, IsDeleted = false };

            var claimedPromotions = new List<ClaimedPromotion>
            {
                new ClaimedPromotion { PromotionId = promotionId, UserId = userId }
            }.AsQueryable();

            var claimedPromotionDbSet = claimedPromotions.BuildMockDbSet();

            _mockPromotionRepository.Setup(r => r.GetByIdAsync(
                promotionId,
                It.IsAny<Expression<Func<Promotion, object>>[]>()))
                .ReturnsAsync(promotion);

            _mockClaimedPromotionRepository.Setup(r => r.GetQueryable())
                .Returns(claimedPromotionDbSet.Object);

            // Act
            var result = await _promotionService.ClaimPromotionAsync(promotionId, userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ClaimPromotionAsync_WithInvalidPromotionId_ThrowsException()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _mockPromotionRepository.Setup(r => r.GetByIdAsync(promotionId, It.IsAny<Expression<Func<Promotion, object>>[]>()))
                .ReturnsAsync((Promotion)null!);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _promotionService.ClaimPromotionAsync(promotionId, userId));
        }

        [Fact]
        public async Task UseClaimedPromotionAsync_WithNoClaimedPromotion_ReturnsFalse()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var claimedPromotions = new List<ClaimedPromotion>().AsQueryable().BuildMockDbSet();

            _mockClaimedPromotionRepository.Setup(r => r.GetQueryable()).Returns(claimedPromotions.Object);

            // Act
            var result = await _promotionService.UseClaimedPromotionAsync(promotionId, userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UseClaimedPromotionAsync_AlreadyUsed_ReturnsFalse()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var claimedPromotion = new ClaimedPromotion { PromotionId = promotionId, UserId = userId, IsUsed = true };
            var claimedPromotions = new List<ClaimedPromotion> { claimedPromotion }.AsQueryable().BuildMockDbSet();

            _mockClaimedPromotionRepository.Setup(r => r.GetQueryable()).Returns(claimedPromotions.Object);

            // Act
            var result = await _promotionService.UseClaimedPromotionAsync(promotionId, userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HasUserClaimedPromotionAsync_ReturnsFalseIfNotClaimed()
        {
            // Arrange
            var promotionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var claimedPromotions = new List<ClaimedPromotion>().AsQueryable().BuildMock();

            _mockClaimedPromotionRepository.Setup(repo => repo.GetQueryable())
                .Returns(claimedPromotions);

            _mockUnitOfWork.Setup(uow => uow.ClaimedPromotions)
                .Returns(_mockClaimedPromotionRepository.Object);

            // Act
            var result = await _promotionService.HasUserClaimedPromotionAsync(promotionId, userId);

            // Assert
            Assert.False(result);
        }
    }
}