using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.MovieDTOs;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.API.Controllers
{
    [Route("api/movie")]
    [ApiController]
    public class MovieController : ControllerBase
    {
        private readonly IMovieService _movieService;
        private readonly IClaimsService _claimsService;
        private readonly ILoggerService _loggerService;

        public MovieController(IMovieService movieService, IClaimsService claimsService, ILoggerService loggerService)
        {
            _movieService = movieService;
            _claimsService = claimsService;
            _loggerService = loggerService;
        }

        /// <summary>
        /// Update movie information by movieId.
        /// </summary>
        [HttpPut("{movieId}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResult<MovieUpdateDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> UpdateMovie([FromRoute] Guid movieId, [FromBody] MovieUpdateDto movieUpdateDto)
        {
            try
            {
                if (movieUpdateDto == null)
                {
                    return BadRequest(ApiResult<object>.Failure("400", "Movie update data is required."));
                }

                var updatedMovie = await _movieService.UpdateMovieInfo(movieId, movieUpdateDto);

                return Ok(ApiResult<MovieUpdateDto>.Success(updatedMovie, "200", "Movie updated successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }
    }
}
