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

        public async Task HoldSeats(string showTimeId, string userId, List<Guid> seatIds)
        {
            if (!Guid.TryParse(userId, out var userGuid) || !Guid.TryParse(showTimeId, out var showTimeGuid))
            {
                await Clients.Caller.SendAsync("ReceiveSeatHoldError", "Invalid user or showtime ID.");
                return;
            }

            try
            {
                var heldSeats = await _seatService.HoldSeatsAsync(userGuid, showTimeGuid, seatIds);

                await Clients.Group($"ShowTime_{showTimeId}").SendAsync("ReceiveSeatUpdate", new
                {
                    ShowTimeId = showTimeId,
                    Seats = heldSeats.Select(s => new
                    {
                        SeatId = s.Id,
                        Row = s.Row,
                        Number = s.Number,
                        Type = s.Type,
                        Status = "Holding"
                    })
                });
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ReceiveSeatHoldError", ex.Message);
            }
        }
    }
}