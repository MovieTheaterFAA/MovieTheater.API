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
        IGenericRepository<CinemaRoom> cinemaRooms)
    {
        _dbContext = dbContext;
        Users = userRepository;
        OtpStorages = otpStorage;
        Movies = movies;
        ShowTimes = showTimes;
        Promotions = promotions;
        CinemaRooms = cinemaRooms;
    }

    public IGenericRepository<User> Users { get; }

    public IGenericRepository<OtpStorage> OtpStorages { get; }

    public IGenericRepository<Movie> Movies { get; }

    public IGenericRepository<ShowTime> ShowTimes { get; }
    public IGenericRepository<Promotion> Promotions { get; }

    public IGenericRepository<CinemaRoom> CinemaRooms { get; }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _dbContext.SaveChangesAsync();
    }
}