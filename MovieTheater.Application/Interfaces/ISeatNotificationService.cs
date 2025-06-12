namespace MovieTheater.Application.Interfaces
{
    public interface ISeatNotificationService
    {
        Task NotifySeatsUpdated(Guid showTimeId, IEnumerable<object> seatUpdates);
    }
}
