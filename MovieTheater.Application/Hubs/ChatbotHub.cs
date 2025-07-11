using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MovieTheater.Application.Interfaces;

namespace MovieTheater.Application.Hubs
{
    public class ChatbotHub : Hub
    {
        private readonly IChatbotService _chatbotService;
        private readonly ILogger<ChatbotHub> _logger;

        public ChatbotHub(IChatbotService chatbotService, ILogger<ChatbotHub> logger)
        {
            _chatbotService = chatbotService;
            _logger = logger;
            _logger.LogInformation("ChatbotHub initialized.");
        }

        public async Task AskChatbot(string prompt, string groupId)
        {
            var response = await _chatbotService.FreestyleAskAsync(prompt, groupId);

            await Clients.Group(groupId).SendAsync("ReceiveChatbotResponse", new
            {
                GroupId = groupId,
                Response = response,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task JoinChatGroup(string groupId)
        {
            _logger.LogInformation($"User {Context.ConnectionId} joining group {groupId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        }

        public async Task LeaveChatGroup(string groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        }
    }
}