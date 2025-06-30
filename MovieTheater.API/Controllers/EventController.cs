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

        public EventController(IEventService eventService, IClaimsService claimsService)
        {
            _eventService = eventService;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get all events", Description = "Get paginated list of events with optional filters.")]
        [ProducesResponseType(typeof(ApiResult<Pagination<EventResponseDto>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetAllEventsAsync(
            [FromQuery, SwaggerParameter(Description = "Search by event name")] string? search,
            [FromQuery, SwaggerParameter(Description = "Sort by field: StartTime, EndTime (optional)")] string? sortBy,
            [FromQuery, SwaggerParameter(Description = "Sort descending? Default: false")] bool isDescending = false,
            [FromQuery, SwaggerParameter(Description = "Page number, starts at 1")] int page = 1,
            [FromQuery, SwaggerParameter(Description = "Items per page")] int pageSize = 10)
        {
            try
            {
                if (page < 1 || pageSize < 1)
                    return BadRequest(ApiResult<object>.Failure("400", "Invalid pagination parameters"));

                var result = await _eventService.GetAllEventsAsync(search, sortBy, isDescending, page, pageSize);

                return Ok(ApiResult<Pagination<EventResponseDto>>.Success(result, "200", "Get events successfully"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPost("image")]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
            Summary = "Add a new event",
            Description = "Creates a new event with the provided information. Requires Admin privileges.")]
        [ProducesResponseType(typeof(ApiResult<EventResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 404)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> CreateEventWithImage([FromForm] EventWithImageRequestDto request)
        {
            try
            {
                var result = await _eventService.AddEventWithImageAsync(request);
                return Ok(ApiResult<EventResponseDto>.Success(result, "200", "Event created successfully."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResult<string>.Failure("400", ex.Message));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<EventResponseDto>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

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
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResult<object>.Failure("404", ex.Message));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<EventResponseDto>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
            Summary = "Update an event",
            Description = "Update an existing event. Requires Admin privileges.")]
        [ProducesResponseType(typeof(EventResponseDto), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> UpdateEventAsync([FromRoute] Guid id, [FromBody] EventUpdateDto dto)
        {
            try
            {
                var result = await _eventService.UpdateEventAsync(id, dto);
                return Ok(ApiResult<EventResponseDto>.Success(result!, "200", "updated event successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<EventResponseDto>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
            Summary = "Delete event",
            Description = "Delete an event and its associated promotions by ID.")]
        [ProducesResponseType(typeof(ApiResult<bool>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> DeleteEventAsync([FromRoute, SwaggerParameter(Description = "ID of the event to delete")] Guid id)
        {
            try
            {
                var result = await _eventService.DeleteEventByIdAsync(id);

                if (!result)
                {
                    return NotFound(ApiResult<object>.Failure("404", $"Event with ID {id} not found."));
                }

                return Ok(ApiResult<bool>.Success(true, "200", "Event deleted successfully"));
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
