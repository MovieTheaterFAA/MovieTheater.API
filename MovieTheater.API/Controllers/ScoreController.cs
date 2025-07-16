using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.Entities;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers
{
    [Route("api/score")]
    [ApiController]
    public class ScoreController : ControllerBase
    {
        private readonly IScoreService _scoreService;

        public ScoreController(IScoreService scoreService)
        {
            _scoreService = scoreService;
        }

        [HttpGet("current")]
        [Authorize]
        [SwaggerOperation(Summary = "Get current score", Description = "Get the current score balance of the logged-in user.")]
        [ProducesResponseType(typeof(ApiResult<int>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 401)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetCurrentScoreAsync()
        {
            try
            {
                var score = await _scoreService.GetCurrentScoreAsync();
                return Ok(ApiResult<int>.Success(score, "200", "Get current score successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpGet("me")]
        [Authorize]
        [SwaggerOperation(Summary = "Get score history", Description = "Get the score history of the logged-in user.")]
        [ProducesResponseType(typeof(ApiResult<List<ScoreHistory>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 401)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetScoreHistoryAsync()
        {
            try
            {
                var history = await _scoreService.GetScoreHistoryAsync();
                return Ok(ApiResult<List<ScoreHistory>>.Success(history, "200", "Get score history successfully."));
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