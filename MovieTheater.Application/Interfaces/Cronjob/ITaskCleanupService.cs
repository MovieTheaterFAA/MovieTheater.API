namespace MovieTheater.Application.Interfaces.Cronjob
{
    public interface ITaskCleanupService
    {
        Task<int> CleanupPastShowTimesAsync();

        Task<int> CleanupExpiredEventsAsync();

        Task<int> CreateBirthdayPromotionsAsync();
        Task<int> CleanupExpiredOrDeletedShowTimeSeatsAsync();

    }
}
