using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces.Cronjob;
using MovieTheater.Application.Utils;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers.Cronjob
{
    [ApiController]
    [Route("api/cronjob-task-cleanup")]
    public class TaskCleanupController : ControllerBase
    {
        private readonly ITaskCleanupService _cleanupService;

        public TaskCleanupController(ITaskCleanupService cleanupService)
        {
            _cleanupService = cleanupService;
        }

        [HttpPost("past-showtimes")]
        [SwaggerOperation(
            Summary = "Cleanup past showtimes",
            Description = "Soft delete all showtimes that have already passed (ShowDate before now). Intended for scheduled cleanup tasks or manual maintenance."
        )]
        [ProducesResponseType(typeof(ApiResult<int>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> CleanupPastShowTimes()
        {
            try
            {
                var count = await _cleanupService.CleanupPastShowTimesAsync();
                return Ok(ApiResult<int>.Success(count, "200", $"Soft deleted {count} past showtimes."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPost("expired-events")]
        [SwaggerOperation(
            Summary = "Cleanup expired events",
            Description = "Soft delete all events that have ended (EndTime before now) and their associated promotions. Intended for scheduled cleanup tasks or manual maintenance."
        )]
        [ProducesResponseType(typeof(ApiResult<int>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> CleanupExpiredEvents()
        {
            try
            {
                var count = await _cleanupService.CleanupExpiredEventsAsync();
                return Ok(ApiResult<int>.Success(count, "200", $"Soft deleted {count} expired events."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPost("birthday-promotions")]
        [SwaggerOperation(
            Summary = "Create birthday promotions",
            Description = "Create birthday promotions for users with upcoming birthdays. Intended for scheduled tasks or manual execution."
        )]
        [ProducesResponseType(typeof(ApiResult<int>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> CreateBirthdayPromotions()
        {
            try
            {
                var count = await _cleanupService.CreateBirthdayPromotionsAsync();
                return Ok(ApiResult<int>.Success(count, "200", $"Created {count} birthday promotions."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPost("expired-or-deleted-showtimeseats")]
        [SwaggerOperation(
            Summary = "Cleanup ShowTimeSeat for expired or deleted showtimes",
            Description = "Delete all ShowTimeSeat records where the associated showtime is expired (ShowDate < now) or has been soft deleted. Intended for scheduled cleanup tasks or manual maintenance."
        )]
        [ProducesResponseType(typeof(ApiResult<int>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> CleanupExpiredOrDeletedShowTimeSeats()
        {
            try
            {
                var count = await _cleanupService.CleanupExpiredOrDeletedShowTimeSeatsAsync();
                return Ok(ApiResult<int>.Success(count, "200", $"Deleted {count} ShowTimeSeat records for expired or deleted showtimes."));
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