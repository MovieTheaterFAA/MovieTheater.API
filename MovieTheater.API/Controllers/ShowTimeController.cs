using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.ShowTimeDTOs;
using MovieTheater.Infrastructure.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers
{
    [Route("api/[controller]")]
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

        [HttpPost("showtime")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
            Summary = "Add a new showtime",
            Description = "Creates a new showtime for a movie in a cinema room. Requires Admin privileges."
        )]
        [ProducesResponseType(typeof(ApiResult<ShowtimeResponseDTO>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> AddShowTimeAsync([FromBody, SwaggerParameter("New showtime data to be added")] ShowTimeRequestDto showTimeRequestDto)
        {
            try
            {
                var result = await _showTimeService.AddShowTimeAsync(showTimeRequestDto);
                return Ok(ApiResult<ShowtimeResponseDTO>.Success(result, "200", "Showtime added successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<ShowtimeResponseDTO>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }
    }
}
