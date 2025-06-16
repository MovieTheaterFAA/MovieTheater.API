using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.ShowTimeDTOs;
using MovieTheater.Infrastructure.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers
{
    [Route("api/showtime")]
    [ApiController]
    public class ShowTimeController : ControllerBase
    {
        private readonly IShowTimeService _showTimeService;
        private readonly IClaimsService _claimsService;

        public ShowTimeController(IShowTimeService showTimeService, IClaimsService claimsService)
        {
            _showTimeService = showTimeService;
            _claimsService = claimsService;
        }


        //[HttpPost("movieId")]
        //[Authorize(Policy = "AdminPolicy")]
        //[SwaggerOperation(
        //    Summary = "Add a new showtime",
        //    Description = "Creates a new showtime for a movie in a cinema room. Requires Admin privileges."
        //)]
        //[ProducesResponseType(typeof(ApiResult<ShowtimeResponseDTO>), 200)]
        //[ProducesResponseType(typeof(ApiResult<object>), 400)]
        //[ProducesResponseType(typeof(ApiResult<object>), 500)]
        //public async Task<IActionResult> AddShowTimeAsync([FromBody, SwaggerParameter("New showtime data to be added")] ShowTimeRequestDto showTimeRequestDto)
        //{
        //    try
        //    {
        //        var result = await _showTimeService.AddShowTimeAsync(showTimeRequestDto);
        //        return Ok(ApiResult<ShowtimeResponseDTO>.Success(result, "200", "Showtime added successfully."));
        //    }
        //    catch (Exception ex)
        //    {
        //        var statusCode = ExceptionUtils.ExtractStatusCode(ex);
        //        var errorResponse = ExceptionUtils.CreateErrorResponse<ShowtimeResponseDTO>(ex);
        //        return StatusCode(statusCode, errorResponse);
        //    }
        //}

        [HttpGet("by-movie-and-date")]
        [SwaggerOperation(
            Summary = "Get showtimes by movie and date",
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

        [HttpGet("by-date")]
        [SwaggerOperation(
        Summary = "Get showtimes by date",
        Description = "Retrieve all showtimes for a specific date."
        )]
        [ProducesResponseType(typeof(ApiResult<List<ShowtimeResponseDTO>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetShowTimesByDate(
        [FromQuery, SwaggerParameter("Show date (yyyy-MM-dd)")] DateTime date)
        {
            try
            {
                if (date.Kind == DateTimeKind.Unspecified)
                    date = DateTime.SpecifyKind(date, DateTimeKind.Utc);

                var result = await _showTimeService.GetShowTimesByDateAsync(date);
                return Ok(ApiResult<List<ShowtimeResponseDTO>>.Success(result, "200", "Showtimes retrieved successfully."));
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
