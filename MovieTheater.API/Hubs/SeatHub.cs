using Microsoft.AspNetCore.SignalR;

namespace MovieTheater.API.Hubs
{
    public class SeatHub : Hub
    {
        public async Task JoinShowTimeGroup(string showTimeId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ShowTime_{showTimeId}");
        }
    }
}
