using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Domain;
using MovieTheater.Domain.Entities;

namespace MovieTheater.Application.Services
{
    public class DataAnalyzerService : IDataAnalyzerService
    {
        private readonly MovieTheaterDbContext _context;

        public DataAnalyzerService(MovieTheaterDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Movie>> GetMostBookedMoviesAsync(int top)
        {
            // Query movies ordered by total bookings (across all showtimes)
            return await _context.Movies
                .Where(m => !m.IsDeleted)
                .OrderByDescending(m => m.Showtimes
                    .SelectMany(st => st.Bookings)
                    .Count(b => !b.IsDeleted))
                .Take(top)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Movie>> GetTopRatingMoviesAsync(int top)
        {
            return await _context.Movies
                .Where(m => !m.IsDeleted)
                .OrderByDescending(m => m.Rating)
                .Take(top)
                .ToListAsync();
        }
    }
}