using Microsoft.AspNetCore.SignalR;
using MovieTheater.Application.Interfaces;

namespace MovieTheater.Application.Hubs
{
    public class SeatHub : Hub
    {
        private readonly ISeatService _seatService;

        public SeatHub(ISeatService seatService)
        {
            _seatService = seatService;
        }

        public async Task JoinShowTimeGroup(string showTimeId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ShowTime_{showTimeId}");
        }

        public async Task LeaveShowTimeGroup(string showTimeId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ShowTime_{showTimeId}");
        }

        public async Task BroadcastSeatStatus(string showTimeId)
        {
            if (!Guid.TryParse(showTimeId, out var showTimeGuid))
            {
                await Clients.Caller.SendAsync("ReceiveSeatStatusError", "Invalid showtime ID.");
                return;
            }

            try
            {
                var seatStatusList = await _seatService.GetShowTimeSeatStatusAsync(showTimeGuid);

                await Clients.Group($"ShowTime_{showTimeId}").SendAsync("ReceiveSeatStatus", new
                {
                    ShowTimeId = showTimeId,
                    Seats = seatStatusList.Select(s => new
                    {
                        SeatId = s.SeatId,
                        Row = s.Row,
                        Number = s.Number,
                        Type = s.Type,
                        Status = s.Status.ToString()
                    })
                });
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ReceiveSeatStatusError", ex.Message);
            }
        }
    }
}