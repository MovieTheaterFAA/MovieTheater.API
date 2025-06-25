using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;

namespace MovieTheater.API.Controllers
{
    [ApiController]
    [Route("api/chatbot")]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;

        public ChatbotController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        /// <summary>
        /// Get AI analysis of the most booked movies for members.
        /// </summary>
        [HttpGet("analyze-most-booked-movies")]
        public async Task<IActionResult> AnalyzeMostBookedMovies([FromQuery] int top = 5)
        {
            try
            {
                var result = await _chatbotService.AnalyzeMostBookedMoviesForMemberAsync(top);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        /// <summary>
        /// Get AI analysis of the top rating movies for members.
        /// </summary>
        [HttpGet("analyze-top-rating-movies")]
        public async Task<IActionResult> AnalyzeTopRatingMovies([FromQuery] int top = 5)
        {
            try
            {
                var result = await _chatbotService.AnalyzeTopRatingMoviesForMemberAsync(top);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        /// <summary>
        /// Ask the AI chatbot a custom question as a member.
        /// </summary>
        [HttpPost("ask")]
        public async Task<IActionResult> AskMember([FromBody] AskMemberRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Prompt))
                    return BadRequest(ApiResult<object>.Failure("400", "Prompt is required."));

                var result = await _chatbotService.AskMemberAsync(request.Prompt);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        public class AskMemberRequest
        {
            public string Prompt { get; set; }
        }
    }
}