using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.MovieDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers
{
    [Route("api/movies")]
    [ApiController]
    public class MovieController : ControllerBase
    {
        private readonly IMovieService _movieService;

        public MovieController(IMovieService movieService)
        {
            _movieService = movieService;
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Get all movies",
            Description = "Retrieve a paginated list of movies with optional search, genre, and status filtering.")]
        [ProducesResponseType(typeof(ApiResult<Pagination<MovieResponseDto>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetAllMoviesAsync(
        [FromQuery, SwaggerParameter(Description = "Search by name, director, or actors (optional)")] string? search,
        [FromQuery, SwaggerParameter(Description = "Sort by field: name, fromDate, toDate, status (optional)")] string? sortBy,
        [FromQuery, SwaggerParameter(Description = "Sort in descending order? Default: false")] bool isDescending = false,
        [FromQuery, SwaggerParameter(Description = "Page number, starting from 1")] int page = 1,
        [FromQuery, SwaggerParameter(Description = "Number of items per page")] int pageSize = 10,
        [FromQuery(Name = "genres")] List<string>? genres = null,
        [FromQuery, SwaggerParameter(Description = "Filter by movie status")] MovieStatus? status = null)
        {
            try
            {
                if (page < 1 || pageSize < 1)
                    return BadRequest(ApiResult<object>.Failure("400", "Invalid pagination parameters."));

                var movies = await _movieService.GetAllMoviesAsync(search, sortBy, isDescending, page, pageSize, genres, status);

                return Ok(ApiResult<Pagination<MovieResponseDto>>.Success(movies, "200", "Movies retrieved successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Get movie details",
            Description = "Retrieve detailed information for a specific movie by its ID.")]
        [ProducesResponseType(typeof(ApiResult<MovieResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 404)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetMovieDetailAsync([FromRoute] Guid id)
        {
            try
            {
                var movie = await _movieService.GetMovieDetailAsync(id);
                return Ok(ApiResult<MovieResponseDto>.Success(movie, "200", "Movie details retrieved successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResult<object>.Failure("404", ex.Message));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpGet("search")]
        [SwaggerOperation(
            Summary = "Search movies by name",
            Description = "Allows members to search for movies by name.")]
        [ProducesResponseType(typeof(ApiResult<List<MovieResponseDto>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetMoviesAsync(
        [FromQuery, SwaggerParameter(Description = "Movie name to search (optional)")] string? Name)
        {
            try
            {
                var result = await _movieService.GetMovieByNameAsync(Name);

                if (!result.Any())
                {
                    return Ok(ApiResult<List<MovieResponseDto>>.Success(result, "200", "No movies found"));
                }

                return Ok(ApiResult<List<MovieResponseDto>>.Success(result, "200", "Movies found"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPost]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
               Summary = "Add a new movie",
               Description = "Creates a new movie with the provided information. Requires Admin privileges.")]
        [ProducesResponseType(typeof(ApiResult<MovieResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> AddMovieAsync([FromBody, SwaggerParameter("New movie data to be added")] MovieRequestDto movieRequestDto)
        {
            try
            {
                var result = await _movieService.AddMovieAsync(movieRequestDto);
                return Ok(ApiResult<MovieResponseDto>.Success(result, "200", "Movie added successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<MovieResponseDto>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        [SwaggerOperation(
               Summary = "Update movie information",
               Description = "Updates the details of a specific movie by its ID."
        )]
        [ProducesResponseType(typeof(ApiResult<MovieUpdateDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> UpdateMovieAsync([FromRoute] Guid id, [FromBody] MovieUpdateDto movieUpdateDto)
        {
            try
            {
                if (movieUpdateDto == null)
                {
                    return BadRequest(ApiResult<object>.Failure("400", "Movie update data is required."));
                }

                var updatedMovie = await _movieService.UpdateMovieInfoAsync(id, movieUpdateDto);

                return Ok(ApiResult<MovieUpdateDto>.Success(updatedMovie, "200", "Movie updated successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
               Summary = "Delete movie",
               Description = "Delete movie by its ID."
        )]
        [ProducesResponseType(typeof(ApiResult<bool>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> DeleteMovie(Guid id)
        {
            try
            {
                var result = await _movieService.DeleteMovieAsync(id);

                if (!result)
                {
                    return NotFound(ApiResult<object>.Failure("404", $"Movie with ID {id} not found."));
                }

                return Ok(ApiResult<bool>.Success(result, "200", "Movie delete successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPost("add-with-files")]
        [Authorize(Policy = "AdminPolicy")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResult<bool>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> AddMovieWithFilesAsync([FromForm] MovieCreateWithFilesDto dto)
        {
            try
            {
                var result = await _movieService.AddMovieWithFilesAsync(dto);
                return Ok(ApiResult<MovieResponseDto>.Success(result, "200", "Movie and files added successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<MovieResponseDto>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }
    }
}