using MovieTheater.Domain.Entities;

namespace MovieTheater.Application.Interfaces
{
    public interface IDataAnalyzerService
    {
        Task<IReadOnlyList<Movie>> GetMostBookedMoviesAsync(int top);

        Task<IReadOnlyList<Movie>> GetTopRatingMoviesAsync(int top);
    }
}