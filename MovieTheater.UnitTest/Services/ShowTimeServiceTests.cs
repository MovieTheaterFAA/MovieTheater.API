using Microsoft.EntityFrameworkCore;
using Moq;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.DTOs.ShowTimeDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;
using static MovieTheater.Domain.DTOs.ShowTimeDTOs.BatchShowtimeRequestDto;

namespace MovieTheater.UnitTest.Services
{
    public class ShowTimeServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILoggerService> _mockLoggerService;
        private readonly Mock<IClaimsService> _mockClaimsService;
        private readonly Mock<IRedisService> _mockRedisService;
        private readonly Mock<IGenericRepository<ShowTime>> _mockShowTimeRepo;
        private readonly Mock<IGenericRepository<Movie>> _mockMovieRepo;
        private readonly Mock<IGenericRepository<CinemaRoom>> _mockRoomRepo;
        private readonly Mock<IGenericRepository<AuditLog>> _mockAuditLogRepo;
        private readonly ShowTimeService _service;
        private readonly Guid _adminId = Guid.NewGuid();

        public ShowTimeServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLoggerService = new Mock<ILoggerService>();
            _mockClaimsService = new Mock<IClaimsService>();
            _mockRedisService = new Mock<IRedisService>();
            _mockShowTimeRepo = new Mock<IGenericRepository<ShowTime>>();
            _mockMovieRepo = new Mock<IGenericRepository<Movie>>();
            _mockRoomRepo = new Mock<IGenericRepository<CinemaRoom>>();
            _mockAuditLogRepo = new Mock<IGenericRepository<AuditLog>>();

            _mockUnitOfWork.Setup(u => u.ShowTimes).Returns(_mockShowTimeRepo.Object);
            _mockUnitOfWork.Setup(u => u.Movies).Returns(_mockMovieRepo.Object);
            _mockUnitOfWork.Setup(u => u.CinemaRooms).Returns(_mockRoomRepo.Object);
            _mockUnitOfWork.Setup(u => u.AuditLogs).Returns(_mockAuditLogRepo.Object);
            _mockClaimsService.Setup(c => c.GetCurrentUserId).Returns(_adminId);

            _service = new ShowTimeService(
                _mockUnitOfWork.Object,
                _mockLoggerService.Object,
                _mockClaimsService.Object,
                _mockRedisService.Object
            );
        }

        [Fact]
        public async Task AddBatchShowTimesAsync_ThrowsIfShowtimeNotInNextWeek()
        {
            var dto = new BatchShowTimeRequestDto
            {
                CinemaRoomId = Guid.NewGuid(),
                ShowTimes = new List<BatchShowtimeRequestDto.SingleShowTimeDto>
                {
                    new() { MovieId = Guid.NewGuid(), StartTime = DateTime.UtcNow }
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddBatchShowTimesAsync(dto));
        }

        [Fact]
        public async Task AddBatchShowTimesAsync_ThrowsIfRoomNotFound()
        {
            var nextWeek = DateTime.UtcNow.Date.AddDays(7 - (int)DateTime.UtcNow.DayOfWeek);
            var dto = new BatchShowTimeRequestDto
            {
                CinemaRoomId = Guid.NewGuid(),
                ShowTimes = new List<BatchShowtimeRequestDto.SingleShowTimeDto>
                {
                    new() { MovieId = Guid.NewGuid(), StartTime = nextWeek }
                }
            };
            _mockRoomRepo.Setup(r => r.GetByIdAsync(dto.CinemaRoomId, null!)).ReturnsAsync((CinemaRoom)null!);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddBatchShowTimesAsync(dto));
        }

        [Fact]
        public async Task AddBatchShowTimesAsync_ThrowsIfMovieNotFound()
        {
            var nextWeek = DateTime.UtcNow.Date.AddDays(7 - (int)DateTime.UtcNow.DayOfWeek);
            var movieId = Guid.NewGuid();
            var dto = new BatchShowTimeRequestDto
            {
                CinemaRoomId = Guid.NewGuid(),
                ShowTimes = new List<BatchShowtimeRequestDto.SingleShowTimeDto>
                {
                    new() { MovieId = movieId, StartTime = nextWeek }
                }
            };
            _mockRoomRepo.Setup(r => r.GetByIdAsync(dto.CinemaRoomId, null!)).ReturnsAsync(new CinemaRoom());
            _mockMovieRepo.Setup(m => m.GetQueryable()).Returns(new List<Movie>().AsQueryable());

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddBatchShowTimesAsync(dto));
        }

        [Fact]
        public async Task AddBatchShowTimesAsync_ThrowsIfOverlapWithExisting()
        {
            var nextWeek = DateTime.UtcNow.Date.AddDays(7 - (int)DateTime.UtcNow.DayOfWeek);
            var movieId = Guid.NewGuid();
            var roomId = Guid.NewGuid();
            var dto = new BatchShowTimeRequestDto
            {
                CinemaRoomId = roomId,
                ShowTimes = new List<BatchShowtimeRequestDto.SingleShowTimeDto>
                {
                    new() { MovieId = movieId, StartTime = nextWeek }
                }
            };
            _mockRoomRepo.Setup(r => r.GetByIdAsync(roomId, null!)).ReturnsAsync(new CinemaRoom());
            _mockMovieRepo.Setup(m => m.GetQueryable()).Returns(new List<Movie>
            {
                new Movie { Id = movieId, RunningTime = 100 }
            }.AsQueryable());
            _mockShowTimeRepo.Setup(r => r.GetQueryable()).Returns(new List<ShowTime>
            {
                new ShowTime { CinemaRoomId = roomId, ShowDate = nextWeek, Duration = TimeSpan.FromMinutes(120) }
            }.AsQueryable());
            _mockShowTimeRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ShowTime, bool>>>(), It.IsAny<Expression<Func<ShowTime, object>>[]>()))
                .ReturnsAsync(new List<ShowTime>
                {
                    new ShowTime { CinemaRoomId = roomId, ShowDate = nextWeek, Duration = TimeSpan.FromMinutes(120) }
                });

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddBatchShowTimesAsync(dto));
        }

        [Fact]
        public async Task DeleteShowTimesByDateAsync_ReturnsZeroIfNoShowtimes()
        {
            var date = DateTime.UtcNow.Date;
            _mockShowTimeRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ShowTime, bool>>>(), It.IsAny<Expression<Func<ShowTime, object>>[]>()))
                .ReturnsAsync(new List<ShowTime>());

            var result = await _service.DeleteShowTimesByDateAsync(date);

            Assert.Equal(0, result);
        }

        [Fact]
        public async Task DeleteShowTimesByDateAsync_DeletesAndReturnsCount()
        {
            var date = DateTime.UtcNow.Date;
            var showTimes = new List<ShowTime>
            {
                new ShowTime { Id = Guid.NewGuid(), ShowDate = date, IsDeleted = false }
            };
            _mockShowTimeRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<ShowTime, bool>>>(), It.IsAny<Expression<Func<ShowTime, object>>[]>()))
                .ReturnsAsync(showTimes);
            _mockShowTimeRepo.Setup(r => r.SoftRemove(It.IsAny<ShowTime>())).ReturnsAsync(true);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockAuditLogRepo.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await _service.DeleteShowTimesByDateAsync(date);

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task UpdateShowTimeAsync_ThrowsIfShowTimeNotFound()
        {
            var showTimeId = Guid.NewGuid();
            _mockShowTimeRepo.Setup(r => r.GetByIdAsync(showTimeId, null!)).ReturnsAsync((ShowTime)null!);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateShowTimeAsync(showTimeId, new UpdateShowtimeDto()));
        }

        [Fact]
        public async Task UpdateShowTimeAsync_ThrowsIfMovieNotFound()
        {
            var showTimeId = Guid.NewGuid();
            var dto = new UpdateShowtimeDto { MovieId = Guid.NewGuid() };
            _mockShowTimeRepo.Setup(r => r.GetByIdAsync(showTimeId, null!)).ReturnsAsync(new ShowTime());
            _mockMovieRepo.Setup(m => m.GetByIdAsync(dto.MovieId, null!)).ReturnsAsync((Movie)null!);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateShowTimeAsync(showTimeId, dto));
        }

        [Fact]
        public async Task UpdateShowTimeAsync_ThrowsIfOverlap()
        {
            // Arrange
            var showTimeId = Guid.NewGuid();
            var movieId = Guid.NewGuid();
            var roomId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var dto = new UpdateShowtimeDto
            {
                MovieId = movieId,
                CinemaRoomId = roomId,
                ShowDate = now,
                Duration = TimeSpan.FromMinutes(120)
            };

            var existingShowTime = new ShowTime
            {
                Id = Guid.NewGuid(),
                CinemaRoomId = roomId,
                ShowDate = now,
                Duration = TimeSpan.FromMinutes(120),
                IsDeleted = false
            };

            _mockShowTimeRepo.Setup(r => r.GetByIdAsync(showTimeId))
                .ReturnsAsync(new ShowTime { Id = showTimeId, IsDeleted = false });

            _mockMovieRepo.Setup(m => m.GetByIdAsync(movieId))
                .ReturnsAsync(new Movie { Id = movieId, RunningTime = 100 });

            _mockShowTimeRepo.Setup(r => r.GetQueryable())
                .Returns(new List<ShowTime> { existingShowTime }.AsQueryable());

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateShowTimeAsync(showTimeId, dto));
        }


        [Fact]
        public async Task SoftDeleteShowTimeAsync_ReturnsFalseIfNotFound()
        {
            var showTimeId = Guid.NewGuid();
            _mockShowTimeRepo.Setup(r => r.GetByIdAsync(showTimeId, null!)).ReturnsAsync((ShowTime)null!);

            var result = await _service.SoftDeleteShowTimeAsync(showTimeId);

            Assert.False(result);
        }

        [Fact]
        public async Task SoftDeleteShowTimeAsync_ReturnsTrueIfSuccess()
        {
            var showTimeId = Guid.NewGuid();
            var showTime = new ShowTime { Id = showTimeId, IsDeleted = false };

            _mockShowTimeRepo.Setup(r => r.GetByIdAsync(showTimeId)).ReturnsAsync(showTime);
            _mockShowTimeRepo.Setup(r => r.SoftRemove(It.IsAny<ShowTime>())).ReturnsAsync(true);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _mockRedisService.Setup(r => r.RemoveByPatternAsync(It.IsAny<string>()))
                             .Returns(Task.CompletedTask);

            var result = await _service.SoftDeleteShowTimeAsync(showTimeId);

            Assert.True(result);
        }


        [Fact]
        public async Task GetShowTimesByDateAsync_ReturnsFromCache()
        {
            var date = DateTime.UtcNow.Date;
            var cacheKey = $"showtime:date:{date:yyyyMMdd}:movie:all:room:all";
            var cached = new List<ShowtimeResponseDTO>
            {
                new ShowtimeResponseDTO { Id = Guid.NewGuid(), ShowDate = date }
            };
            _mockRedisService.Setup(r => r.GetAsync<List<ShowtimeResponseDTO>>(cacheKey)).ReturnsAsync(cached);

            var result = await _service.GetShowTimesByDateAsync(date, null, null);

            Assert.NotNull(result);
            Assert.Single(result);
        }


        [Fact]
        public async Task GetShowTimesByMovieAndDateAsync_ReturnsFromCache()
        {
            var movieId = Guid.NewGuid();
            var date = DateTime.UtcNow.Date;
            var cacheKey = $"showtime:movie:{movieId}:date:{date:yyyyMMdd}";
            var cached = new List<ShowtimeResponseDTO>
            {
                new ShowtimeResponseDTO { Id = Guid.NewGuid(), MovieId = movieId, ShowDate = date }
            };
            _mockRedisService.Setup(r => r.GetAsync<List<ShowtimeResponseDTO>>(cacheKey)).ReturnsAsync(cached);

            var result = await _service.GetShowTimesByMovieAndDateAsync(movieId, date);

            Assert.NotNull(result);
            Assert.Single(result);
        }
    }
}