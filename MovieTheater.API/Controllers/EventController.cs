using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.EventDTOs;
using MovieTheater.Infrastructure.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers
{
    [Route("api/events")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly IClaimsService _claimsService;

        public EventController(IEventService eventService, IClaimsService claimsService)
        {
            _eventService = eventService;
            _claimsService = claimsService;
        }

        // API to add a new event
        [HttpPost]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
            Summary = "Add a new event",
            Description = "Creates a new event with the provided information. Requires Admin privileges.")]
        [ProducesResponseType(typeof(ApiResult<EventResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 404)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> AddEventAsync([FromBody] EventRequestDto eventDto)
        {
            try
            {
                var result = await _eventService.AddEventAsync(eventDto);
                return Ok(ApiResult<EventResponseDto>.Success(result!, "200", "Added event successfully."));
            }
            catch (KeyNotFoundException ex) // Catch the specific exception for promotion not found
            {
                return NotFound(ApiResult<object>.Failure("404", ex.Message)); // Return 404 Not Found with the error message
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<EventResponseDto>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPut("{eventId}")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
            Summary = "Update an event",
            Description = "Update an existing event. Requires Admin privileges.")]
        [ProducesResponseType(typeof(EventResponseDto), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> UpdateEventAsync([FromRoute] Guid eventId, [FromBody] EventUpdateDto dto)
        {
            try
            {
                var result = await _eventService.UpdateEventAsync(eventId, dto);
                return Ok(ApiResult<EventResponseDto>.Success(result!, "200", "updated event successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<EventResponseDto>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }
    }
}
