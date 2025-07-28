using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Domain;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;

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

        public async Task<IReadOnlyList<Movie>> GetAllMoviesAsync()
        {
            var movies = await _context.Movies
                .Where(m => !m.IsDeleted)
                .ToListAsync();

            return movies;
        }

        public async Task<IReadOnlyList<FoodAndDrink>> GetAllFoodAndDrinksAsync()
        {
            return await _context.FoodAndDrinks
                .Where(f => !f.IsDeleted)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Event>> GetAllEventsAsync()
        {
            return await _context.Events
                .Where(e => !e.IsDeleted)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Promotion>> GetAllPromotionsAsync()
        {
            return await _context.Promotions
                .Include(p => p.Event)
                .Where(p => !p.IsDeleted && p.Event != null && !p.Event.IsDeleted)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<CinemaRoom>> GetAllCinemaRoomsAsync()
        {
            return await _context.CinemaRooms
                .Where(r => !r.IsDeleted)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<SeatType>> GetAllSeatTypesAsync()
        {
            var seatTypes = System.Enum.GetValues(typeof(SeatType))
                .Cast<SeatType>()
                .ToList()
                .AsReadOnly();
            return await Task.FromResult((IReadOnlyList<SeatType>)seatTypes);
        }

        public async Task<IReadOnlyList<ShowTime>> GetAllShowTimesAsync()
        {
            return await _context.Showtimes
                .Where(st => !st.IsDeleted)
                .ToListAsync();
        }
    }
}