using Microsoft.EntityFrameworkCore;
using Moq;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.DTOs.MovieDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System.Linq.Expressions;

namespace MovieTheater.UnitTest.Services
{
    public class MovieServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILoggerService> _mockLoggerService;
        private readonly Mock<IClaimsService> _mockClaimsService;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<IRedisService> _mockRedisService;
        private readonly Mock<IGenericRepository<Movie>> _mockMovieRepository;
        private readonly MovieService _movieService;
        private readonly Guid _currentUserId = Guid.NewGuid();

        public MovieServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLoggerService = new Mock<ILoggerService>();
            _mockClaimsService = new Mock<IClaimsService>();
            _mockAuditLogService = new Mock<IAuditLogService>();
            _mockRedisService = new Mock<IRedisService>();
            _mockMovieRepository = new Mock<IGenericRepository<Movie>>();

            // Setup UnitOfWork to return movie repository
            _mockUnitOfWork.Setup(uow => uow.Movies).Returns(_mockMovieRepository.Object);

            // Setup ClaimsService to return current user id
            _mockClaimsService.Setup(s => s.GetCurrentUserId).Returns(_currentUserId);

            _movieService = new MovieService(
                _mockUnitOfWork.Object,
                _mockLoggerService.Object,
                _mockClaimsService.Object,
                _mockAuditLogService.Object,
                _mockRedisService.Object
            );
        }

        #region GetAllMoviesAsync Tests

        [Fact]
        public async Task GetAllMoviesAsync_WithCachedResult_ReturnsCachedPagination()
        {
            // Arrange
            string search = "action";
            string sortBy = "name";
            bool isDescending = false;
            int page = 1;
            int pageSize = 10;
            var genres = new List<string> { "Action", "Comedy" };
            MovieStatus? status = MovieStatus.NowShowing;
            string genreKey = string.Join(",", genres.OrderBy(g => g));

            string cacheKey = $"movie:list:{search}:{sortBy}:{isDescending}:{page}:{pageSize}:{genreKey}:{status}";

            var cachedResult = new Pagination<MovieResponseDto>(
                new List<MovieResponseDto>
                {
                    new MovieResponseDto { Id = Guid.NewGuid(), Name = "Action Movie 1" },
                    new MovieResponseDto { Id = Guid.NewGuid(), Name = "Action Movie 2" }
                },
                2, page, pageSize);

            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<MovieResponseDto>>(cacheKey))
                .ReturnsAsync(cachedResult);

            // Act
            var result = await _movieService.GetAllMoviesAsync(search, sortBy, isDescending, page, pageSize, genres, status);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal("Action Movie 1", result.Items[0].Name);

            _mockRedisService.Verify(redis => redis.GetAsync<Pagination<MovieResponseDto>>(cacheKey), Times.Once);
            _mockMovieRepository.Verify(repo => repo.GetAllAsync(It.IsAny<Expression<Func<Movie, bool>>>(), It.IsAny<Expression<Func<Movie, object>>[]>()), Times.Never);
            _mockLoggerService.Verify(log => log.Info(It.Is<string>(s => s.Contains("[CACHE HIT]"))), Times.Once);
        }

        [Fact]
        public async Task GetAllMoviesAsync_WithoutCache_ReturnsPaginationFromDatabase()
        {
            // Arrange
            string search = "action";
            string sortBy = "name";
            bool isDescending = false;
            int page = 1;
            int pageSize = 10;

            var movies = new List<Movie>
            {
                new Movie {
                    Id = Guid.NewGuid(),
                    Name = "Action Movie",
                    Genres = new List<string> { "Action" },
                    Status = MovieStatus.NowShowing,
                    IsDeleted = false
                },
                new Movie {
                    Id = Guid.NewGuid(),
                    Name = "Another Action",
                    Genres = new List<string> { "Action", "Adventure" },
                    Status = MovieStatus.NowShowing,
                    IsDeleted = false
                },
                new Movie {
                    Id = Guid.NewGuid(),
                    Name = "Comedy Film",
                    Genres = new List<string> { "Comedy" },
                    Status = MovieStatus.NowShowing,
                    IsDeleted = false
                }
            };

            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<MovieResponseDto>>(It.IsAny<string>()))
                .ReturnsAsync((Pagination<MovieResponseDto>)null!);

            _mockMovieRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<Movie, bool>>>(), It.IsAny<Expression<Func<Movie, object>>[]>()))
                .ReturnsAsync(movies);

            // Act
            var result = await _movieService.GetAllMoviesAsync(search, sortBy, isDescending, page, pageSize);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);

            _mockRedisService.Verify(redis => redis.GetAsync<Pagination<MovieResponseDto>>(It.IsAny<string>()), Times.Once);
            _mockRedisService.Verify(redis => redis.SetAsync(
                It.IsAny<string>(), It.IsAny<Pagination<MovieResponseDto>>(), It.IsAny<TimeSpan>()), Times.Once);
            _mockMovieRepository.Verify(repo => repo.GetAllAsync(It.IsAny<Expression<Func<Movie, bool>>>(), It.IsAny<Expression<Func<Movie, object>>[]>()), Times.Once);
            _mockLoggerService.Verify(log => log.Info(It.Is<string>(s => s.Contains("[CACHE MISS]"))), Times.Once);
        }

        [Fact]
        public async Task GetAllMoviesAsync_WithGenresFilter_ReturnsFilteredMovies()
        {
            // Arrange
            string search = null!;
            string sortBy = "name";
            bool isDescending = false;
            int page = 1;
            int pageSize = 10;
            var genres = new List<string> { "Action" };

            var movies = new List<Movie>
            {
                new Movie {
                    Id = Guid.NewGuid(),
                    Name = "Action Movie",
                    Genres = new List<string> { "Action" },
                    IsDeleted = false
                },
                new Movie {
                    Id = Guid.NewGuid(),
                    Name = "Comedy Movie",
                    Genres = new List<string> { "Comedy" },
                    IsDeleted = false
                }
            };

            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<MovieResponseDto>>(It.IsAny<string>()))
                .ReturnsAsync((Pagination<MovieResponseDto>)null!);

            _mockMovieRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<Movie, bool>>>(), It.IsAny<Expression<Func<Movie, object>>[]>()))
                .ReturnsAsync(movies);

            // Act
            var result = await _movieService.GetAllMoviesAsync(search, sortBy, isDescending, page, pageSize, genres);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal("Action Movie", result.Items[0].Name);
        }

        [Fact]
        public async Task GetAllMoviesAsync_WithStatusFilter_ReturnsFilteredMovies()
        {
            // Arrange
            string search = null!;
            string sortBy = "fromdate";
            bool isDescending = true;
            int page = 1;
            int pageSize = 10;
            MovieStatus? status = MovieStatus.NowShowing;

            var movies = new List<Movie>
            {
                new Movie {
                    Id = Guid.NewGuid(),
                    Name = "Now Showing Movie",
                    Status = MovieStatus.NowShowing,
                    FromDate = DateTime.Now.AddDays(-5),
                    IsDeleted = false
                },
                new Movie {
                    Id = Guid.NewGuid(),
                    Name = "Coming Soon Movie",
                    Status = MovieStatus.ComingSoon,
                    FromDate = DateTime.Now.AddDays(5),
                    IsDeleted = false
                }
            };

            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<MovieResponseDto>>(It.IsAny<string>()))
                .ReturnsAsync((Pagination<MovieResponseDto>)null!);

            _mockMovieRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<Movie, bool>>>(), It.IsAny<Expression<Func<Movie, object>>[]>()))
                .ReturnsAsync(movies);

            // Act
            var result = await _movieService.GetAllMoviesAsync(search, sortBy, isDescending, page, pageSize, null, status);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal("Now Showing Movie", result.Items[0].Name);
        }

        [Fact]
        public async Task GetAllMoviesAsync_SortsByToDate_ReturnsSortedMovies()
        {
            // Arrange
            string search = null!;
            string sortBy = "todate";
            bool isDescending = false;
            int page = 1;
            int pageSize = 10;

            var movies = new List<Movie>
            {
                new Movie {
                    Id = Guid.NewGuid(),
                    Name = "Movie 1",
                    ToDate = DateTime.Now.AddDays(20),
                    IsDeleted = false
                },
                new Movie {
                    Id = Guid.NewGuid(),
                    Name = "Movie 2",
                    ToDate = DateTime.Now.AddDays(10),
                    IsDeleted = false
                }
            };

            _mockRedisService.Setup(redis => redis.GetAsync<Pagination<MovieResponseDto>>(It.IsAny<string>()))
                .ReturnsAsync((Pagination<MovieResponseDto>)null!);

            _mockMovieRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<Movie, bool>>>(), It.IsAny<Expression<Func<Movie, object>>[]>()))
                .ReturnsAsync(movies);

            // Act
            var result = await _movieService.GetAllMoviesAsync(search, sortBy, isDescending, page, pageSize);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal("Movie 2", result.Items[0].Name); // Movie with earlier ToDate
        }

        [Fact]
        public async Task GetAllMoviesAsync_WhenExceptionOccurs_ThrowsException()
        {
            // Arrange
            _mockMovieRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<Movie, bool>>>(), It.IsAny<Expression<Func<Movie, object>>[]>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _movieService.GetAllMoviesAsync(null, null, false, 1, 10));

            Assert.Contains("An error occurred while retrieving movies", exception.Message);
            _mockLoggerService.Verify(log => log.Error(It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region GetMovieDetailAsync Tests

        [Fact]
        public async Task GetMovieDetailAsync_WithCachedResult_ReturnsMovieDto()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var cacheKey = $"movie:detail:{movieId}";
            var cachedMovie = new MovieResponseDto
            {
                Id = movieId,
                Name = "Cached Movie",
                Director = "Cached Director"
            };

            _mockRedisService.Setup(redis => redis.GetAsync<MovieResponseDto>(cacheKey))
                .ReturnsAsync(cachedMovie);

            // Act
            var result = await _movieService.GetMovieDetailAsync(movieId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(cachedMovie.Name, result.Name);
            Assert.Equal(cachedMovie.Director, result.Director);

            _mockRedisService.Verify(redis => redis.GetAsync<MovieResponseDto>(cacheKey), Times.Once);
            _mockMovieRepository.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockLoggerService.Verify(log => log.Info(It.Is<string>(s => s.Contains("[CACHE HIT]"))), Times.Once);
        }

        [Fact]
        public async Task GetMovieDetailAsync_WithoutCache_ReturnsFromDatabase()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var movie = new Movie
            {
                Id = movieId,
                Name = "Database Movie",
                Director = "Database Director",
                Actors = new List<string> { "Actor 1", "Actor 2" },
                ActorsUrl = new List<string> { "url1", "url2" },
                RunningTime = 120,
                IsDeleted = false
            };

            _mockRedisService.Setup(redis => redis.GetAsync<MovieResponseDto>(It.IsAny<string>()))
                .ReturnsAsync((MovieResponseDto)null!);

            _mockMovieRepository.Setup(repo => repo.GetByIdAsync(movieId))
                .ReturnsAsync(movie);

            // Act
            var result = await _movieService.GetMovieDetailAsync(movieId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(movie.Name, result.Name);
            Assert.Equal(movie.Director, result.Director);
            Assert.Equal(movie.Actors, result.Actors);
            Assert.Equal(movie.ActorsUrl, result.ActorsUrl);
            Assert.Equal(movie.RunningTime, result.RunningTime);

            _mockRedisService.Verify(redis => redis.GetAsync<MovieResponseDto>(It.IsAny<string>()), Times.Once);
            _mockRedisService.Verify(redis => redis.SetAsync(
                It.IsAny<string>(), It.IsAny<MovieResponseDto>(), It.IsAny<TimeSpan>()), Times.Once);
            _mockMovieRepository.Verify(repo => repo.GetByIdAsync(movieId), Times.Once);
            _mockLoggerService.Verify(log => log.Info(It.Is<string>(s => s.Contains("[CACHE MISS]"))), Times.Once);
        }

        [Fact]
        public async Task GetMovieDetailAsync_WithNonExistentMovie_ThrowsException()
        {
            // Arrange
            var movieId = Guid.NewGuid();

            _mockRedisService.Setup(redis => redis.GetAsync<MovieResponseDto>(It.IsAny<string>()))
                .ReturnsAsync((MovieResponseDto)null!);

            _mockMovieRepository.Setup(repo => repo.GetByIdAsync(movieId))
                .ReturnsAsync((Movie)null!);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _movieService.GetMovieDetailAsync(movieId));

            Assert.Contains("An error occurred while fetching movie details. Please try again later.", exception.Message);
            _mockLoggerService.Verify(log => log.Warn(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetMovieDetailAsync_WithDeletedMovie_ThrowsException()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var movie = new Movie
            {
                Id = movieId,
                Name = "Deleted Movie",
                IsDeleted = true
            };

            _mockRedisService.Setup(redis => redis.GetAsync<MovieResponseDto>(It.IsAny<string>()))
                .ReturnsAsync((MovieResponseDto)null!);

            _mockMovieRepository.Setup(repo => repo.GetByIdAsync(movieId))
                .ReturnsAsync(movie);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _movieService.GetMovieDetailAsync(movieId));

            Assert.Contains("An error occurred while fetching movie details. Please try again later.", exception.Message);
        }

        [Fact]
        public async Task GetMovieDetailAsync_WhenExceptionOccurs_ThrowsException()
        {
            // Arrange
            var movieId = Guid.NewGuid();

            _mockRedisService.Setup(redis => redis.GetAsync<MovieResponseDto>(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Redis error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _movieService.GetMovieDetailAsync(movieId));

            Assert.Contains("An error occurred while fetching movie details", exception.Message);
            _mockLoggerService.Verify(log => log.Error(It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region GetMovieByNameAsync Tests

        [Fact]
        public async Task GetMovieByNameAsync_WithValidName_ReturnsFilteredMovies()
        {
            // Arrange
            string searchName = "action";

            var movies = new List<Movie>
            {
                new Movie {
                    Id = Guid.NewGuid(),
                    Name = "Action Movie",
                    Director = "Director 1",
                    IsDeleted = false
                },
                new Movie {
                    Id = Guid.NewGuid(),
                    Name = "Another Action",
                    Director = "Director 2",
                    IsDeleted = false
                },
                new Movie {
                    Id = Guid.NewGuid(),
                    Name = "Comedy Film",
                    Director = "Director 3",
                    IsDeleted = false
                }
            };

            _mockMovieRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<Movie, bool>>>(), It.IsAny<Expression<Func<Movie, object>>[]>()))
                .ReturnsAsync(movies);

            // Act
            var result = await _movieService.GetMovieByNameAsync(searchName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, m => m.Name == "Action Movie");
            Assert.Contains(result, m => m.Name == "Another Action");
            Assert.DoesNotContain(result, m => m.Name == "Comedy Film");
        }

        [Fact]
        public async Task GetMovieByNameAsync_WithNullName_ReturnsAllMovies()
        {
            // Arrange
            string searchName = null!;

            var movies = new List<Movie>
            {
                new Movie {
                    Id = Guid.NewGuid(),
                    Name = "Action Movie",
                    IsDeleted = false
                },
                new Movie {
                    Id = Guid.NewGuid(),
                    Name = "Comedy Movie",
                    IsDeleted = false
                }
            };

            _mockMovieRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<Movie, bool>>>(), It.IsAny<Expression<Func<Movie, object>>[]>()))
                .ReturnsAsync(movies);

            // Act
            var result = await _movieService.GetMovieByNameAsync(searchName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetMovieByNameAsync_ExcludesDeletedMovies()
        {
            // Arrange
            string searchName = "movie";

            var movies = new List<Movie>
            {
                new Movie {
                    Id = Guid.NewGuid(),
                    Name = "Active Movie",
                    IsDeleted = false
                },
                new Movie {
                    Id = Guid.NewGuid(),
                    Name = "Deleted Movie",
                    IsDeleted = true
                }
            };

            _mockMovieRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<Movie, bool>>>(), It.IsAny<Expression<Func<Movie, object>>[]>()))
                .ReturnsAsync(movies);

            // Act
            var result = await _movieService.GetMovieByNameAsync(searchName);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Active Movie", result[0].Name);
        }

        [Fact]
        public async Task GetMovieByNameAsync_WhenExceptionOccurs_ThrowsException()
        {
            // Arrange
            _mockMovieRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<Movie, bool>>>(), It.IsAny<Expression<Func<Movie, object>>[]>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _movieService.GetMovieByNameAsync("test"));

            Assert.Contains("An error occurred while searching for movies", exception.Message);
            _mockLoggerService.Verify(log => log.Error(It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region AddMovieAsync Tests

        [Fact]
        public async Task AddMovieAsync_WithValidData_ReturnsMovieResponseDto()
        {
            // Arrange
            var movieRequestDto = new MovieRequestDto
            {
                Name = "Test Movie",
                Director = "Test Director",
                Description = "Test Description",
                RunningTime = 120,
                FromDate = DateTime.Now.AddDays(10),
                ToDate = DateTime.Now.AddDays(40),
                Genres = new List<string> { "Action", "Adventure" },
                TrailerUrl = "https://example.com/trailer",
                Rating = 8.5f
            };

            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Name = movieRequestDto.Name,
                Director = movieRequestDto.Director,
                Description = movieRequestDto.Description,
                RunningTime = movieRequestDto.RunningTime,
                FromDate = movieRequestDto.FromDate,
                ToDate = movieRequestDto.ToDate,
                Genres = movieRequestDto.Genres,
                TrailerUrl = movieRequestDto.TrailerUrl,
                Rating = movieRequestDto.Rating,
                Status = MovieStatus.ComingSoon
            };

            _mockMovieRepository.Setup(repo => repo.AddAsync(It.IsAny<Movie>()))
                .ReturnsAsync((Movie m) => { m.Id = Guid.NewGuid(); return m; });

            _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _movieService.AddMovieAsync(movieRequestDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(movieRequestDto.Name, result.Name);
            Assert.Equal(movieRequestDto.Director, result.Director);
            Assert.Equal(movieRequestDto.Rating, result.Rating);
            Assert.Equal(MovieStatus.ComingSoon, result.Status);

            _mockMovieRepository.Verify(repo => repo.AddAsync(It.Is<Movie>(m =>
                m.Name == movieRequestDto.Name &&
                m.Director == movieRequestDto.Director &&
                m.RunningTime == movieRequestDto.RunningTime)), Times.Once);

            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
            _mockRedisService.Verify(redis => redis.RemoveByPatternAsync("movies:list:"), Times.Once);

            _mockAuditLogService.Verify(log => log.LogAsync(
                It.IsAny<Guid>(), AuditActionType.Create, "Movie", It.IsAny<Guid>(),
                It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _mockLoggerService.Verify(log => log.Success(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task AddMovieAsync_WithInvalidDateRange_ThrowsInvalidOperationException()
        {
            // Arrange
            var movieRequestDto = new MovieRequestDto
            {
                Name = "Invalid Movie",
                Director = "Test Director",
                FromDate = DateTime.Now.AddDays(40),  // End date before start date
                ToDate = DateTime.Now.AddDays(10)
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _movieService.AddMovieAsync(movieRequestDto));

            Assert.Contains("ToDate cannot be earlier than FromDate", exception.Message);
            _mockLoggerService.Verify(log => log.Warn(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task AddMovieAsync_WhenExceptionOccurs_ThrowsException()
        {
            // Arrange
            var movieRequestDto = new MovieRequestDto
            {
                Name = "Test Movie",
                FromDate = DateTime.Now.AddDays(10),
                ToDate = DateTime.Now.AddDays(40)
            };

            _mockMovieRepository.Setup(repo => repo.AddAsync(It.IsAny<Movie>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _movieService.AddMovieAsync(movieRequestDto));

            _mockLoggerService.Verify(log => log.Error(It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region UpdateMovieInfoAsync Tests

        [Fact]
        public async Task UpdateMovieInfoAsync_WithValidData_ReturnsUpdatedMovie()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var updateMovieDto = new MovieUpdateDto
            {
                Name = "Updated Movie",
                Director = "Updated Director",
                Description = "Updated Description",
                FromDate = DateTime.Now.AddDays(5),
                ToDate = DateTime.Now.AddDays(35),
                RunningTime = 130,
                TrailerUrl = "https://example.com/updated-trailer",
                Genres = new List<string> { "Action", "Thriller" },
                Rating = 9.0f,
                Status = MovieStatus.NowShowing
            };

            var existingMovie = new Movie
            {
                Id = movieId,
                Name = "Original Movie",
                Director = "Original Director",
                Description = "Original Description",
                FromDate = DateTime.Now.AddDays(10),
                ToDate = DateTime.Now.AddDays(40),
                RunningTime = 120,
                TrailerUrl = "https://example.com/trailer",
                Genres = new List<string> { "Action" },
                Rating = 8.5f,
                Status = MovieStatus.ComingSoon,
                IsDeleted = false
            };

            _mockMovieRepository.Setup(repo => repo.GetByIdAsync(movieId))
                .ReturnsAsync(existingMovie);

            _mockUnitOfWork.Setup(uow => uow.Movies.Update(It.IsAny<Movie>()))
                .ReturnsAsync(true);

            _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _movieService.UpdateMovieInfoAsync(movieId, updateMovieDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateMovieDto.Name, result.Name);
            Assert.Equal(updateMovieDto.Director, result.Director);
            Assert.Equal(updateMovieDto.Description, result.Description);
            Assert.Equal(updateMovieDto.RunningTime, result.RunningTime);
            Assert.Equal(updateMovieDto.TrailerUrl, result.TrailerUrl);
            Assert.Equal(updateMovieDto.Genres, result.Genres);
            Assert.Equal(updateMovieDto.Rating, result.Rating);
            Assert.Equal(updateMovieDto.Status, result.Status);

            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
            _mockRedisService.Verify(redis => redis.RemoveByPatternAsync("movie:list:*"), Times.Once);
            _mockRedisService.Verify(redis => redis.RemoveByPatternAsync($"movie:detail:*"), Times.Once);

            _mockAuditLogService.Verify(log => log.LogAsync(
                It.IsAny<Guid>(), AuditActionType.Update, "Movie", movieId,
                It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _mockLoggerService.Verify(log => log.Success(It.IsAny<string>()), Times.Once);
        }
        [Fact]
        public async Task UpdateMovieInfoAsync_WithNoChanges_ReturnsUnchangedMovie()
        {
            // Arrange
            var movieId = Guid.NewGuid();

            var existingMovie = new Movie
            {
                Id = movieId,
                Name = "Unchanged Movie",
                Director = "Director",
                Description = "Description",
                FromDate = DateTime.Now.AddDays(10),
                ToDate = DateTime.Now.AddDays(40),
                RunningTime = 120,
                IsDeleted = false,
                Status = MovieStatus.ComingSoon
            };

            // Create a DTO with the same values as the existing movie
            var updateMovieDto = new MovieUpdateDto
            {
                Name = existingMovie.Name,
                Director = existingMovie.Director,
                Description = existingMovie.Description,
                FromDate = existingMovie.FromDate,
                ToDate = existingMovie.ToDate,
                RunningTime = existingMovie.RunningTime,
                Status = existingMovie.Status // Explicitly set the Status to match
            };

            _mockMovieRepository.Setup(repo => repo.GetByIdAsync(movieId))
                .ReturnsAsync(existingMovie);

            // Act
            var result = await _movieService.UpdateMovieInfoAsync(movieId, updateMovieDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingMovie.Name, result.Name);
            Assert.Equal(existingMovie.Director, result.Director);
            Assert.Equal(existingMovie.Status, result.Status);

            _mockMovieRepository.Verify(repo => repo.Update(It.IsAny<Movie>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Never);
            _mockLoggerService.Verify(log => log.Warn(It.Is<string>(s => s.Contains("No changes detected"))), Times.Once);
        }

        [Fact]
        public async Task UpdateMovieInfoAsync_WithNonExistentMovie_ThrowsKeyNotFoundException()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var updateMovieDto = new MovieUpdateDto
            {
                Name = "Updated Movie"
            };

            _mockMovieRepository.Setup(repo => repo.GetByIdAsync(movieId))
                .ReturnsAsync((Movie)null!);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _movieService.UpdateMovieInfoAsync(movieId, updateMovieDto));

            Assert.Contains($"Movie with ID {movieId} not found.", exception.Message);
            _mockLoggerService.Verify(log => log.Warn(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateMovieInfoAsync_WithPastFromDate_ThrowsArgumentException()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var updateMovieDto = new MovieUpdateDto
            {
                FromDate = DateTime.UtcNow.AddDays(-1)  // Past date
            };

            var existingMovie = new Movie
            {
                Id = movieId,
                Name = "Test Movie",
                IsDeleted = false
            };

            _mockMovieRepository.Setup(repo => repo.GetByIdAsync(movieId))
                .ReturnsAsync(existingMovie);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _movieService.UpdateMovieInfoAsync(movieId, updateMovieDto));

            Assert.Contains("FromDate cannot be in the past", exception.Message);
        }

        [Fact]
        public async Task UpdateMovieInfoAsync_WithInvalidDateRange_ThrowsArgumentException()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var updateMovieDto = new MovieUpdateDto
            {
                FromDate = DateTime.Now.AddDays(10),
                ToDate = DateTime.Now.AddDays(5)  // Before FromDate
            };

            var existingMovie = new Movie
            {
                Id = movieId,
                Name = "Test Movie",
                IsDeleted = false
            };

            _mockMovieRepository.Setup(repo => repo.GetByIdAsync(movieId))
                .ReturnsAsync(existingMovie);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _movieService.UpdateMovieInfoAsync(movieId, updateMovieDto));

            Assert.Contains("ToDate must be after FromDate", exception.Message);
        }

        #endregion

        #region DeleteMovieAsync Tests

        [Fact]
        public async Task DeleteMovieAsync_WithValidId_ReturnsTrue()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var movie = new Movie
            {
                Id = movieId,
                Name = "Movie to Delete",
                IsDeleted = false
            };

            _mockMovieRepository.Setup(repo => repo.GetByIdAsync(movieId))
                .ReturnsAsync(movie);

            _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _movieService.DeleteMovieAsync(movieId);

            // Assert
            Assert.True(result);

            _mockMovieRepository.Verify(repo => repo.SoftRemove(It.Is<Movie>(m =>
                m.Id == movieId)), Times.Once);

            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
            _mockRedisService.Verify(redis => redis.RemoveByPatternAsync("movie:list:*"), Times.Once);
            _mockRedisService.Verify(redis => redis.RemoveByPatternAsync($"movie:detail:*"), Times.Once);

            _mockAuditLogService.Verify(log => log.LogAsync(
                It.IsAny<Guid>(), AuditActionType.Delete, "Movie", movieId,
                It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _mockLoggerService.Verify(log => log.Info(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DeleteMovieAsync_WithNonExistentId_ReturnsFalse()
        {
            // Arrange
            var movieId = Guid.NewGuid();

            _mockMovieRepository.Setup(repo => repo.GetByIdAsync(movieId))
                .ReturnsAsync((Movie)null!);

            // Act
            var result = await _movieService.DeleteMovieAsync(movieId);

            // Assert
            Assert.False(result);
            _mockMovieRepository.Verify(repo => repo.SoftRemove(It.IsAny<Movie>()), Times.Never);
            _mockLoggerService.Verify(log => log.Warn(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DeleteMovieAsync_WhenExceptionOccurs_ReturnsFalse()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var movie = new Movie
            {
                Id = movieId,
                Name = "Movie to Delete",
                IsDeleted = false
            };

            _mockMovieRepository.Setup(repo => repo.GetByIdAsync(movieId))
                .ReturnsAsync(movie);

            _mockMovieRepository.Setup(repo => repo.SoftRemove(It.IsAny<Movie>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _movieService.DeleteMovieAsync(movieId);

            // Assert
            Assert.False(result);
            _mockLoggerService.Verify(log => log.Error(It.IsAny<string>()), Times.Once);
        }

        #endregion
    }
}