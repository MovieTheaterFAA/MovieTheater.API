using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.DTOs.EventDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using Xunit;

namespace MovieTheater.UnitTest.Services
{
    public class EventServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILoggerService> _mockLoggerService;
        private readonly Mock<IClaimsService> _mockClaimsService;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<IRedisService> _mockRedisService;
        private readonly Mock<IGenericRepository<Event>> _mockEventRepository;
        private readonly Mock<IGenericRepository<Promotion>> _mockPromotionRepository;
        private readonly EventService _eventService;
        private readonly Guid _currentAdminId = Guid.NewGuid();

        public EventServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLoggerService = new Mock<ILoggerService>();
            _mockClaimsService = new Mock<IClaimsService>();
            _mockAuditLogService = new Mock<IAuditLogService>();
            _mockRedisService = new Mock<IRedisService>();
            _mockEventRepository = new Mock<IGenericRepository<Event>>();
            _mockPromotionRepository = new Mock<IGenericRepository<Promotion>>();

            // Setup UnitOfWork to return repositories
            _mockUnitOfWork.Setup(uow => uow.Events).Returns(_mockEventRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.Promotions).Returns(_mockPromotionRepository.Object);

            // Setup ClaimsService to return admin id
            _mockClaimsService.Setup(s => s.GetCurrentUserId).Returns(_currentAdminId);

            _eventService = new EventService(
                _mockUnitOfWork.Object,
                _mockLoggerService.Object,
                _mockClaimsService.Object,
                _mockAuditLogService.Object,
                _mockRedisService.Object
            );
        }

        [Fact]
        public async Task AddEventAsync_WithValidData_ReturnsEventDto()
        {
            // Arrange
            var eventDto = new EventRequestDto
            {
                Name = "Test Event",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(7),
                Detail = "Test event details"
            };

            _mockEventRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<Event, bool>>>()))
                .ReturnsAsync((Event)null!);

            _mockEventRepository.Setup(repo => repo.AddAsync(It.IsAny<Event>()))
                .ReturnsAsync((Event e) => e);

            // Act
            var result = await _eventService.AddEventAsync(eventDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(eventDto.Name, result.Name);
            Assert.Equal(eventDto.StartTime, result.StartTime);
            Assert.Equal(eventDto.EndTime, result.EndTime);
            Assert.Equal(eventDto.Detail, result.Detail);

            _mockEventRepository.Verify(repo => repo.AddAsync(It.Is<Event>(e =>
                e.Name == eventDto.Name &&
                e.StartTime == eventDto.StartTime &&
                e.EndTime == eventDto.EndTime &&
                e.Detail == eventDto.Detail)), Times.Once);

            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
            _mockRedisService.Verify(redis => redis.RemoveByPatternAsync("event:list:"), Times.Once);
            _mockAuditLogService.Verify(log => log.LogAsync(
                _currentAdminId, AuditActionType.Create, "Event", It.IsAny<Guid>(),
                null!, It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task AddEventAsync_WithExistingName_ThrowsException()
        {
            // Arrange
            var eventDto = new EventRequestDto
            {
                Name = "Existing Event",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(7)
            };

            var existingEvent = new Event
            {
                Id = Guid.NewGuid(),
                Name = eventDto.Name
            };

            _mockEventRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<Event, bool>>>()))
                .ReturnsAsync(existingEvent);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _eventService.AddEventAsync(eventDto));

            Assert.Contains("Event with this name already exists", exception.Message);
            _mockEventRepository.Verify(repo => repo.AddAsync(It.IsAny<Event>()), Times.Never);
        }

        [Fact]
        public async Task AddEventAsync_WhenSaveChangesFails_ThrowsException()
        {
            // Arrange
            var eventDto = new EventRequestDto
            {
                Name = "Test Event",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(7)
            };

            _mockEventRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<Event, bool>>>()))
                .ReturnsAsync((Event)null!);

            _mockEventRepository.Setup(repo => repo.AddAsync(It.IsAny<Event>()))
                .ReturnsAsync(new Event { Name = eventDto.Name });

            _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync())
                .ThrowsAsync(new DbUpdateException("Database error", new Exception("Inner error")));

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() => _eventService.AddEventAsync(eventDto));

            _mockLoggerService.Verify(logger =>
                logger.Error(It.Is<string>(s => s.Contains("DbUpdateException"))), Times.Once);
        }

        [Fact]
        public async Task DeleteEventByIdAsync_WithValidId_ReturnsTrue()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Name = "Test Event",
                IsDeleted = false,
                Promotions = new List<Promotion>
                {
                    new Promotion { Id = Guid.NewGuid(), Title = "Test Promotion" }
                }
            };

            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId, It.IsAny<Expression<Func<Event, object>>>()))
                .ReturnsAsync(eventEntity);

            // Act
            var result = await _eventService.DeleteEventByIdAsync(eventId);

            // Assert
            Assert.True(result);

            _mockPromotionRepository.Verify(repo => repo.SoftRemoveRange(It.IsAny<List<Promotion>>()), Times.Once);
            _mockEventRepository.Verify(repo => repo.SoftRemove(eventEntity), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
            _mockRedisService.Verify(redis => redis.RemoveByPatternAsync("event:list:"), Times.Once);
            _mockAuditLogService.Verify(log => log.LogAsync(
                _currentAdminId, AuditActionType.Delete, "Event", eventId,
                It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DeleteEventByIdAsync_WithNonExistentId_ReturnsFalse()
        {
            // Arrange
            var eventId = Guid.NewGuid();

            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId, It.IsAny<Expression<Func<Event, object>>>()))
                .ReturnsAsync((Event)null!);

            // Act
            var result = await _eventService.DeleteEventByIdAsync(eventId);

            // Assert
            Assert.False(result);
            _mockPromotionRepository.Verify(repo => repo.SoftRemoveRange(It.IsAny<List<Promotion>>()), Times.Never);
            _mockEventRepository.Verify(repo => repo.SoftRemove(It.IsAny<Event>()), Times.Never);
            _mockLoggerService.Verify(logger => logger.Warn(It.Is<string>(s =>
                s.Contains($"Event with ID {eventId} not found"))), Times.Once);
        }

        [Fact]
        public async Task DeleteEventByIdAsync_WithException_ReturnsFalse()
        {
            // Arrange
            var eventId = Guid.NewGuid();

            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId, It.IsAny<Expression<Func<Event, object>>>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _eventService.DeleteEventByIdAsync(eventId);

            // Assert
            Assert.False(result);
            _mockLoggerService.Verify(logger => logger.Error(It.Is<string>(s =>
                s.Contains("Error deleting Event"))), Times.Once);
        }

        [Fact]
        public async Task GetAllEventsAsync_ReturnsCachedResult_WhenCacheExists()
        {
            // Arrange
            var search = "test";
            var sortBy = "name";
            var isDescending = true;
            var page = 1;
            var pageSize = 10;

            var cachedResult = new Pagination<EventResponseDto>(
                new List<EventResponseDto>
                {
                    new EventResponseDto { Id = Guid.NewGuid(), Name = "Test Event" }
                },
                1, page, pageSize);

            var cacheKey = $"event:list:{search}:{sortBy}:{isDescending}:{page}:{pageSize}";

            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<EventResponseDto>>(cacheKey))
                .ReturnsAsync(cachedResult);

            // Act
            var result = await _eventService.GetAllEventsAsync(search, sortBy, isDescending, page, pageSize);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(cachedResult.Items.First().Name, result.Items.First().Name);

            _mockRedisService.Verify(redis => redis.GetAsync<Pagination<EventResponseDto>>(cacheKey), Times.Once);
            _mockEventRepository.Verify(repo => repo.GetAllAsync(
                It.IsAny<Expression<Func<Event, bool>>>(),
                It.IsAny<Expression<Func<Event, object>>>()), Times.Never);
        }

        [Fact]
        public async Task GetAllEventsAsync_WithValidSearch_ReturnsPagination()
        {
            // Arrange
            var search = "test";
            var sortBy = "name";
            var isDescending = false;
            var page = 1;
            var pageSize = 10;

            var cacheKey = $"event:list:{search}:{sortBy}:{isDescending}:{page}:{pageSize}";

            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<EventResponseDto>>(cacheKey))
                .ReturnsAsync((Pagination<EventResponseDto>)null!);

            var events = new List<Event>
            {
                new Event {
                    Id = Guid.NewGuid(),
                    Name = "Test Event 1",
                    StartTime = DateTime.UtcNow.AddDays(1),
                    EndTime = DateTime.UtcNow.AddDays(7),
                    IsDeleted = false,
                    Promotions = new List<Promotion>()
                },
                new Event {
                    Id = Guid.NewGuid(),
                    Name = "Test Event 2",
                    StartTime = DateTime.UtcNow.AddDays(2),
                    EndTime = DateTime.UtcNow.AddDays(8),
                    IsDeleted = false,
                    Promotions = new List<Promotion>()
                }
            };

            _mockEventRepository.Setup(repo => repo.GetAllAsync(
                It.IsAny<Expression<Func<Event, bool>>>(),
                It.IsAny<Expression<Func<Event, object>>>()))
                .ReturnsAsync(events);

            // Act
            var result = await _eventService.GetAllEventsAsync(search, sortBy, isDescending, page, pageSize);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal("Test Event 1", result.Items[0].Name);

            _mockRedisService.Verify(redis => redis.SetAsync(
                cacheKey, It.IsAny<Pagination<EventResponseDto>>(), It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task GetAllEventsAsync_WithNoEvents_ReturnsEmptyPagination()
        {
            // Arrange
            var search = "nonexistent";
            var sortBy = "name";
            var isDescending = false;
            var page = 1;
            var pageSize = 10;

            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<EventResponseDto>>(It.IsAny<string>()))
                .ReturnsAsync((Pagination<EventResponseDto>)null!);

            _mockEventRepository.Setup(repo => repo.GetAllAsync(
                It.IsAny<Expression<Func<Event, bool>>>(),
                It.IsAny<Expression<Func<Event, object>>>()))
                .ReturnsAsync(new List<Event>());

            // Act
            var result = await _eventService.GetAllEventsAsync(search, sortBy, isDescending, page, pageSize);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task GetAllEventsAsync_WithException_ThrowsException()
        {
            // Arrange
            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<EventResponseDto>>(It.IsAny<string>()))
                .ReturnsAsync((Pagination<EventResponseDto>)null!);

            _mockEventRepository.Setup(repo => repo.GetAllAsync(
                It.IsAny<Expression<Func<Event, bool>>>(),
                It.IsAny<Expression<Func<Event, object>>>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _eventService.GetAllEventsAsync("test", "name", false, 1, 10));

            Assert.Contains("An error occurred while retrieving events", exception.Message);
            _mockLoggerService.Verify(logger => logger.Error(It.Is<string>(s =>
                s.Contains("Failed to retrieve events"))), Times.Once);
        }

        [Fact]
        public async Task UpdateEventAsync_WithValidData_ReturnsUpdatedDto()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Name = "Original Name",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(7),
                Detail = "Original Detail",
                Image = "original.jpg",
                IsDeleted = false
            };

            var updateDto = new EventUpdateDto
            {
                Name = "Updated Name",
                Detail = "Updated Detail",
                Image = "updated.jpg"
            };

            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId))
                .ReturnsAsync(eventEntity);

            _mockEventRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<Event, bool>>>()))
                .ReturnsAsync((Event)null!);

            // Act
            var result = await _eventService.UpdateEventAsync(eventId, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateDto.Name, result.Name);
            Assert.Equal(updateDto.Detail, result.Detail);
            Assert.Equal(updateDto.Image, result.Image);

            _mockEventRepository.Verify(repo => repo.Update(It.Is<Event>(e =>
                e.Name == updateDto.Name &&
                e.Detail == updateDto.Detail &&
                e.Image == updateDto.Image)), Times.Once);

            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
            _mockRedisService.Verify(redis => redis.RemoveByPatternAsync("event:list:"), Times.Once);
            _mockAuditLogService.Verify(log => log.LogAsync(
                _currentAdminId, AuditActionType.Update, "Event", eventId,
                It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateEventAsync_WithNonExistentEvent_ThrowsException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var updateDto = new EventUpdateDto { Name = "Updated Name" };

            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId))
                .ReturnsAsync((Event)null!);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _eventService.UpdateEventAsync(eventId, updateDto));

            _mockLoggerService.Verify(logger => logger.Warn(It.Is<string>(s =>
                s.Contains($"Event with ID {eventId} not found"))), Times.Once);
        }

        [Fact]
        public async Task UpdateEventAsync_WithStartTimeInPast_ThrowsException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Name = "Test Event",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(7),
                IsDeleted = false
            };

            var updateDto = new EventUpdateDto
            {
                StartTime = DateTime.UtcNow.AddDays(-1)
            };

            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId))
                .ReturnsAsync(eventEntity);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _eventService.UpdateEventAsync(eventId, updateDto));

            Assert.Contains("Start time cannot be in the past", exception.Message);
        }

        [Fact]
        public async Task UpdateEventAsync_WithEndTimeBeforeStartTime_ThrowsException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Name = "Test Event",
                StartTime = DateTime.UtcNow.AddDays(5),
                EndTime = DateTime.UtcNow.AddDays(7),
                IsDeleted = false
            };

            var updateDto = new EventUpdateDto
            {
                EndTime = DateTime.UtcNow.AddDays(3)
            };

            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId))
                .ReturnsAsync(eventEntity);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _eventService.UpdateEventAsync(eventId, updateDto));

            Assert.Contains("End time must be greater than start time", exception.Message);
        }

        [Fact]
        public async Task UpdateEventAsync_WithNoChanges_ReturnsOriginalDto()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Name = "Test Event",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(7),
                Detail = "Test detail",
                Image = "test.jpg",
                IsDeleted = false
            };

            var updateDto = new EventUpdateDto
            {
                Name = "Test Event",
                Detail = "Test detail"
            };

            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId))
                .ReturnsAsync(eventEntity);

            // Act
            var result = await _eventService.UpdateEventAsync(eventId, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(eventEntity.Name, result.Name);
            Assert.Equal(eventEntity.Detail, result.Detail);

            _mockEventRepository.Verify(repo => repo.Update(It.IsAny<Event>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateEventAsync_WithExistingName_ThrowsException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Name = "Original Name",
                IsDeleted = false
            };

            var existingEvent = new Event
            {
                Id = Guid.NewGuid(),
                Name = "Existing Name",
                IsDeleted = false
            };

            var updateDto = new EventUpdateDto
            {
                Name = "Existing Name"
            };

            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId))
                .ReturnsAsync(eventEntity);

            _mockEventRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<Event, bool>>>()))
                .ReturnsAsync(existingEvent);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _eventService.UpdateEventAsync(eventId, updateDto));

            Assert.Contains("Event with the same name already exists", exception.Message);
        }

        [Fact]
        public async Task CleanUpExpiredEventsAsync_DeletesExpiredEvents()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var expiredEvents = new List<Event>
            {
                new Event
                {
                    Id = Guid.NewGuid(),
                    Name = "Expired Event 1",
                    StartTime = now.AddDays(-10),
                    EndTime = now.AddDays(-1),
                    IsDeleted = false,
                    Promotions = new List<Promotion>
                    {
                        new Promotion { Id = Guid.NewGuid(), Title = "Expired Promotion" }
                    }
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    Name = "Expired Event 2",
                    StartTime = now.AddDays(-5),
                    EndTime = now.AddDays(-2),
                    IsDeleted = false,
                    Promotions = new List<Promotion>()
                }
            };

            _mockEventRepository.Setup(repo => repo.GetAllAsync(
                It.IsAny<Expression<Func<Event, bool>>>(),
                It.IsAny<Expression<Func<Event, object>>>()))
                .ReturnsAsync(expiredEvents);

            // Act
            await _eventService.CleanUpExpiredEventsAsync();

            // Assert
            _mockPromotionRepository.Verify(repo => repo.SoftRemoveRange(It.IsAny<List<Promotion>>()), Times.Once);
            _mockEventRepository.Verify(repo => repo.SoftRemove(It.IsAny<Event>()), Times.Exactly(2));
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
            _mockRedisService.Verify(redis => redis.RemoveByPatternAsync("event:list:"), Times.Once);

            _mockAuditLogService.Verify(log => log.LogAsync(
                Guid.Empty, AuditActionType.Delete, "Event", It.IsAny<Guid>(),
                It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>(),
                "System auto-deleted expired event."), Times.Exactly(2));
        }

        [Fact]
        public async Task CleanUpExpiredEventsAsync_WithNoExpiredEvents_DoesNothing()
        {
            // Arrange
            _mockEventRepository.Setup(repo => repo.GetAllAsync(
                It.IsAny<Expression<Func<Event, bool>>>(),
                It.IsAny<Expression<Func<Event, object>>>()))
                .ReturnsAsync(new List<Event>());

            // Act
            await _eventService.CleanUpExpiredEventsAsync();

            // Assert
            _mockEventRepository.Verify(repo => repo.SoftRemove(It.IsAny<Event>()), Times.Never);
            _mockPromotionRepository.Verify(repo => repo.SoftRemoveRange(It.IsAny<List<Promotion>>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
            _mockRedisService.Verify(redis => redis.RemoveByPatternAsync("event:list:"), Times.Once);
        }

        [Fact]
        public async Task CleanUpExpiredEventsAsync_WithException_LogsError()
        {
            // Arrange
            _mockEventRepository.Setup(repo => repo.GetAllAsync(
                It.IsAny<Expression<Func<Event, bool>>>(),
                It.IsAny<Expression<Func<Event, object>>>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            await _eventService.CleanUpExpiredEventsAsync();

            // Assert
            _mockLoggerService.Verify(logger => logger.Error(It.Is<string>(s =>
                s.Contains("Error while cleaning up expired events"))), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task GetAllEventsAsync_SortByStartTime_ReturnsSorted()
        {
            // Arrange
            var events = new List<Event>
    {
        new Event { Id = Guid.NewGuid(), Name = "A", StartTime = DateTime.UtcNow.AddDays(2), EndTime = DateTime.UtcNow.AddDays(3), IsDeleted = false, Promotions = new List<Promotion>() },
        new Event { Id = Guid.NewGuid(), Name = "B", StartTime = DateTime.UtcNow.AddDays(1), EndTime = DateTime.UtcNow.AddDays(4), IsDeleted = false, Promotions = new List<Promotion>() }
    };
            _mockRedisService.Setup(r => r.GetAsync<Pagination<EventResponseDto>>(It.IsAny<string>())).ReturnsAsync((Pagination<EventResponseDto>)null!);
            _mockEventRepository.Setup(r => r.GetAllAsync(null!, It.IsAny<Expression<Func<Event, object>>>())).ReturnsAsync(events);

            // Act
            var result = await _eventService.GetAllEventsAsync(null, "starttime", false, 1, 10);

            // Assert
            Assert.Equal(events[1].Name, result.Items[0].Name); // B có StartTime sớm hơn
        }

        [Fact]
        public async Task GetAllEventsAsync_SortByEndTimeDescending_ReturnsSorted()
        {
            // Arrange
            var events = new List<Event>
    {
        new Event { Id = Guid.NewGuid(), Name = "A", StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddDays(1), IsDeleted = false, Promotions = new List<Promotion>() },
        new Event { Id = Guid.NewGuid(), Name = "B", StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddDays(2), IsDeleted = false, Promotions = new List<Promotion>() }
    };
            _mockRedisService.Setup(r => r.GetAsync<Pagination<EventResponseDto>>(It.IsAny<string>())).ReturnsAsync((Pagination<EventResponseDto>)null!);
            _mockEventRepository.Setup(r => r.GetAllAsync(null!, It.IsAny<Expression<Func<Event, object>>>())).ReturnsAsync(events);

            // Act
            var result = await _eventService.GetAllEventsAsync(null, "endtime", true, 1, 10);

            // Assert
            Assert.Equal(events[1].Name, result.Items[0].Name); // B có EndTime muộn hơn
        }

        [Fact]
        public async Task GetAllEventsAsync_SortByNull_UsesDefaultSort()
        {
            // Arrange
            var events = new List<Event>
    {
        new Event { Id = Guid.NewGuid(), Name = "A", IsDeleted = false, Promotions = new List<Promotion>() },
        new Event { Id = Guid.NewGuid(), Name = "B", IsDeleted = false, Promotions = new List<Promotion>() }
    };
            _mockRedisService.Setup(r => r.GetAsync<Pagination<EventResponseDto>>(It.IsAny<string>())).ReturnsAsync((Pagination<EventResponseDto>)null!);
            _mockEventRepository.Setup(r => r.GetAllAsync(null!, It.IsAny<Expression<Func<Event, object>>>())).ReturnsAsync(events);

            // Act
            var result = await _eventService.GetAllEventsAsync(null, null, false, 1, 10);

            // Assert
            Assert.Equal(events[0].Id, result.Items[0].Id); // Default sort by Id
        }

        [Fact]
        public async Task GetAllEventsAsync_SearchIsNullOrWhitespace_ReturnsAll()
        {
            // Arrange
            var events = new List<Event>
    {
        new Event { Id = Guid.NewGuid(), Name = "A", IsDeleted = false, Promotions = new List<Promotion>() }
    };
            _mockRedisService.Setup(r => r.GetAsync<Pagination<EventResponseDto>>(It.IsAny<string>())).ReturnsAsync((Pagination<EventResponseDto>)null!);
            _mockEventRepository.Setup(r => r.GetAllAsync(null!, It.IsAny<Expression<Func<Event, object>>>())).ReturnsAsync(events);

            // Act
            var result1 = await _eventService.GetAllEventsAsync(null, "name", false, 1, 10);
            var result2 = await _eventService.GetAllEventsAsync("", "name", false, 1, 10);
            var result3 = await _eventService.GetAllEventsAsync("   ", "name", false, 1, 10);

            // Assert
            Assert.Single(result1.Items);
            Assert.Single(result2.Items);
            Assert.Single(result3.Items);
        }

        [Fact]
        public async Task GetAllEventsAsync_EventWithNullPromotions_DoesNotThrow()
        {
            // Arrange
            var events = new List<Event>
    {
        new Event { Id = Guid.NewGuid(), Name = "A", IsDeleted = false, Promotions = null! }
    };
            _mockRedisService.Setup(r => r.GetAsync<Pagination<EventResponseDto>>(It.IsAny<string>())).ReturnsAsync((Pagination<EventResponseDto>)null!);
            _mockEventRepository.Setup(r => r.GetAllAsync(null!, It.IsAny<Expression<Func<Event, object>>>())).ReturnsAsync(events);

            // Act
            var result = await _eventService.GetAllEventsAsync(null, "name", false, 1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Items[0].Promotions);
        }

        [Fact]
        public async Task AddEventAsync_WithNullDetail_DoesNotThrow()
        {
            // Arrange
            var eventDto = new EventRequestDto
            {
                Name = "Test Event",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(7),
                Detail = null!
            };
            _mockEventRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<Event, bool>>>())).ReturnsAsync((Event)null!);
            _mockEventRepository.Setup(repo => repo.AddAsync(It.IsAny<Event>())).ReturnsAsync((Event e) => e);

            // Act
            var result = await _eventService.AddEventAsync(eventDto);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task AddEventAsync_WhenUnexpectedException_Throws()
        {
            // Arrange
            var eventDto = new EventRequestDto
            {
                Name = "Test Event",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(7),
                Detail = "Test"
            };
            _mockEventRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<Event, bool>>>())).ReturnsAsync((Event)null!);
            _mockEventRepository.Setup(repo => repo.AddAsync(It.IsAny<Event>())).ReturnsAsync((Event e) => e);
            _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync()).ThrowsAsync(new Exception("Unexpected"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _eventService.AddEventAsync(eventDto));
            // Không verify logger.Error vì code không log cho Exception thường
        }

        [Fact]
        public async Task DeleteEventByIdAsync_WithNullPromotions_DoesNotThrow()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Name = "Test Event",
                IsDeleted = false,
                Promotions = null!
            };
            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId, It.IsAny<Expression<Func<Event, object>>>())).ReturnsAsync(eventEntity);

            // Act
            var result = await _eventService.DeleteEventByIdAsync(eventId);

            // Assert
            Assert.True(result);
            _mockPromotionRepository.Verify(repo => repo.SoftRemoveRange(It.IsAny<List<Promotion>>()), Times.Never);
        }

        [Fact]
        public async Task DeleteEventByIdAsync_AlreadyDeletedEvent_ReturnsFalse()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Name = "Test Event",
                IsDeleted = true,
                Promotions = new List<Promotion>()
            };
            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId, It.IsAny<Expression<Func<Event, object>>>())).ReturnsAsync(eventEntity);

            // Act
            var result = await _eventService.DeleteEventByIdAsync(eventId);

            // Assert
            Assert.False(result);
            _mockLoggerService.Verify(logger => logger.Warn(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateEventAsync_EventIsDeleted_ThrowsException()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Name = "Test Event",
                IsDeleted = true
            };
            var updateDto = new EventUpdateDto { Name = "New Name" };
            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId)).ReturnsAsync(eventEntity);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _eventService.UpdateEventAsync(eventId, updateDto));
            _mockLoggerService.Verify(logger => logger.Warn(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateEventAsync_ImageAndDetailUnchanged_DoesNotUpdate()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Name = "Test Event",
                Detail = "Detail",
                Image = "img.jpg",
                IsDeleted = false
            };
            var updateDto = new EventUpdateDto { Image = "img.jpg", Detail = "Detail" };
            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId)).ReturnsAsync(eventEntity);

            // Act
            var result = await _eventService.UpdateEventAsync(eventId, updateDto);

            // Assert
            Assert.NotNull(result);
            _mockEventRepository.Verify(repo => repo.Update(It.IsAny<Event>()), Times.Never);
        }

        [Fact]
        public async Task UpdateEventAsync_WhenUnexpectedException_ThrowsAndLogs()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Name = "Test Event",
                IsDeleted = false
            };
            var updateDto = new EventUpdateDto { Name = "New Name" };
            _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId)).ReturnsAsync(eventEntity);
            _mockEventRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<Event, bool>>>())).ThrowsAsync(new Exception("Unexpected"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _eventService.UpdateEventAsync(eventId, updateDto));
            _mockLoggerService.Verify(logger => logger.Error(It.IsAny<string>()), Times.Once);
        }
    }
}