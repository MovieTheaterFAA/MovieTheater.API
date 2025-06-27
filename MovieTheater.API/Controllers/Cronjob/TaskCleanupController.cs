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
    }
}