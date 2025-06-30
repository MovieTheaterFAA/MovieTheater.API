using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.ChatbotDTOs;
using MovieTheater.Infrastructure.Hubs;

namespace MovieTheater.API.Controllers
{
    [ApiController]
    [Route("api/chatbot")]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;
        private readonly IHubContext<ChatbotHub> _chatbotHub;

        public ChatbotController(IChatbotService chatbotService, IHubContext<ChatbotHub> chatbotHub)
        {
            _chatbotService = chatbotService;
            _chatbotHub = chatbotHub;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskMember([FromBody] AskMemberRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Prompt))
                    return BadRequest(ApiResult<object>.Failure("400", "Prompt is required."));

                var result = await _chatbotService.FreestyleAskAsync(request.Prompt);

                if (!string.IsNullOrWhiteSpace(request.GroupId))
                {
                    await _chatbotHub.Clients.Group(request.GroupId)
                        .SendAsync("ReceiveChatbotResponse", new
                        {
                            GroupId = request.GroupId,
                            Response = result,
                            Timestamp = DateTime.UtcNow
                        });
                }

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