using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.API.Controllers;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.MovieDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;
using System.Collections.Generic;

namespace MovieTheater.UnitTest.Controllers
{
    public class MovieControllerTests
    {
        private readonly Mock<IMovieService> _mockMovieService;
        private readonly MovieController _controller;

        public MovieControllerTests()
        {
            _mockMovieService = new Mock<IMovieService>();
            _controller = new MovieController(
                _mockMovieService.Object
            );
        }

        [Fact]
        public async Task GetAllMoviesAsync_ValidParameters_ReturnsOkResult()
        {
            // Arrange
            var movieList = new Pagination<MovieResponseDto> { Items = new List<MovieResponseDto>() };
            _mockMovieService.Setup(s => s.GetAllMoviesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<MovieStatus>()))
                .ReturnsAsync(movieList);

            // Act
            var result = await _controller.GetAllMoviesAsync("title", "name", false, 1, 10, new List<string> { "Action" }, MovieStatus.NowShowing);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<Pagination<MovieResponseDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(movieList, apiResult.Value.Data);
        }


        [Theory]
        [InlineData("name", false)]
        [InlineData("fromDate", true)]
        [InlineData("toDate", false)]
        [InlineData("status", true)]
        public async Task GetAllMoviesAsync_DifferentSortParameters_ReturnsOkResult(string sortBy, bool isDescending)
        {
            // Arrange
            var movieList = new Pagination<MovieResponseDto> { Items = new List<MovieResponseDto>() };
            _mockMovieService.Setup(s => s.GetAllMoviesAsync(It.IsAny<string>(), sortBy, isDescending,
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<MovieStatus>()))
                .ReturnsAsync(movieList);

            // Act
            var result = await _controller.GetAllMoviesAsync(null, sortBy, isDescending, 1, 10, null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<ApiResult<Pagination<MovieResponseDto>>>(okResult.Value);
        }

        [Fact]
        public async Task GetMovieDetailAsync_MovieExists_ReturnsOkResult()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var movieDto = new MovieResponseDto { Id = movieId, Name = "Test Movie" };
            _mockMovieService.Setup(s => s.GetMovieDetailAsync(movieId)).ReturnsAsync(movieDto);

            // Act
            var result = await _controller.GetMovieDetailAsync(movieId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<MovieResponseDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(movieDto, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetMovieDetailAsync_MovieNotFound_ReturnsNotFound()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            _mockMovieService.Setup(s => s.GetMovieDetailAsync(movieId))
                .ThrowsAsync(new KeyNotFoundException($"Movie with ID {movieId} not found."));

            // Act
            var result = await _controller.GetMovieDetailAsync(movieId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(notFoundResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetMovieDetailAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            _mockMovieService.Setup(s => s.GetMovieDetailAsync(movieId))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetMovieDetailAsync(movieId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetMoviesAsync_MoviesFound_ReturnsOkResult()
        {
            // Arrange
            var moviesList = new List<MovieResponseDto>
            {
                new MovieResponseDto { Id = Guid.NewGuid(), Name = "Test Movie 1" },
                new MovieResponseDto { Id = Guid.NewGuid(), Name = "Test Movie 2" }
            };
            _mockMovieService.Setup(s => s.GetMovieByNameAsync(It.IsAny<string>()))
                .ReturnsAsync(moviesList);

            // Act
            var result = await _controller.GetMoviesAsync("Test");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<List<MovieResponseDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(moviesList, apiResult.Value.Data);
            Assert.Equal("Movies found", apiResult.Value.Message);
        }

        [Fact]
        public async Task GetMoviesAsync_NoMoviesFound_ReturnsOkWithEmptyList()
        {
            // Arrange
            var emptyList = new List<MovieResponseDto>();
            _mockMovieService.Setup(s => s.GetMovieByNameAsync(It.IsAny<string>()))
                .ReturnsAsync(emptyList);

            // Act
            var result = await _controller.GetMoviesAsync("NonExistent");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<List<MovieResponseDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Empty(apiResult.Value.Data);
            Assert.Equal("No movies found", apiResult.Value.Message);
        }

        [Fact]
        public async Task GetMoviesAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            _mockMovieService.Setup(s => s.GetMovieByNameAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetMoviesAsync("Test");

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task AddMovieAsync_ValidData_ReturnsOkResult()
        {
            // Arrange
            var movieRequest = new MovieRequestDTO
            {
                Name = "Test Movie",
                FromDate = DateTime.Now,
                ToDate = DateTime.Now.AddDays(30),
                Director = "Test Director",
                Genres = new List<string> { "Action", "Drama" },
                Description = "Test description",
                PosterImage = "https://example.com/poster.jpg",
                BackgroundImage = "https://example.com/bg.jpg",
                TrailerUrl = "https://example.com/trailer.mp4"
            };
            var movieResponse = new MovieResponseDto { Id = Guid.NewGuid(), Name = "Test Movie" };
            _mockMovieService.Setup(s => s.AddMovieAsync(movieRequest)).ReturnsAsync(movieResponse);

            // Act
            var result = await _controller.AddMovieAsync(movieRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<MovieResponseDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(movieResponse, apiResult.Value.Data);
        }

        [Fact]
        public async Task AddMovieAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var movieRequest = new MovieRequestDTO { Name = "Test Movie" };
            _mockMovieService.Setup(s => s.AddMovieAsync(movieRequest))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.AddMovieAsync(movieRequest);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<MovieResponseDto>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task UpdateMovieAsync_ValidData_ReturnsOkResult()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var updateDto = new MovieUpdateDto { Name = "Updated Movie" };
            _mockMovieService.Setup(s => s.UpdateMovieInfoAsync(movieId, updateDto))
                .ReturnsAsync(updateDto);

            // Act
            var result = await _controller.UpdateMovieAsync(movieId, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<MovieUpdateDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(updateDto, apiResult.Value.Data);
        }

        [Fact]
        public async Task UpdateMovieAsync_NullDto_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.UpdateMovieAsync(Guid.NewGuid(), null!);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task UpdateMovieAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            var updateDto = new MovieUpdateDto { Name = "Updated Movie" };
            _mockMovieService.Setup(s => s.UpdateMovieInfoAsync(movieId, updateDto))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.UpdateMovieAsync(movieId, updateDto);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task DeleteMovie_ValidId_ReturnsOkResult()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            _mockMovieService.Setup(s => s.DeleteMovieAsync(movieId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteMovie(movieId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<bool>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.True(apiResult.Value.Data);
        }

        [Fact]
        public async Task DeleteMovie_MovieNotFound_ReturnsNotFound()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            _mockMovieService.Setup(s => s.DeleteMovieAsync(movieId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteMovie(movieId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(notFoundResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task DeleteMovie_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var movieId = Guid.NewGuid();
            _mockMovieService.Setup(s => s.DeleteMovieAsync(movieId))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.DeleteMovie(movieId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetAllMoviesAsync_InvalidPagination_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetAllMoviesAsync(null, null, false, 0, 0, null, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("Invalid pagination parameters.", apiResult.Error.Message);
        }

        [Fact]
        public async Task GetAllMoviesAsync_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var movies = new Pagination<MovieResponseDto> { Items = new List<MovieResponseDto>() };
            _mockMovieService.Setup(s => s.GetAllMoviesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<MovieStatus?>()))
                .ReturnsAsync(movies);

            // Act
            var result = await _controller.GetAllMoviesAsync(null, null, false, 1, 10, null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<Pagination<MovieResponseDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(movies, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetAllMoviesAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            _mockMovieService.Setup(s => s.GetAllMoviesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<MovieStatus?>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetAllMoviesAsync(null, null, false, 1, 10, null, null);

            // Assert
            Assert.IsType<ObjectResult>(result);
        }
    }
}