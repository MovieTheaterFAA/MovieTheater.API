using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.ShowTimeDTOs;
using Swashbuckle.AspNetCore.Annotations;
using static MovieTheater.Domain.DTOs.ShowTimeDTOs.BatchShowtimeRequestDto;

namespace MovieTheater.API.Controllers
{
    [Route("api/showtime")]
    [ApiController]
    public class ShowTimeController : ControllerBase
    {
        private readonly IShowTimeService _showTimeService;

        public ShowTimeController(IShowTimeService showTimeService)
        {
            _showTimeService = showTimeService;
        }

        [HttpPost("batch")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
            Summary = "Add a batch of showtimes",
            Description = "Creates multiple showtimes for a cinema room. Requires Admin privileges."
        )]
        [ProducesResponseType(typeof(ApiResult<List<ShowtimeResponseDTO>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> AddBatchShowTimesAsync(
            [FromBody, SwaggerParameter("Batch showtime data to be added")] BatchShowTimeRequestDto batchShowTimeRequestDto)
        {
            try
            {
                var result = await _showTimeService.AddBatchShowTimesAsync(batchShowTimeRequestDto);
                return Ok(ApiResult<List<ShowtimeResponseDTO>>.Success(result, "200", "Batch showtimes added successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpGet("movie-and-date")]
        [SwaggerOperation(
            Summary = "Get showtimes by movie and date for User",
            Description = "Retrieve all showtimes for a specific movie on a specific date."
        )]
        [ProducesResponseType(typeof(ApiResult<List<ShowtimeResponseDTO>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetShowTimesByMovieAndDate(
            [FromQuery, SwaggerParameter("Movie ID")] Guid movieId,
            [FromQuery, SwaggerParameter("Show date (yyyy-MM-dd)")] DateTime date)
        {
            try
            {
                if (date.Kind == DateTimeKind.Unspecified)
                    date = DateTime.SpecifyKind(date, DateTimeKind.Utc);

                var result = await _showTimeService.GetShowTimesByMovieAndDateAsync(movieId, date);
                return Ok(ApiResult<List<ShowtimeResponseDTO>>.Success(result, "200", "Showtimes retrieved successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpGet("date")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
            Summary = "Get showtimes by date for Admin",
            Description = "Retrieve all showtimes for a specific date.")]
        [ProducesResponseType(typeof(ApiResult<List<ShowtimeResponseDTO>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetShowTimesByDate(
        [FromQuery, SwaggerParameter("Show date (yyyy-MM-dd)")] DateTime date,
        [FromQuery, SwaggerParameter("Optional Movie ID to filter showtimes by movie")] Guid? movieId = null,
        [FromQuery, SwaggerParameter("Optional Cinema Room ID to filter showtimes by room")] Guid? roomId = null)
        {
            try
            {
                if (date.Kind == DateTimeKind.Unspecified)
                    date = DateTime.SpecifyKind(date, DateTimeKind.Utc);

                var result = await _showTimeService.GetShowTimesByDateAsync(date, movieId, roomId);

                return Ok(ApiResult<List<ShowtimeResponseDTO>>.Success(result, "200", "Showtimes retrieved successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpDelete("by-date")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
            Summary = "Delete all showtimes by date",
            Description = "Deletes all showtimes for a specific date. Requires Admin privileges."
        )]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 404)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> DeleteShowTimesByDate(
        [FromQuery, SwaggerParameter("Show date (yyyy-MM-dd)")] DateTime date)
        {
            try
            {
                if (date.Kind == DateTimeKind.Unspecified)
                    date = DateTime.SpecifyKind(date, DateTimeKind.Utc);

                var deletedCount = await _showTimeService.DeleteShowTimesByDateAsync(date);
                if (deletedCount == 0)
                    return NotFound(ApiResult<object>.Failure("404", $"No showtimes found for date {date:yyyy-MM-dd}."));

                return Ok(ApiResult<object>.Success(null, "200", $"Deleted {deletedCount} showtimes for date {date:yyyy-MM-dd}."));
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