using Microsoft.EntityFrameworkCore;
using MockQueryable;
using Moq;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.DTOs.FoodAndDrinkDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System.Linq.Expressions;

namespace MovieTheater.UnitTest.Services
{
    public class FoodAndDrinkServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILoggerService> _mockLoggerService;
        private readonly Mock<IClaimsService> _mockClaimsService;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<IRedisService> _mockRedisService;
        private readonly Mock<IGenericRepository<FoodAndDrink>> _mockFoodAndDrinkRepository;
        private readonly FoodAndDrinkService _foodAndDrinkService;
        private readonly Guid _currentAdminId = Guid.NewGuid();

        public FoodAndDrinkServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLoggerService = new Mock<ILoggerService>();
            _mockClaimsService = new Mock<IClaimsService>();
            _mockAuditLogService = new Mock<IAuditLogService>();
            _mockRedisService = new Mock<IRedisService>();
            _mockFoodAndDrinkRepository = new Mock<IGenericRepository<FoodAndDrink>>();

            // Setup UnitOfWork to return repository
            _mockUnitOfWork.Setup(uow => uow.FoodAndDrinks).Returns(_mockFoodAndDrinkRepository.Object);

            // Setup ClaimsService to return admin id
            _mockClaimsService.Setup(s => s.GetCurrentUserId).Returns(_currentAdminId);

            _foodAndDrinkService = new FoodAndDrinkService(
                _mockUnitOfWork.Object,
                _mockLoggerService.Object,
                _mockClaimsService.Object,
                _mockAuditLogService.Object,
                _mockRedisService.Object
            );
        }

        [Fact]
        public async Task GetAllFoodAndDrinkAsync_ReturnsCachedResult_WhenCacheExists()
        {
            // Arrange
            var search = "test";
            var sortBy = "price";
            var isDescending = true;
            var page = 1;
            var pageSize = 10;
            var type = FoodType.Food;

            var cachedResult = new Pagination<FoodAndDrinkResponseDto>(
                new List<FoodAndDrinkResponseDto>
                {
                    new FoodAndDrinkResponseDto { Id = Guid.NewGuid(), Name = "Test Food" }
                },
                1, page, pageSize);

            var cacheKey = $"fooddrink:list:{search}:{sortBy}:{isDescending}:{page}:{pageSize}:{type}";

            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<FoodAndDrinkResponseDto>>(cacheKey))
                .ReturnsAsync(cachedResult);

            // Act
            var result = await _foodAndDrinkService.GetAllFoodAndDrinkAsync(search, sortBy, isDescending, page, pageSize, type);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(cachedResult.Items.First().Name, result.Items.First().Name);

            _mockRedisService.Verify(redis => redis.GetAsync<Pagination<FoodAndDrinkResponseDto>>(cacheKey), Times.Once);
            _mockFoodAndDrinkRepository.Verify(repo => repo.GetAllAsync(It.IsAny<Expression<Func<FoodAndDrink, bool>>>()), Times.Never);
        }

        [Fact]
        public async Task GetAllFoodAndDrinkAsync_ReturnsFromDatabase_WhenCacheDoesNotExist()
        {
            // Arrange
            var foodItems = new List<FoodAndDrink>
    {
        new FoodAndDrink
        {
            Id = Guid.NewGuid(),
            Name = "Popcorn",
            Price = 5.99m,
            Type = FoodType.Food,
            IsAvailable = true,
            IsDeleted = false
        },
        new FoodAndDrink
        {
            Id = Guid.NewGuid(),
            Name = "Cola",
            Price = 3.99m,
            Type = FoodType.Drink,
            IsAvailable = true,
            IsDeleted = false
        }
    };

            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<FoodAndDrinkResponseDto>>(It.IsAny<string>()))
                .ReturnsAsync((Pagination<FoodAndDrinkResponseDto>)null!);

            // Create a mock IQueryable that supports async operations
            var mockQueryable = foodItems.AsQueryable().BuildMock();

            _mockFoodAndDrinkRepository.Setup(repo => repo.GetQueryable())
                .Returns(mockQueryable);

            // Act
            var result = await _foodAndDrinkService.GetAllFoodAndDrinkAsync(null, null, false, 1, 10, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Items.Count);
            _mockRedisService.Verify(redis => redis.SetAsync(
                It.IsAny<string>(), It.IsAny<Pagination<FoodAndDrinkResponseDto>>(), It.IsAny<TimeSpan>()), Times.Once);
        }

        [Theory]
        [InlineData("popcorn", "name", true, FoodType.Food)]
        [InlineData("cola", "price", false, FoodType.Drink)]
        [InlineData("combo", null, false, FoodType.Combo)]
        public async Task GetAllFoodAndDrinkAsync_WithDifferentFilters_ReturnsFilteredResults(
    string search, string? sortBy, bool isDescending, FoodType type)
        {
            // Arrange
            var foodItems = new List<FoodAndDrink>
    {
        new FoodAndDrink { Id = Guid.NewGuid(), Name = "Popcorn", Type = FoodType.Food, IsDeleted = false, Price = 5.99m },
        new FoodAndDrink { Id = Guid.NewGuid(), Name = "Cola", Type = FoodType.Drink, IsDeleted = false, Price = 3.99m },
        new FoodAndDrink { Id = Guid.NewGuid(), Name = "Combo Meal", Type = FoodType.Combo, IsDeleted = false, Price = 9.99m }
    };

            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<FoodAndDrinkResponseDto>>(It.IsAny<string>()))
                .ReturnsAsync((Pagination<FoodAndDrinkResponseDto>)null!);

            // Create a mock IQueryable that supports async operations with all items
            var mockQueryable = foodItems.AsQueryable().BuildMock();

            _mockFoodAndDrinkRepository.Setup(repo => repo.GetQueryable())
                .Returns(mockQueryable);

            // Act
            var result = await _foodAndDrinkService.GetAllFoodAndDrinkAsync(search, sortBy, isDescending, 1, 10, type);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Items.Count > 0);
            Assert.All(result.Items, item => Assert.Equal(type, item.Type));
        }

        [Fact]
        public async Task GetAllFoodAndDrinkAsync_WithNoItems_ReturnsEmptyPagination()
        {
            // Arrange
            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<FoodAndDrinkResponseDto>>(It.IsAny<string>()))
                .ReturnsAsync((Pagination<FoodAndDrinkResponseDto>)null!);

            // Create an empty list and mock queryable
            var emptyFoodItems = new List<FoodAndDrink>();
            var mockQueryable = emptyFoodItems.AsQueryable().BuildMock();

            _mockFoodAndDrinkRepository.Setup(repo => repo.GetQueryable())
                .Returns(mockQueryable);

            // Act
            var result = await _foodAndDrinkService.GetAllFoodAndDrinkAsync(null, null, false, 1, 10, null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task GetAllFoodAndDrinkAsync_WhenExceptionOccurs_ThrowsException()
        {
            // Arrange
            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<FoodAndDrinkResponseDto>>(It.IsAny<string>()))
                .ReturnsAsync((Pagination<FoodAndDrinkResponseDto>)null!);

            _mockFoodAndDrinkRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<FoodAndDrink, bool>>>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _foodAndDrinkService.GetAllFoodAndDrinkAsync(null, null, false, 1, 10, null));

            Assert.Contains("An error occurred while retrieving food and drink items", ex.Message);
        }

        [Fact]
        public async Task AddFoodAndDrinkAsync_WithValidData_ReturnsAddedItem()
        {
            // Arrange
            var dto = new FoodAndDrinkRequestDto
            {
                Name = "New Popcorn",
                Description = "Delicious popcorn",
                Price = 6.99m,
                Type = FoodType.Food,
                ImageUrl = "http://example.com/popcorn.jpg",
                IsAvailable = true
            };

            _mockFoodAndDrinkRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<FoodAndDrink, bool>>>()))
                .ReturnsAsync((FoodAndDrink)null!);

            _mockFoodAndDrinkRepository.Setup(repo => repo.AddAsync(It.IsAny<FoodAndDrink>()))
                .ReturnsAsync((FoodAndDrink f) => f);

            _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _foodAndDrinkService.AddFoodAndDrinkAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Name, result.Name);
            Assert.Equal(dto.Description, result.Description);
            Assert.Equal(dto.Price, result.Price);
            Assert.Equal(dto.Type, result.Type);

            _mockFoodAndDrinkRepository.Verify(repo => repo.AddAsync(It.Is<FoodAndDrink>(f =>
                f.Name == dto.Name &&
                f.Description == dto.Description &&
                f.Price == dto.Price &&
                f.Type == dto.Type)), Times.Once);

            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
            _mockAuditLogService.Verify(log => log.LogAsync(
                It.IsAny<Guid>(), AuditActionType.Create, "FoodAndDrink", It.IsAny<Guid>(),
                null!, It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task AddFoodAndDrinkAsync_WithExistingName_ThrowsException()
        {
            // Arrange
            var dto = new FoodAndDrinkRequestDto
            {
                Name = "Existing Popcorn",
                Price = 6.99m,
                Type = FoodType.Food
            };

            var existingItem = new FoodAndDrink
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                IsDeleted = false
            };

            _mockFoodAndDrinkRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<FoodAndDrink, bool>>>()))
                .ReturnsAsync(existingItem);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _foodAndDrinkService.AddFoodAndDrinkAsync(dto));
            Assert.Contains("Food or drink with the same name already exists", ex.Message);

            _mockFoodAndDrinkRepository.Verify(repo => repo.AddAsync(It.IsAny<FoodAndDrink>()), Times.Never);
        }

        [Fact]
        public async Task AddFoodAndDrinkAsync_WhenDbUpdateException_ThrowsException()
        {
            // Arrange
            var dto = new FoodAndDrinkRequestDto
            {
                Name = "Test Food",
                Price = 5.99m,
                Type = FoodType.Food
            };

            _mockFoodAndDrinkRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<FoodAndDrink, bool>>>()))
                .ReturnsAsync((FoodAndDrink)null!);

            _mockFoodAndDrinkRepository.Setup(repo => repo.AddAsync(It.IsAny<FoodAndDrink>()))
                .ReturnsAsync(new FoodAndDrink { Name = dto.Name });

            _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync())
                .ThrowsAsync(new DbUpdateException("DB error"));

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() => _foodAndDrinkService.AddFoodAndDrinkAsync(dto));
        }

        [Fact]
        public async Task UpdateFoodAndDrinkAsync_WithValidData_ReturnsUpdatedItem()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new FoodAndDrinkRequestDto
            {
                Name = "Updated Popcorn",
                Description = "Updated description",
                Price = 7.99m,
                Type = FoodType.Food,
                ImageUrl = "http://example.com/updated.jpg",
                IsAvailable = true
            };

            var existingItem = new FoodAndDrink
            {
                Id = id,
                Name = "Original Popcorn",
                Description = "Original description",
                Price = 5.99m,
                Type = FoodType.Food,
                ImageUrl = "http://example.com/original.jpg",
                IsAvailable = true,
                IsDeleted = false
            };

            _mockFoodAndDrinkRepository.Setup(repo => repo.GetByIdAsync(id))
                .ReturnsAsync(existingItem);

            _mockFoodAndDrinkRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<FoodAndDrink, bool>>>()))
                .ReturnsAsync((FoodAndDrink)null!);

            // Act
            var result = await _foodAndDrinkService.UpdateFoodAndDrinkAsync(id, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Name, result.Name);
            Assert.Equal(dto.Description, result.Description);
            Assert.Equal(dto.Price, result.Price);
            Assert.Equal(dto.Type, result.Type);

            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
            _mockAuditLogService.Verify(log => log.LogAsync(
                It.IsAny<Guid>(), AuditActionType.Update, "FoodAndDrink", id,
                It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateFoodAndDrinkAsync_WithNoChanges_DoesNotUpdateDatabase()
        {
            // Arrange
            var id = Guid.NewGuid();
            var name = "Original Popcorn";
            var description = "Original description";
            var price = 5.99m;
            var type = FoodType.Food;

            var dto = new FoodAndDrinkRequestDto
            {
                Name = name,
                Description = description,
                Price = price,
                Type = type,
                IsAvailable = true
            };

            var existingItem = new FoodAndDrink
            {
                Id = id,
                Name = name,
                Description = description,
                Price = price,
                Type = type,
                IsAvailable = true,
                IsDeleted = false
            };

            _mockFoodAndDrinkRepository.Setup(repo => repo.GetByIdAsync(id))
                .ReturnsAsync(existingItem);

            // Act
            var result = await _foodAndDrinkService.UpdateFoodAndDrinkAsync(id, dto);

            // Assert
            Assert.NotNull(result);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateFoodAndDrinkAsync_WithNonExistentId_ThrowsException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new FoodAndDrinkRequestDto
            {
                Name = "Updated Food",
                Price = 5.99m,
                Type = FoodType.Food
            };

            _mockFoodAndDrinkRepository.Setup(repo => repo.GetByIdAsync(id))
                .ReturnsAsync((FoodAndDrink)null!);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _foodAndDrinkService.UpdateFoodAndDrinkAsync(id, dto));
            Assert.Contains("Food or drink not found", ex.Message);
        }

        [Fact]
        public async Task UpdateFoodAndDrinkAsync_WithDeletedItem_ThrowsException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new FoodAndDrinkRequestDto
            {
                Name = "Updated Food",
                Price = 5.99m,
                Type = FoodType.Food
            };

            var existingItem = new FoodAndDrink
            {
                Id = id,
                Name = "Original Food",
                IsDeleted = true
            };

            _mockFoodAndDrinkRepository.Setup(repo => repo.GetByIdAsync(id))
                .ReturnsAsync(existingItem);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _foodAndDrinkService.UpdateFoodAndDrinkAsync(id, dto));
            Assert.Contains("Food or drink not found", ex.Message);
        }

        [Fact]
        public async Task UpdateFoodAndDrinkAsync_WithExistingName_ThrowsException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new FoodAndDrinkRequestDto
            {
                Name = "Existing Food",
                Price = 5.99m,
                Type = FoodType.Food
            };

            var existingItem = new FoodAndDrink
            {
                Id = id,
                Name = "Original Food",
                IsDeleted = false
            };

            var conflictItem = new FoodAndDrink
            {
                Id = Guid.NewGuid(), // Different ID
                Name = dto.Name,
                IsDeleted = false
            };

            _mockFoodAndDrinkRepository.Setup(repo => repo.GetByIdAsync(id))
                .ReturnsAsync(existingItem);

            _mockFoodAndDrinkRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<FoodAndDrink, bool>>>()))
                .ReturnsAsync(conflictItem);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _foodAndDrinkService.UpdateFoodAndDrinkAsync(id, dto));
            Assert.Contains("Food or drink with the same name already exists", ex.Message);
        }

        [Fact]
        public async Task DeleteFoodAndDrinkAsync_WithValidId_ReturnsTrue()
        {
            // Arrange
            var id = Guid.NewGuid();
            var foodItem = new FoodAndDrink
            {
                Id = id,
                Name = "Food to Delete",
                IsDeleted = false
            };

            _mockFoodAndDrinkRepository.Setup(repo => repo.GetByIdAsync(id))
                .ReturnsAsync(foodItem);

            // Act
            var result = await _foodAndDrinkService.DeleteFoodAndDrinkAsync(id);

            // Assert
            Assert.True(result);
            _mockFoodAndDrinkRepository.Verify(repo => repo.SoftRemove(foodItem), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
            _mockAuditLogService.Verify(log => log.LogAsync(
                It.IsAny<Guid>(), AuditActionType.Delete, "FoodAndDrink", id,
                It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DeleteFoodAndDrinkAsync_WithNonExistentId_ReturnsFalse()
        {
            // Arrange
            var id = Guid.NewGuid();

            _mockFoodAndDrinkRepository.Setup(repo => repo.GetByIdAsync(id))
                .ReturnsAsync((FoodAndDrink)null!);

            // Act
            var result = await _foodAndDrinkService.DeleteFoodAndDrinkAsync(id);

            // Assert
            Assert.False(result);
            _mockFoodAndDrinkRepository.Verify(repo => repo.SoftRemove(It.IsAny<FoodAndDrink>()), Times.Never);
        }

        [Fact]
        public async Task DeleteFoodAndDrinkAsync_WithAlreadyDeletedItem_ReturnsFalse()
        {
            // Arrange
            var id = Guid.NewGuid();
            var foodItem = new FoodAndDrink
            {
                Id = id,
                Name = "Already Deleted Food",
                IsDeleted = true
            };

            _mockFoodAndDrinkRepository.Setup(repo => repo.GetByIdAsync(id))
                .ReturnsAsync(foodItem);

            // Act
            var result = await _foodAndDrinkService.DeleteFoodAndDrinkAsync(id);

            // Assert
            Assert.False(result);
            _mockFoodAndDrinkRepository.Verify(repo => repo.SoftRemove(It.IsAny<FoodAndDrink>()), Times.Never);
        }

        [Fact]
        public async Task DeleteFoodAndDrinkAsync_WhenExceptionOccurs_ReturnsFalse()
        {
            // Arrange
            var id = Guid.NewGuid();
            var foodItem = new FoodAndDrink
            {
                Id = id,
                Name = "Food to Delete",
                IsDeleted = false
            };

            _mockFoodAndDrinkRepository.Setup(repo => repo.GetByIdAsync(id))
                .ReturnsAsync(foodItem);

            _mockFoodAndDrinkRepository.Setup(repo => repo.SoftRemove(foodItem))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _foodAndDrinkService.DeleteFoodAndDrinkAsync(id);

            // Assert
            Assert.False(result);
        }
    }
}