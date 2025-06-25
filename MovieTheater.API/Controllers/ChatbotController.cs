using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.ChatbotDTOs;

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

        [HttpPost("ask")]
        public async Task<IActionResult> AskMember([FromBody] AskMemberRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Prompt))
                    return BadRequest(ApiResult<object>.Failure("400", "Prompt is required."));

                var result = await _chatbotService.AskMemberAsync(request.Prompt);
                return Ok(ApiResult<string>.Success(result, "200", "Chatbot response generated successfully."));
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