using Microsoft.AspNetCore.SignalR;

namespace MovieTheater.Infrastructure.Hubs
{
    public class ChatbotHub : Hub
    {
        //Allow clients to join a group (e.g., for a specific chat session or user)
        public async Task JoinChatGroup(string groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        }

        //Allow clients to leave a group
        public async Task LeaveChatGroup(string groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        }
    }
}