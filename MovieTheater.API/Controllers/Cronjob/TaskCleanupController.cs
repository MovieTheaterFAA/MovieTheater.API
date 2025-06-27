using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces.Cronjob;

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
        public async Task<IActionResult> CleanupPastShowTimes()
        {
            var count = await _cleanupService.CleanupPastShowTimesAsync();
            return Ok(new { Message = $"Soft deleted {count} past showtimes." });
        }
    }
}
