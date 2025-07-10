using MovieTheater.Domain;
using MovieTheater.Domain.Entities;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly MovieTheaterDbContext _dbContext;

    public UnitOfWork(MovieTheaterDbContext dbContext,
        IGenericRepository<User> userRepository,
        IGenericRepository<OtpStorage> otpStorage,
        IGenericRepository<Movie> movies,
        IGenericRepository<ShowTime> showTimes,
        IGenericRepository<Promotion> promotions,
        IGenericRepository<CinemaRoom> cinemaRooms,
        IGenericRepository<AuditLog> aditLogs,
        IGenericRepository<FoodAndDrink> foodAndDrinks,
        IGenericRepository<Event> events,
        IGenericRepository<Seat> seats,
        IGenericRepository<ShowTimeSeat> showTimeSeats,
        IGenericRepository<Booking> bookings,
        IGenericRepository<BookingFood> bookingFoods,
        IGenericRepository<BookingSeat> bookingSeats,
        IGenericRepository<Invoice> invoices,
        IGenericRepository<Payment> payments,
        IGenericRepository<Ticket> tickets)
    {
        _dbContext = dbContext;
        Users = userRepository;
        OtpStorages = otpStorage;
        Movies = movies;
        ShowTimes = showTimes;
        Promotions = promotions;
        CinemaRooms = cinemaRooms;
        AuditLogs = aditLogs;
        FoodAndDrinks = foodAndDrinks;
        Events = events;
        Seats = seats;
        ShowTimeSeats = showTimeSeats;
        Bookings = bookings;
        BookingFoods = bookingFoods;
        BookingSeats = bookingSeats;
        Invoices = invoices;
        Payments = payments;
        Tickets = tickets;
    }

    public IGenericRepository<User> Users { get; }

    public IGenericRepository<OtpStorage> OtpStorages { get; }

    public IGenericRepository<Movie> Movies { get; }

    public IGenericRepository<ShowTime> ShowTimes { get; }

    public IGenericRepository<Promotion> Promotions { get; }

    public IGenericRepository<CinemaRoom> CinemaRooms { get; }

    public IGenericRepository<AuditLog> AuditLogs { get; }

    public IGenericRepository<FoodAndDrink> FoodAndDrinks { get; }

    public IGenericRepository<Event> Events { get; }
    public IGenericRepository<Seat> Seats { get; }
    public IGenericRepository<ShowTimeSeat> ShowTimeSeats { get; }
    public IGenericRepository<Booking> Bookings { get; }
    public IGenericRepository<BookingFood> BookingFoods { get; }
    public IGenericRepository<BookingSeat> BookingSeats { get; }
    public IGenericRepository<Invoice> Invoices { get; }
    public IGenericRepository<Payment> Payments { get; }
    public IGenericRepository<Ticket> Tickets { get; }
    public void Dispose()
    {
        _dbContext.Dispose();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _dbContext.SaveChangesAsync();
    }
}