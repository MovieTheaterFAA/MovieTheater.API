using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;

namespace MovieTheater.Application.Interfaces
{
    public interface IDataAnalyzerService
    {
        Task<IReadOnlyList<Movie>> GetMostBookedMoviesAsync(int top);

        Task<IReadOnlyList<Movie>> GetTopRatingMoviesAsync(int top);

        //==================== Freestyle ====================
        Task<IReadOnlyList<Movie>> GetAllMoviesAsync();

        Task<IReadOnlyList<FoodAndDrink>> GetAllFoodAndDrinksAsync();

        Task<IReadOnlyList<Event>> GetAllEventsAsync();

        Task<IReadOnlyList<Promotion>> GetAllPromotionsAsync();

        Task<IReadOnlyList<CinemaRoom>> GetAllCinemaRoomsAsync();

        Task<IReadOnlyList<SeatType>> GetAllSeatTypesAsync();
    }
}