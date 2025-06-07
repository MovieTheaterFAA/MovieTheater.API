using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.MovieDTOs;
using MovieTheater.Infrastructure.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers
{
    [Route("api/movie")]
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
        [HttpPost()]
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
