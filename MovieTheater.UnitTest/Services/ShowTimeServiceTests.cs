using MockQueryable;
using Moq;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.DTOs.ShowTimeDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Infrastructure.Interfaces;
using System.Linq.Expressions;
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
        public async Task AddBatchShowTimesAsync_ThrowsIfSpecificMovieNotFoundInDictionary()
        {
            // Arrange
            var nextWeek = DateTime.UtcNow.Date.AddDays(7 - (int)DateTime.UtcNow.DayOfWeek);
            var existingMovieId = Guid.NewGuid();
            var nonExistentMovieId = Guid.NewGuid(); // This movie won't be in the dictionary
            var roomId = Guid.NewGuid();

            var dto = new BatchShowTimeRequestDto
            {
                CinemaRoomId = roomId,
                ShowTimes = new List<BatchShowtimeRequestDto.SingleShowTimeDto>
                {
                    new() { MovieId = existingMovieId, StartTime = nextWeek.AddHours(10) },
                    new() { MovieId = nonExistentMovieId, StartTime = nextWeek.AddHours(14) } // This will cause the error
                }
            };

            // Mock cinema room exists
            _mockRoomRepo.Setup(r => r.GetByIdAsync(dto.CinemaRoomId))
                .ReturnsAsync(new CinemaRoom { Id = dto.CinemaRoomId });

            // Mock movie repository - only return one of the two requested movies
            var movies = new List<Movie>
            {
                new Movie { Id = existingMovieId, RunningTime = 120 }
                // Note: nonExistentMovieId is NOT included in this list
            };
            _mockMovieRepo.Setup(m => m.GetQueryable())
                .Returns(movies.AsQueryable().BuildMock());

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AddBatchShowTimesAsync(dto));

            Assert.Contains($"Movie {nonExistentMovieId} not found", ex.Message);
        }
        [Fact]
        public async Task AddBatchShowTimesAsync_ThrowsIfNewShowtimeOverlapsWithExisting()
        {
            // Arrange
            var nextWeek = DateTime.UtcNow.Date.AddDays(7 - (int)DateTime.UtcNow.DayOfWeek);
            var movieId = Guid.NewGuid();
            var roomId = Guid.NewGuid();

            // New showtime: 10:00 AM - 12:15 PM (120 min movie + 15 min rest)
            var newShowtimeStart = nextWeek.AddHours(10);

            var dto = new BatchShowTimeRequestDto
            {
                CinemaRoomId = roomId,
                ShowTimes = new List<BatchShowtimeRequestDto.SingleShowTimeDto>
                {
                    new() { MovieId = movieId, StartTime = newShowtimeStart }
                }
            };

            // Mock cinema room exists
            _mockRoomRepo.Setup(r => r.GetByIdAsync(dto.CinemaRoomId))
                .ReturnsAsync(new CinemaRoom { Id = dto.CinemaRoomId });

            // Mock movie with 120 minutes runtime
            var movies = new List<Movie>
            {
                new Movie { Id = movieId, RunningTime = 120 }
            };
            _mockMovieRepo.Setup(m => m.GetQueryable())
                .Returns(movies.AsQueryable().BuildMock());

            // Mock existing showtime that overlaps: 11:30 AM - 1:30 PM (120 min duration)
            // This overlaps with new showtime (10:00 AM - 12:15 PM)
            // Overlap period: 11:30 AM - 12:15 PM
            var existingShowtimeStart = nextWeek.AddHours(11).AddMinutes(30);
            var existingShowtimes = new List<ShowTime>
            {
                new ShowTime
                {
                    Id = Guid.NewGuid(),
                    CinemaRoomId = roomId,
                    ShowDate = existingShowtimeStart,
                    Duration = TimeSpan.FromMinutes(120),
                    MovieId = Guid.NewGuid(),
                    IsDeleted = false
                }
            };

            // Mock GetShowTimesByRoomAndDateAsync call (used in overlap validation)
            _mockShowTimeRepo.Setup(r => r.GetQueryable())
                .Returns(existingShowtimes.AsQueryable().BuildMock());

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AddBatchShowTimesAsync(dto));

            Assert.Contains("One or more showtimes overlap with existing showtimes in this room", ex.Message);
        }

        //[Fact]
        //public async Task AddBatchShowTimesAsync_ThrowsIfMultipleNewShowtimesOverlapWithEachOther()
        //{
        //    // Arrange
        //    var nextWeek = DateTime.UtcNow.Date.AddDays(7 - (int)DateTime.UtcNow.DayOfWeek);
        //    var movieId = Guid.NewGuid();
        //    var roomId = Guid.NewGuid();

        //    var dto = new BatchShowTimeRequestDto
        //    {
        //        CinemaRoomId = roomId,
        //        ShowTimes = new List<BatchShowtimeRequestDto.SingleShowTimeDto>
        //        {
        //            // First showtime: 10:00 AM - 12:15 PM (120 min + 15 min)
        //            new() { MovieId = movieId, StartTime = nextWeek.AddHours(10) },
        //            // Second showtime: 11:00 AM - 1:15 PM (120 min + 15 min) - overlaps with first
        //            new() { MovieId = movieId, StartTime = nextWeek.AddHours(11) }
        //        }
        //    };

        //    // Mock cinema room exists
        //    _mockRoomRepo.Setup(r => r.GetByIdAsync(dto.CinemaRoomId))
        //        .ReturnsAsync(new CinemaRoom { Id = dto.CinemaRoomId });

        //    // Mock movie with 120 minutes runtime
        //    var movies = new List<Movie>
        //    {
        //        new Movie { Id = movieId, RunningTime = 120 }
        //    };
        //    _mockMovieRepo.Setup(m => m.GetQueryable())
        //        .Returns(movies.AsQueryable().BuildMock());

        //    // Mock no existing showtimes in the room (empty list)
        //    _mockShowTimeRepo.Setup(r => r.GetQueryable())
        //        .Returns(new List<ShowTime>().AsQueryable().BuildMock());

        //    // Act & Assert
        //    var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
        //        _service.AddBatchShowTimesAsync(dto));

        //    Assert.Contains("One or more showtimes overlap with existing showtimes in this room", ex.Message);
        //}

        [Fact]
        public async Task AddBatchShowTimesAsync_SucceedsWhenNoOverlapExists()
        {
            // Arrange
            var nextWeek = DateTime.UtcNow.Date.AddDays(7 - (int)DateTime.UtcNow.DayOfWeek);
            var movieId = Guid.NewGuid();
            var roomId = Guid.NewGuid();

            var dto = new BatchShowTimeRequestDto
            {
                CinemaRoomId = roomId,
                ShowTimes = new List<BatchShowtimeRequestDto.SingleShowTimeDto>
                {
                    // New showtime: 10:00 AM - 12:15 PM (120 min + 15 min)
                    new() { MovieId = movieId, StartTime = nextWeek.AddHours(10) }
                }
            };

            // Mock cinema room exists
            _mockRoomRepo.Setup(r => r.GetByIdAsync(dto.CinemaRoomId))
                .ReturnsAsync(new CinemaRoom { Id = dto.CinemaRoomId });

            // Mock movie with 120 minutes runtime
            var movies = new List<Movie>
            {
                new Movie { Id = movieId, RunningTime = 120 }
            };
            _mockMovieRepo.Setup(m => m.GetQueryable())
                .Returns(movies.AsQueryable().BuildMock());

            // Mock existing showtime that does NOT overlap: 1:00 PM - 3:00 PM
            // No overlap with new showtime (10:00 AM - 12:15 PM)
            var existingShowtimeStart = nextWeek.AddHours(13);
            var existingShowtimes = new List<ShowTime>
            {
                new ShowTime
                {
                    Id = Guid.NewGuid(),
                    CinemaRoomId = roomId,
                    ShowDate = existingShowtimeStart,
                    Duration = TimeSpan.FromMinutes(120),
                    MovieId = Guid.NewGuid(),
                    IsDeleted = false
                }
            };

            // Mock repository operations
            _mockShowTimeRepo.Setup(r => r.GetQueryable())
                .Returns(existingShowtimes.AsQueryable().BuildMock());
            _mockShowTimeRepo.Setup(r => r.AddRangeAsync(It.IsAny<List<ShowTime>>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockAuditLogRepo.Setup(a => a.AddAsync(It.IsAny<AuditLog>()))
                .ReturnsAsync(new AuditLog());
            _mockRedisService.Setup(r => r.RemoveByPatternAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.AddBatchShowTimesAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(movieId, result[0].MovieId);
            Assert.Equal(roomId, result[0].CinemaRoomId);
            Assert.Equal(TimeSpan.FromMinutes(135), result[0].Duration); // 120 + 15 minutes
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

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateShowTimeAsync(showTimeId, new UpdateShowtimeDto()));
            Assert.Contains("An error occurred while updating the showtime", ex.Message);
            Assert.IsType<KeyNotFoundException>(ex.InnerException);
        }

        [Fact]
        public async Task UpdateShowTimeAsync_ThrowsIfMovieNotFound()
        {
            var showTimeId = Guid.NewGuid();
            var dto = new UpdateShowtimeDto { MovieId = Guid.NewGuid(), CinemaRoomId = Guid.NewGuid(), ShowDate = DateTime.UtcNow.AddDays(1) };
            _mockShowTimeRepo.Setup(r => r.GetByIdAsync(showTimeId)).ReturnsAsync(new ShowTime { IsDeleted = false });
            _mockMovieRepo.Setup(m => m.GetByIdAsync(dto.MovieId)).ReturnsAsync((Movie)null!);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateShowTimeAsync(showTimeId, dto));
            Assert.Contains("An error occurred while updating the showtime", ex.Message);
            Assert.IsType<KeyNotFoundException>(ex.InnerException);
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
        public async Task UpdateShowTimeAsync_ThrowsIfOverlapWithSpecificTimeWindows()
        {
            // Arrange
            var showTimeId = Guid.NewGuid();
            var movieId = Guid.NewGuid();
            var roomId = Guid.NewGuid();
            var baseDate = DateTime.UtcNow.Date.AddDays(1);

            // Update showtime: 2:00 PM - 4:45 PM (150 min movie + 15 min rest)
            var dto = new UpdateShowtimeDto
            {
                MovieId = movieId,
                CinemaRoomId = roomId,
                ShowDate = baseDate.AddHours(14), // 2:00 PM
            };

            // Existing showtime that overlaps: 3:30 PM - 6:00 PM (150 min duration)
            // Overlap period: 3:30 PM - 4:45 PM
            var existingShowTime = new ShowTime
            {
                Id = Guid.NewGuid(),
                CinemaRoomId = roomId,
                ShowDate = baseDate.AddHours(15).AddMinutes(30), // 3:30 PM
                Duration = TimeSpan.FromMinutes(150),
                IsDeleted = false
            };

            _mockShowTimeRepo.Setup(r => r.GetByIdAsync(showTimeId))
                .ReturnsAsync(new ShowTime { Id = showTimeId, IsDeleted = false });

            _mockMovieRepo.Setup(m => m.GetByIdAsync(movieId))
                .ReturnsAsync(new Movie { Id = movieId, RunningTime = 150 });

            _mockRoomRepo.Setup(r => r.GetByIdAsync(roomId))
                .ReturnsAsync(new CinemaRoom { Id = roomId });

            _mockShowTimeRepo.Setup(r => r.GetQueryable())
                .Returns(new List<ShowTime> { existingShowTime }.AsQueryable().BuildMock());

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateShowTimeAsync(showTimeId, dto));

            // The outer catch block wraps the original exception, so we need to check the outer message
            Assert.Contains("An error occurred while updating the showtime", ex.Message);
            // And check that the inner exception contains the specific overlap message
            Assert.NotNull(ex.InnerException);
            Assert.IsType<InvalidOperationException>(ex.InnerException);
            Assert.Contains("The new showtime overlaps with another showtime in this room", ex.InnerException.Message);
        }

        [Fact]
        public async Task UpdateShowTimeAsync_SucceedsWhenNoOverlapExists()
        {
            // Arrange
            var showTimeId = Guid.NewGuid();
            var movieId = Guid.NewGuid();
            var roomId = Guid.NewGuid();
            var baseDate = DateTime.UtcNow.Date.AddDays(1);

            // Update showtime: 2:00 PM - 4:15 PM (120 min movie + 15 min rest)
            var dto = new UpdateShowtimeDto
            {
                MovieId = movieId,
                CinemaRoomId = roomId,
                ShowDate = baseDate.AddHours(14), // 2:00 PM
            };

            var existingShowTime = new ShowTime
            {
                Id = showTimeId,
                MovieId = Guid.NewGuid(),
                CinemaRoomId = Guid.NewGuid(),
                ShowDate = DateTime.UtcNow,
                Duration = TimeSpan.FromMinutes(100),
                IsDeleted = false
            };

            // Existing showtime that does NOT overlap: 5:00 PM - 7:00 PM (120 min duration)
            // No overlap with updated showtime (2:00 PM - 4:15 PM)
            var nonOverlappingShowTime = new ShowTime
            {
                Id = Guid.NewGuid(),
                CinemaRoomId = roomId,
                ShowDate = baseDate.AddHours(17), // 5:00 PM
                Duration = TimeSpan.FromMinutes(120),
                IsDeleted = false
            };

            _mockShowTimeRepo.Setup(r => r.GetByIdAsync(showTimeId))
                .ReturnsAsync(existingShowTime);

            _mockMovieRepo.Setup(m => m.GetByIdAsync(movieId))
                .ReturnsAsync(new Movie { Id = movieId, RunningTime = 120 });

            _mockRoomRepo.Setup(r => r.GetByIdAsync(roomId))
                .ReturnsAsync(new CinemaRoom { Id = roomId });

            // Mock only non-overlapping showtimes in the same room
            _mockShowTimeRepo.Setup(r => r.GetQueryable())
                .Returns(new List<ShowTime> { nonOverlappingShowTime }.AsQueryable().BuildMock());

            _mockShowTimeRepo.Setup(r => r.Update(It.IsAny<ShowTime>())).ReturnsAsync(true);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockRedisService.Setup(r => r.RemoveByPatternAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateShowTimeAsync(showTimeId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(showTimeId, result.Id);
            Assert.Equal(movieId, result.MovieId);
            Assert.Equal(roomId, result.CinemaRoomId);
            Assert.Equal(baseDate.AddHours(14), result.ShowDate);
            Assert.Equal(TimeSpan.FromMinutes(135), result.Duration); // 120 + 15 minutes
        }

        [Fact]
        public async Task UpdateShowTimeAsync_IgnoresOverlapWithSameShowtime()
        {
            // Arrange - Test that a showtime doesn't overlap with itself
            var showTimeId = Guid.NewGuid();
            var movieId = Guid.NewGuid();
            var roomId = Guid.NewGuid();
            var baseDate = DateTime.UtcNow.Date.AddDays(1);

            var dto = new UpdateShowtimeDto
            {
                MovieId = movieId,
                CinemaRoomId = roomId,
                ShowDate = baseDate.AddHours(14), // 2:00 PM
            };

            var existingShowTime = new ShowTime
            {
                Id = showTimeId,
                MovieId = Guid.NewGuid(),
                CinemaRoomId = Guid.NewGuid(),
                ShowDate = DateTime.UtcNow,
                Duration = TimeSpan.FromMinutes(100),
                IsDeleted = false
            };

            // Same showtime being updated - should be ignored in overlap check
            var sameShowTime = new ShowTime
            {
                Id = showTimeId, // Same ID as the one being updated
                CinemaRoomId = roomId,
                ShowDate = baseDate.AddHours(14), // Same time
                Duration = TimeSpan.FromMinutes(135),
                IsDeleted = false
            };

            _mockShowTimeRepo.Setup(r => r.GetByIdAsync(showTimeId))
                .ReturnsAsync(existingShowTime);

            _mockMovieRepo.Setup(m => m.GetByIdAsync(movieId))
                .ReturnsAsync(new Movie { Id = movieId, RunningTime = 120 });

            _mockRoomRepo.Setup(r => r.GetByIdAsync(roomId))
                .ReturnsAsync(new CinemaRoom { Id = roomId });

            // Mock includes the same showtime - should be filtered out by the query
            _mockShowTimeRepo.Setup(r => r.GetQueryable())
                .Returns(new List<ShowTime> { sameShowTime }.AsQueryable().BuildMock());

            _mockShowTimeRepo.Setup(r => r.Update(It.IsAny<ShowTime>())).ReturnsAsync(true);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockRedisService.Setup(r => r.RemoveByPatternAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateShowTimeAsync(showTimeId, dto);

            // Assert - Should succeed because it ignores overlap with itself
            Assert.NotNull(result);
            Assert.Equal(showTimeId, result.Id);
        }

        [Fact]
        public async Task UpdateShowTimeAsync_IgnoresDeletedShowtimes()
        {
            // Arrange
            var showTimeId = Guid.NewGuid();
            var movieId = Guid.NewGuid();
            var roomId = Guid.NewGuid();
            var baseDate = DateTime.UtcNow.Date.AddDays(1);

            var dto = new UpdateShowtimeDto
            {
                MovieId = movieId,
                CinemaRoomId = roomId,
                ShowDate = baseDate.AddHours(14), // 2:00 PM
            };

            var existingShowTime = new ShowTime
            {
                Id = showTimeId,
                MovieId = Guid.NewGuid(),
                CinemaRoomId = Guid.NewGuid(),
                ShowDate = DateTime.UtcNow,
                Duration = TimeSpan.FromMinutes(100),
                IsDeleted = false
            };

            // Deleted showtime that would overlap - should be ignored
            var deletedShowTime = new ShowTime
            {
                Id = Guid.NewGuid(),
                CinemaRoomId = roomId,
                ShowDate = baseDate.AddHours(14), // Same time - would overlap
                Duration = TimeSpan.FromMinutes(120),
                IsDeleted = true // This is deleted, so should be ignored
            };

            _mockShowTimeRepo.Setup(r => r.GetByIdAsync(showTimeId))
                .ReturnsAsync(existingShowTime);

            _mockMovieRepo.Setup(m => m.GetByIdAsync(movieId))
                .ReturnsAsync(new Movie { Id = movieId, RunningTime = 120 });

            _mockRoomRepo.Setup(r => r.GetByIdAsync(roomId))
                .ReturnsAsync(new CinemaRoom { Id = roomId });

            // Mock includes deleted showtime - should be filtered out by the query
            _mockShowTimeRepo.Setup(r => r.GetQueryable())
                .Returns(new List<ShowTime> { deletedShowTime }.AsQueryable().BuildMock());

            _mockShowTimeRepo.Setup(r => r.Update(It.IsAny<ShowTime>())).ReturnsAsync(true);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockRedisService.Setup(r => r.RemoveByPatternAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateShowTimeAsync(showTimeId, dto);

            // Assert - Should succeed because deleted showtimes are ignored
            Assert.NotNull(result);
            Assert.Equal(showTimeId, result.Id);
        }

        [Fact]
        public async Task UpdateShowTimeAsync_OnlyChecksOverlapInSameRoom()
        {
            // Arrange
            var showTimeId = Guid.NewGuid();
            var movieId = Guid.NewGuid();
            var roomId = Guid.NewGuid();
            var otherRoomId = Guid.NewGuid();
            var baseDate = DateTime.UtcNow.Date.AddDays(1);

            var dto = new UpdateShowtimeDto
            {
                MovieId = movieId,
                CinemaRoomId = roomId,
                ShowDate = baseDate.AddHours(14), // 2:00 PM
            };

            var existingShowTime = new ShowTime
            {
                Id = showTimeId,
                MovieId = Guid.NewGuid(),
                CinemaRoomId = Guid.NewGuid(),
                ShowDate = DateTime.UtcNow,
                Duration = TimeSpan.FromMinutes(100),
                IsDeleted = false
            };

            // Showtime in different room at same time - should NOT cause overlap
            var showTimeInDifferentRoom = new ShowTime
            {
                Id = Guid.NewGuid(),
                CinemaRoomId = otherRoomId, // Different room
                ShowDate = baseDate.AddHours(14), // Same time
                Duration = TimeSpan.FromMinutes(120),
                IsDeleted = false
            };

            _mockShowTimeRepo.Setup(r => r.GetByIdAsync(showTimeId))
                .ReturnsAsync(existingShowTime);

            _mockMovieRepo.Setup(m => m.GetByIdAsync(movieId))
                .ReturnsAsync(new Movie { Id = movieId, RunningTime = 120 });

            _mockRoomRepo.Setup(r => r.GetByIdAsync(roomId))
                .ReturnsAsync(new CinemaRoom { Id = roomId });

            // Mock includes showtime from different room - should be filtered out by the query
            _mockShowTimeRepo.Setup(r => r.GetQueryable())
                .Returns(new List<ShowTime> { showTimeInDifferentRoom }.AsQueryable().BuildMock());

            _mockShowTimeRepo.Setup(r => r.Update(It.IsAny<ShowTime>())).ReturnsAsync(true);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockRedisService.Setup(r => r.RemoveByPatternAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateShowTimeAsync(showTimeId, dto);

            // Assert - Should succeed because overlap only checked within same room
            Assert.NotNull(result);
            Assert.Equal(showTimeId, result.Id);
            Assert.Equal(roomId, result.CinemaRoomId);
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
        [Fact]
        public async Task GetShowTimesByDateAsync_ThrowsInvalidOperationExceptionWhenDatabaseErrorOccurs()
        {
            // Arrange
            var date = DateTime.UtcNow.Date;
            var movieId = Guid.NewGuid();
            var roomId = Guid.NewGuid();
            var expectedErrorMessage = "Database connection failed";

            // Mock Redis to return null (cache miss)
            _mockRedisService.Setup(r => r.GetAsync<List<ShowtimeResponseDTO>>(It.IsAny<string>()))
                .ReturnsAsync((List<ShowtimeResponseDTO>)null!);

            // Mock repository to throw an exception when querying
            _mockShowTimeRepo.Setup(r => r.GetQueryable())
                .Throws(new InvalidOperationException(expectedErrorMessage));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GetShowTimesByDateAsync(date, movieId, roomId));

            // Verify the outer exception message
            Assert.Equal("An error occurred while retrieving showtimes.", ex.Message);

            // Verify the inner exception contains the original error
            Assert.NotNull(ex.InnerException);
            Assert.Equal(expectedErrorMessage, ex.InnerException.Message);

            // Verify that error logging was called
            _mockLoggerService.Verify(
                l => l.Error(It.Is<string>(msg => msg.Contains("[GetShowTimesByDateAsync] Error:") && msg.Contains(expectedErrorMessage))),
                Times.Once);

            // Verify Redis was called for cache retrieval
            _mockRedisService.Verify(
                r => r.GetAsync<List<ShowtimeResponseDTO>>(It.IsAny<string>()),
                Times.Once);

            // Verify that Redis SetAsync was not called since exception occurred
            _mockRedisService.Verify(
                r => r.SetAsync(It.IsAny<string>(), It.IsAny<List<ShowtimeResponseDTO>>(), It.IsAny<TimeSpan>()),
                Times.Never);
        }

        [Fact]
        public async Task AddBatchShowTimesAsync_SuccessfullyAddsShowTimes()
        {
            // Arrange
            var nextWeek = DateTime.UtcNow.Date.AddDays(7 - (int)DateTime.UtcNow.DayOfWeek);
            var movieId = Guid.NewGuid();
            var roomId = Guid.NewGuid();
            var dto = new BatchShowTimeRequestDto
            {
                CinemaRoomId = roomId,
                ShowTimes = new List<BatchShowtimeRequestDto.SingleShowTimeDto>
                {
                    new() { MovieId = movieId, StartTime = nextWeek.AddHours(10) },
                    new() { MovieId = movieId, StartTime = nextWeek.AddHours(14) }
                }
            };

            // Mock cinema room
            _mockRoomRepo.Setup(r => r.GetByIdAsync(dto.CinemaRoomId))
                .ReturnsAsync(new CinemaRoom { Id = dto.CinemaRoomId });

            // Mock movie repository - this is used in the validation and creation phases
            var movies = new List<Movie>
            {
                new Movie { Id = movieId, RunningTime = 120 }
            };
            _mockMovieRepo.Setup(m => m.GetQueryable())
                .Returns(movies.AsQueryable().BuildMock());

            // Mock ShowTime repository for overlap validation
            // This is critical - GetShowTimesByRoomAndDateAsync uses GetQueryable() directly
            var existingShowTimes = new List<ShowTime>(); // Empty list = no existing showtimes = no overlaps
            _mockShowTimeRepo.Setup(r => r.GetQueryable())
                .Returns(existingShowTimes.AsQueryable().BuildMock());

            // Mock repository operations
            _mockShowTimeRepo.Setup(r => r.AddRangeAsync(It.IsAny<List<ShowTime>>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);
            _mockAuditLogRepo.Setup(a => a.AddAsync(It.IsAny<AuditLog>()))
                .ReturnsAsync(new AuditLog());

            // Mock Redis operations
            _mockRedisService.Setup(r => r.RemoveByPatternAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.AddBatchShowTimesAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, st =>
            {
                Assert.Equal(movieId, st.MovieId);
                Assert.Equal(dto.CinemaRoomId, st.CinemaRoomId);
                Assert.Equal(TimeSpan.FromMinutes(135), st.Duration); // 120 + 15 minutes
            });

            // Verify repository calls
            _mockShowTimeRepo.Verify(r => r.AddRangeAsync(It.Is<List<ShowTime>>(st => st.Count == 2)), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Exactly(2)); // Once for showtimes, once for audit log
            _mockAuditLogRepo.Verify(a => a.AddAsync(It.IsAny<AuditLog>()), Times.Once);

            // Verify Redis cache invalidation calls
            _mockRedisService.Verify(r => r.RemoveByPatternAsync(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task UpdateShowTimeAsync_SuccessfullyUpdatesShowTime()
        {
            // Arrange
            var showTimeId = Guid.NewGuid();
            var movieId = Guid.NewGuid();
            var roomId = Guid.NewGuid();
            var newShowDate = DateTime.UtcNow.AddDays(1);

            var dto = new UpdateShowtimeDto
            {
                MovieId = movieId,
                CinemaRoomId = roomId,
                ShowDate = newShowDate
            };

            var existingShowTime = new ShowTime
            {
                Id = showTimeId,
                MovieId = Guid.NewGuid(),
                CinemaRoomId = Guid.NewGuid(),
                ShowDate = DateTime.UtcNow,
                Duration = TimeSpan.FromMinutes(100),
                IsDeleted = false
            };

            _mockShowTimeRepo.Setup(r => r.GetByIdAsync(showTimeId)).ReturnsAsync(existingShowTime);
            _mockMovieRepo.Setup(m => m.GetByIdAsync(movieId)).ReturnsAsync(new Movie { Id = movieId, RunningTime = 150 });
            _mockRoomRepo.Setup(r => r.GetByIdAsync(roomId)).ReturnsAsync(new CinemaRoom { Id = roomId });

            // Mock no overlapping showtimes
            _mockShowTimeRepo.Setup(r => r.GetQueryable()).Returns(new List<ShowTime>().AsQueryable().BuildMock());
            _mockShowTimeRepo.Setup(r => r.Update(It.IsAny<ShowTime>())).ReturnsAsync(true);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.UpdateShowTimeAsync(showTimeId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(showTimeId, result.Id);
            Assert.Equal(movieId, result.MovieId);
            Assert.Equal(roomId, result.CinemaRoomId);
            Assert.Equal(newShowDate, result.ShowDate);
            Assert.Equal(TimeSpan.FromMinutes(165), result.Duration); // 150 + 15 minutes

            _mockShowTimeRepo.Verify(r => r.Update(It.IsAny<ShowTime>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetShowTimesByDateAsync_FetchesFromDatabaseWhenCacheMiss()
        {
            // Arrange
            var date = DateTime.UtcNow.Date;
            var movieId = Guid.NewGuid();
            var roomId = Guid.NewGuid();
            var showTimes = new List<ShowTime>
            {
                new ShowTime
                {
                    Id = Guid.NewGuid(),
                    MovieId = movieId,
                    CinemaRoomId = roomId,
                    ShowDate = date.AddHours(10),
                    Duration = TimeSpan.FromMinutes(120),
                    IsDeleted = false
                }
            };

            _mockRedisService.Setup(r => r.GetAsync<List<ShowtimeResponseDTO>>(It.IsAny<string>()))
                .ReturnsAsync((List<ShowtimeResponseDTO>)null!);
            _mockShowTimeRepo.Setup(r => r.GetQueryable()).Returns(showTimes.AsQueryable().BuildMock());
            _mockRedisService.Setup(r => r.SetAsync(It.IsAny<string>(), It.IsAny<List<ShowtimeResponseDTO>>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.GetShowTimesByDateAsync(date, movieId, roomId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(movieId, result[0].MovieId);
            Assert.Equal(roomId, result[0].CinemaRoomId);

            _mockRedisService.Verify(r => r.SetAsync(It.IsAny<string>(), It.IsAny<List<ShowtimeResponseDTO>>(), TimeSpan.FromMinutes(5)), Times.Once);
        }

        [Fact]
        public async Task GetShowTimesByDateAsync_ReturnsEmptyListWhenNoShowTimesFound()
        {
            // Arrange
            var date = DateTime.UtcNow.Date;

            _mockRedisService.Setup(r => r.GetAsync<List<ShowtimeResponseDTO>>(It.IsAny<string>()))
                .ReturnsAsync((List<ShowtimeResponseDTO>)null!);
            _mockShowTimeRepo.Setup(r => r.GetQueryable()).Returns(new List<ShowTime>().AsQueryable().BuildMock());

            // Act
            var result = await _service.GetShowTimesByDateAsync(date, null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetShowTimesByMovieAndDateAsync_FetchesFromDatabaseWhenCacheMiss()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var date = DateTime.UtcNow.Date;
            var showTimes = new List<ShowTime>
            {
                new ShowTime
                {
                    Id = Guid.NewGuid(),
                    MovieId = movieId,
                    CinemaRoomId = Guid.NewGuid(),
                    ShowDate = date.AddHours(15),
                    Duration = TimeSpan.FromMinutes(90),
                    IsDeleted = false
                }
            };

            _mockRedisService.Setup(r => r.GetAsync<List<ShowtimeResponseDTO>>(It.IsAny<string>()))
                .ReturnsAsync((List<ShowtimeResponseDTO>)null!);
            _mockShowTimeRepo.Setup(r => r.GetQueryable()).Returns(showTimes.AsQueryable().BuildMock());
            _mockRedisService.Setup(r => r.SetAsync(It.IsAny<string>(), It.IsAny<List<ShowtimeResponseDTO>>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.GetShowTimesByMovieAndDateAsync(movieId, date);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(movieId, result[0].MovieId);

            _mockRedisService.Verify(r => r.SetAsync(It.IsAny<string>(), It.IsAny<List<ShowtimeResponseDTO>>(), TimeSpan.FromMinutes(5)), Times.Once);
        }
        [Fact]
        public async Task GetShowTimesByMovieAndDateAsync_ThrowsInvalidOperationExceptionWhenDatabaseErrorOccurs()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var date = DateTime.UtcNow.Date;
            var expectedErrorMessage = "Database connection failed";

            // Mock Redis to return null (cache miss)
            _mockRedisService.Setup(r => r.GetAsync<List<ShowtimeResponseDTO>>(It.IsAny<string>()))
                .ReturnsAsync((List<ShowtimeResponseDTO>)null!);

            // Mock repository to throw an exception when querying
            _mockShowTimeRepo.Setup(r => r.GetQueryable())
                .Throws(new InvalidOperationException(expectedErrorMessage));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GetShowTimesByMovieAndDateAsync(movieId, date));

            // Verify the outer exception message
            Assert.Equal("An error occurred while retrieving showtimes.", ex.Message);

            // Verify the inner exception contains the original error
            Assert.NotNull(ex.InnerException);
            Assert.Equal(expectedErrorMessage, ex.InnerException.Message);

            // Verify that error logging was called with the correct method name
            _mockLoggerService.Verify(
                l => l.Error(It.Is<string>(msg => msg.Contains("[GetShowTimesByMovieAndDateAsync] Error:") && msg.Contains(expectedErrorMessage))),
                Times.Once);

            // Verify Redis was called for cache retrieval
            _mockRedisService.Verify(
                r => r.GetAsync<List<ShowtimeResponseDTO>>(It.IsAny<string>()),
                Times.Once);

            // Verify that Redis SetAsync was not called since exception occurred
            _mockRedisService.Verify(
                r => r.SetAsync(It.IsAny<string>(), It.IsAny<List<ShowtimeResponseDTO>>(), It.IsAny<TimeSpan>()),
                Times.Never);
        }

        [Fact]
        public async Task GetShowTimesByMovieAndDateAsync_ReturnsEmptyListWhenNoShowTimesFound()
        {
            // Arrange
            var movieId = Guid.NewGuid();

            _mockRedisService.Setup(r => r.GetAsync<List<ShowtimeResponseDTO>>(It.IsAny<string>()))
                .ReturnsAsync((List<ShowtimeResponseDTO>)null!);
            _mockShowTimeRepo.Setup(r => r.GetQueryable()).Returns(new List<ShowTime>().AsQueryable().BuildMock());

            // Act
            var result = await _service.GetShowTimesByMovieAndDateAsync(movieId, DateTime.UtcNow.Date);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task UpdateShowTimeAsync_ThrowsIfCinemaRoomNotFound()
        {
            // Arrange
            var showTimeId = Guid.NewGuid();
            var dto = new UpdateShowtimeDto
            {
                MovieId = Guid.NewGuid(),
                CinemaRoomId = Guid.NewGuid(),
                ShowDate = DateTime.UtcNow.AddDays(1)
            };

            _mockShowTimeRepo.Setup(r => r.GetByIdAsync(showTimeId)).ReturnsAsync(new ShowTime { IsDeleted = false });
            _mockMovieRepo.Setup(m => m.GetByIdAsync(dto.MovieId)).ReturnsAsync(new Movie { RunningTime = 120 });
            _mockRoomRepo.Setup(r => r.GetByIdAsync(dto.CinemaRoomId)).ReturnsAsync((CinemaRoom)null!);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateShowTimeAsync(showTimeId, dto));
            Assert.Contains("An error occurred while updating the showtime", ex.Message);
            Assert.IsType<KeyNotFoundException>(ex.InnerException);
        }
    }
}