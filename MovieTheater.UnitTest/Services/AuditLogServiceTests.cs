using Microsoft.Extensions.Configuration;
using Moq;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.DTOs.AuditLogDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;
using System.Linq.Expressions;
using System.Text.Json;

namespace MovieTheater.UnitTest.Services
{
    public class AuditLogServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILoggerService> _mockLoggerService;
        private readonly Mock<IRedisService> _mockRedisService;
        private readonly IAuditLogService _auditLogService;
        private readonly Mock<IGenericRepository<AuditLog>> _mockAuditLogRepository;
        private readonly Mock<IGenericRepository<User>> _mockUserRepository;

        public AuditLogServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLoggerService = new Mock<ILoggerService>();
            _mockRedisService = new Mock<IRedisService>();
            _mockAuditLogRepository = new Mock<IGenericRepository<AuditLog>>();
            _mockUserRepository = new Mock<IGenericRepository<User>>();

            _mockUnitOfWork.Setup(uow => uow.AuditLogs).Returns(_mockAuditLogRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.Users).Returns(_mockUserRepository.Object);

            _auditLogService = new AuditLogService(_mockUnitOfWork.Object, _mockLoggerService.Object, _mockRedisService.Object);
        }

        [Fact]
        public async Task LogAsync_CreatesAuditLogAndSavesChanges()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var actionType = AuditActionType.Create;
            var entityType = "User";
            var entityId = Guid.NewGuid();
            var oldValue = new { Name = "OldName" };
            var newValue = new { Name = "NewName" };
            var changedFields = "Name";
            var reason = "Testing";

            _mockAuditLogRepository.Setup(repo => repo.AddAsync(It.IsAny<AuditLog>()))
                .ReturnsAsync((AuditLog log) => log);

            // Act
            await _auditLogService.LogAsync(adminId, actionType, entityType, entityId, oldValue, newValue, changedFields, reason);

            // Assert
            _mockAuditLogRepository.Verify(repo => repo.AddAsync(It.Is<AuditLog>(log =>
                log.AdminId == adminId &&
                log.ActionType == actionType.ToString() &&
                log.EntityType == entityType &&
                log.EntityId == entityId &&
                log.OldValue == JsonSerializer.Serialize(oldValue, (JsonSerializerOptions)null!) && // Explicitly pass null for options
                log.NewValue == JsonSerializer.Serialize(newValue, (JsonSerializerOptions)null!) && // Explicitly pass null for options
                log.ChangedFields == changedFields &&
                log.Reason == reason)),
    Times.Once);

            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
            _mockRedisService.Verify(redis => redis.RemoveByPatternAsync("auditlog:list:"), Times.Once);
        }

        [Fact]
        public async Task LogAsync_WhenSaveFails_ThrowsException()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var actionType = AuditActionType.Update;
            var entityType = "User";
            var entityId = Guid.NewGuid();
            var exception = new Exception("Database error");

            _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync())
                .ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(() =>
                _auditLogService.LogAsync(adminId, actionType, entityType, entityId, null!, null!, null!));

            Assert.Same(exception, thrownException);
        }

        [Fact]
        public async Task ViewLogAsync_WithCacheHit_ReturnsCachedData()
        {
            // Arrange
            string search = "test";
            AuditActionType? actionType = AuditActionType.Update;
            string entityType = "User";
            bool isDescending = true;
            int page = 1;
            int pageSize = 10;

            var expectedResult = new Pagination<AuditLogDto>(
                new List<AuditLogDto> { new AuditLogDto { Id = Guid.NewGuid() } },
                1, page, pageSize);

            var cacheKey = $"auditlog:list:{search}:{actionType}:{entityType}:{isDescending}:{page}:{pageSize}";

            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<AuditLogDto>>(cacheKey))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _auditLogService.ViewLogAsync(search, actionType, entityType, isDescending, page, pageSize);

            // Assert
            Assert.Same(expectedResult, result);
            _mockLoggerService.Verify(logger => logger.Info(It.Is<string>(s => s.Contains("[CACHE HIT]"))), Times.Once);

            // Verify we don't query the database when cache is hit
            _mockAuditLogRepository.Verify(repo => repo.GetAllAsync(It.IsAny<Expression<Func<AuditLog, bool>>>(), It.IsAny<Expression<Func<AuditLog, object>>[]>()), Times.Never);
        }

        [Fact]
        public async Task ViewLogAsync_WithCacheMiss_QueriesDatabaseAndCachesResult()
        {
            // Arrange
            string search = null!;
            AuditActionType? actionType = null!;
            string entityType = null!;
            bool isDescending = false;
            int page = 1;
            int pageSize = 10;

            var auditLogs = new List<AuditLog>
            {
                new AuditLog {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.Create.ToString(),
                    EntityType = "User",
                    AdminId = Guid.NewGuid(),
                    OldValue = null,
                    NewValue = JsonSerializer.Serialize(new { Name = "New User" }),
                    Timestamp = DateTime.UtcNow
                },
                new AuditLog {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.Update.ToString(),
                    EntityType = "Movie",
                    AdminId = Guid.NewGuid(),
                    OldValue = JsonSerializer.Serialize(new { Title = "Old Title" }),
                    NewValue = JsonSerializer.Serialize(new { Title = "New Title" }),
                    Timestamp = DateTime.UtcNow.AddHours(-1)
                }
            };

            var users = new List<User>();

            var cacheKey = $"auditlog:list:{search}:{actionType}:{entityType}:{isDescending}:{page}:{pageSize}";

            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<AuditLogDto>>(cacheKey))
                .ReturnsAsync((Pagination<AuditLogDto>)null!);

            _mockAuditLogRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<AuditLog, bool>>>(), It.IsAny<Expression<Func<AuditLog, object>>[]>())).ReturnsAsync(auditLogs);

            _mockUserRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<Expression<Func<User, object>>[]>())).ReturnsAsync(users);

            _mockAuditLogRepository.Setup(repo => repo.GetQueryable())
                .Returns(auditLogs.AsQueryable());

            // Act
            var result = await _auditLogService.ViewLogAsync(search, actionType, entityType, isDescending, page, pageSize);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(page, result.CurrentPage);
            Assert.Equal(pageSize, result.PageSize);

            _mockLoggerService.Verify(logger => logger.Info(It.Is<string>(s => s.Contains("[CACHE MISS]"))), Times.Once);
            _mockRedisService.Verify(redis => redis.SetAsync(cacheKey, It.IsAny<Pagination<AuditLogDto>>(),
                It.IsAny<TimeSpan>()), Times.Once);
            _mockLoggerService.Verify(logger => logger.Success(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ViewLogAsync_WithActionTypeFilter_ReturnsFilteredResults()
        {
            // Arrange
            AuditActionType actionType = AuditActionType.Delete;
            int page = 1;
            int pageSize = 10;

            var auditLogs = new List<AuditLog>
            {
                new AuditLog {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.Create.ToString(),
                    EntityType = "User"
                },
                new AuditLog {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.Delete.ToString(),
                    EntityType = "User"
                }
            };

            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<AuditLogDto>>(It.IsAny<string>()))
                .ReturnsAsync((Pagination<AuditLogDto>)null!);

            _mockAuditLogRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<AuditLog, bool>>>(), It.IsAny<Expression<Func<AuditLog, object>>[]>())).ReturnsAsync(auditLogs);

            _mockUserRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<Expression<Func<User, object>>[]>())).ReturnsAsync(new List<User>());

            _mockAuditLogRepository.Setup(repo => repo.GetQueryable())
                .Returns(auditLogs.AsQueryable());

            // Act
            var result = await _auditLogService.ViewLogAsync(null, actionType, null, false, page, pageSize);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(AuditActionType.Delete, result.Items[0].ActionType);
        }

        [Fact]
        public async Task ViewLogAsync_WithEntityTypeFilter_ReturnsFilteredResults()
        {
            // Arrange
            string entityType = "Movie";
            int page = 1;
            int pageSize = 10;

            var auditLogs = new List<AuditLog>
            {
                new AuditLog {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.Update.ToString(),
                    EntityType = "User"
                },
                new AuditLog {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.Update.ToString(),
                    EntityType = "Movie"
                }
            };

            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<AuditLogDto>>(It.IsAny<string>()))
                .ReturnsAsync((Pagination<AuditLogDto>)null!);

            _mockAuditLogRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<AuditLog, bool>>>(), It.IsAny<Expression<Func<AuditLog, object>>[]>())).ReturnsAsync(auditLogs);

            _mockUserRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<Expression<Func<User, object>>[]>())).ReturnsAsync(new List<User>());

            _mockAuditLogRepository.Setup(repo => repo.GetQueryable())
                .Returns(auditLogs.AsQueryable());

            // Act
            var result = await _auditLogService.ViewLogAsync(null, null, entityType, false, page, pageSize);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal("Movie", result.Items[0].EntityType);
        }

        [Fact]
        public async Task ViewLogAsync_WithSearchParameter_FiltersCorrectly()
        {
            // Arrange
            string search = "user";
            int page = 1;
            int pageSize = 10;

            var auditLogs = new List<AuditLog>
            {
                new AuditLog {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.Create.ToString(),
                    EntityType = "User"
                },
                new AuditLog {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.Update.ToString(),
                    EntityType = "Movie"
                }
            };

            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), FullName = "Admin User" }
            };

            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<AuditLogDto>>(It.IsAny<string>()))
                .ReturnsAsync((Pagination<AuditLogDto>)null!);

            _mockAuditLogRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<AuditLog, bool>>>(), It.IsAny<Expression<Func<AuditLog, object>>[]>())).ReturnsAsync(auditLogs);

            _mockUserRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<Expression<Func<User, object>>[]>())).ReturnsAsync(users);

            _mockAuditLogRepository.Setup(repo => repo.GetQueryable())
                .Returns(auditLogs.AsQueryable());

            // Act
            var result = await _auditLogService.ViewLogAsync(search, null, null, false, page, pageSize);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal("User", result.Items[0].EntityType);
        }

        [Fact]
        public async Task ViewLogAsync_WithDescendingSort_SortsCorrectly()
        {
            // Arrange
            bool isDescending = true;
            int page = 1;
            int pageSize = 10;

            var now = DateTime.UtcNow;

            var auditLogs = new List<AuditLog>
            {
                new AuditLog {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.Create.ToString(),
                    EntityType = "User",
                    Timestamp = now.AddMinutes(-10)
                },
                new AuditLog {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.Update.ToString(),
                    EntityType = "User",
                    Timestamp = now
                }
            };

            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<AuditLogDto>>(It.IsAny<string>()))
                .ReturnsAsync((Pagination<AuditLogDto>)null!);

            _mockAuditLogRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<AuditLog, bool>>>(), It.IsAny<Expression<Func<AuditLog, object>>[]>())).ReturnsAsync(auditLogs);

            _mockUserRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<Expression<Func<User, object>>[]>())).ReturnsAsync(new List<User>());

            _mockAuditLogRepository.Setup(repo => repo.GetQueryable())
                .Returns(auditLogs.AsQueryable());

            // Act
            var result = await _auditLogService.ViewLogAsync(null, null, null, isDescending, page, pageSize);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Items.Count);
            // First item should be the most recent one
            Assert.Equal(now, result.Items[0].Timestamp);
        }

        [Fact]
        public async Task ViewLogAsync_WithPagination_ReturnsPaginatedResults()
        {
            // Arrange
            int page = 2;
            int pageSize = 1;

            var auditLogs = new List<AuditLog>
            {
                new AuditLog {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.Create.ToString(),
                    EntityType = "User",
                    Timestamp = DateTime.UtcNow.AddMinutes(-20)
                },
                new AuditLog {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.Update.ToString(),
                    EntityType = "User",
                    Timestamp = DateTime.UtcNow.AddMinutes(-10)
                }
            };

            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<AuditLogDto>>(It.IsAny<string>()))
                .ReturnsAsync((Pagination<AuditLogDto>)null!);

            _mockAuditLogRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<AuditLog, bool>>>(), It.IsAny<Expression<Func<AuditLog, object>>[]>())).ReturnsAsync(auditLogs);

            _mockUserRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<Expression<Func<User, object>>[]>())).ReturnsAsync(new List<User>());

            _mockAuditLogRepository.Setup(repo => repo.GetQueryable())
                .Returns(auditLogs.AsQueryable());

            // Act
            var result = await _auditLogService.ViewLogAsync(null, null, null, false, page, pageSize);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(page, result.CurrentPage);
            Assert.Equal(pageSize, result.PageSize);
            Assert.Equal(2, result.TotalPages);
        }

        [Fact]
        public async Task ViewLogAsync_WhenExceptionOccurs_RethrowsException()
        {
            // Arrange
            int page = 1;
            int pageSize = 10;

            var exception = new Exception("Query error");

            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<AuditLogDto>>(It.IsAny<string>()))
                .ReturnsAsync((Pagination<AuditLogDto>)null!);

            _mockAuditLogRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<AuditLog, bool>>>(), It.IsAny<Expression<Func<AuditLog, object>>[]>()))
                .ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(() =>
                _auditLogService.ViewLogAsync(null, null, null, false, page, pageSize));

            Assert.NotSame(exception, thrownException); // Because it wraps in a new exception
            _mockLoggerService.Verify(logger => logger.Error(It.IsAny<string>()), Times.Once);
        }
    }
}