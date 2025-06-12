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

    Task<int> SaveChangesAsync();
}