using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.MovieDTOs;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers
{
    [Route("api/movies")]
    [ApiController]
    public class MovieController : ControllerBase
    {
        private readonly IMovieService _movieService;
        private readonly IClaimsService _claimsService;

        public MovieController(IMovieService movieService, IClaimsService claimsService)
        {
            _movieService = movieService;
            _claimsService = claimsService;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get all movies", Description = "Retrieve a paginated list of movies with optional search and sorting.")]
        [ProducesResponseType(typeof(ApiResult<Pagination<MovieResponseDto>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetAllMoviesAsync(
        [FromQuery, SwaggerParameter(Description = "Search by name, director, or actors (optional)")] string? search,
        [FromQuery, SwaggerParameter(Description = "Sort by field: name, fromDate, toDate (optional)")] string? sortBy,
        [FromQuery, SwaggerParameter(Description = "Sort in descending order? Default: false")] bool isDescending = false,
        [FromQuery, SwaggerParameter(Description = "Page number, starting from 1")] int page = 1,
        [FromQuery, SwaggerParameter(Description = "Number of items per page")] int pageSize = 10)
        {
            try
            {
                if (page < 1 || pageSize < 1)
                    return BadRequest(ApiResult<object>.Failure("400", "Invalid pagination parameters."));

                var movies = await _movieService.GetAllMoviesAsync(search, sortBy, isDescending, page, pageSize);

                return Ok(ApiResult<Pagination<MovieResponseDto>>.Success(movies, "200", "Movies retrieved successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }
        [HttpGet("search")]
        [SwaggerOperation(Summary = "Search movies by name", Description = "Allows members to search for movies by name.")]
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



        [HttpPost("movie")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
        Summary = "Add a new movie",
        Description = "Creates a new movie with the provided information. Requires Admin privileges."
    )]
        [ProducesResponseType(typeof(ApiResult<MovieResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> AddMovieAsync([FromBody, SwaggerParameter("New movie data to be added")] MovieRequestDTO movieRequestDto)
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
    }
}
