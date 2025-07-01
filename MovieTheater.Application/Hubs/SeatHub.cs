using Microsoft.AspNetCore.SignalR;

namespace MovieTheater.Application.Hubs
{
    public class SeatHub : Hub
    {
        public async Task JoinShowTimeGroup(string showTimeId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ShowTime_{showTimeId}");
        }
    }
}