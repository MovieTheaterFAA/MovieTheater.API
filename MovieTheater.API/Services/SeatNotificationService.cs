using Microsoft.AspNetCore.SignalR;
using MovieTheater.API.Hubs;
using MovieTheater.Application.Interfaces;

public class SeatNotificationService : ISeatNotificationService
{
    private readonly IHubContext<SeatHub> _hubContext;
    public SeatNotificationService(IHubContext<SeatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifySeatsUpdated(Guid showTimeId, IEnumerable<object> seatUpdates)
    {
        await _hubContext.Clients.Group($"ShowTime_{showTimeId}")
            .SendAsync("SeatsUpdated", seatUpdates);
    }
}