using MovieTheater.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Application.Interfaces
{
    public interface IScoreService
    {
        Task AddScoreForBookingAsync(User user, Booking booking);
        (decimal discountPercent, int usedPoints) CalculateDiscount(int availablePoints, int requestedPoints);
        Task UseScoreForBookingAsync(User user, Booking booking, int usedPoints);
        Task RefundScoreForBookingAsync(Booking booking);
        Task<int> GetCurrentScoreAsync();
        Task<List<ScoreHistory>> GetScoreHistoryAsync();
    }
}
