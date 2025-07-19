using MovieTheater.Domain.Entities;

namespace MovieTheater.Application.Interfaces
{
    public interface IScoreService
    {
        Task AddScoreForBookingAsync(User user, Booking booking);
        (decimal discountPercent, int usedPoints) CalculateDiscount(int availablePoints, int requestedPoints);
        Task UseScoreForBookingAsync(User user, Booking booking, int usedPoints);
        Task RefundScoreForBookingAsync(Guid bookingId);
        Task<int> GetCurrentScoreAsync();
        Task<List<ScoreHistory>> GetScoreHistoryAsync();
    }
}
