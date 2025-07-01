using Microsoft.AspNetCore.SignalR;
using MovieTheater.Application.Interfaces;

namespace MovieTheater.Application.Hubs
{
    public class ChatbotHub : Hub
    {
        private readonly IChatbotService _chatbotService;

        public ChatbotHub(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
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
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        }

        public async Task LeaveChatGroup(string groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        }
    }
}