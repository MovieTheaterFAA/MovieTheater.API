using MovieTheater.Domain.Entities;

namespace MovieTheater.Infrastructure.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }
    IGenericRepository<OtpStorage> OtpStorages { get; }
    IGenericRepository<Movie> Movies { get; }
    IGenericRepository<ShowTime> ShowTimes { get; }
    IGenericRepository<Promotion> Promotions { get; }
    IGenericRepository<CinemaRoom> CinemaRooms { get; }
    IGenericRepository<AuditLog> AuditLogs { get; }
    IGenericRepository<FoodAndDrink> FoodAndDrinks { get; }
    IGenericRepository<Event> Events { get; }
    IGenericRepository<Seat> Seats { get; }
    IGenericRepository<ShowTimeSeat> ShowTimeSeats { get; }
    IGenericRepository<Booking> Bookings { get; }
    IGenericRepository<BookingSeat> BookingSeats { get; }
    IGenericRepository<BookingFood> BookingFoods { get; }
    IGenericRepository<Invoice> Invoices { get; }
    IGenericRepository<Payment> Payments { get; }
    IGenericRepository<ClaimedPromotion> ClaimedPromotions { get; }
    IGenericRepository<Ticket> Tickets { get; }
    IGenericRepository<ScoreHistory> ScoreHistories { get; }
    IGenericRepository<TicketSeat> TicketSeats { get; }
    IGenericRepository<TicketFoodAndDrink> TicketFoodAndDrinks { get; }
    Task<int> SaveChangesAsync();
}