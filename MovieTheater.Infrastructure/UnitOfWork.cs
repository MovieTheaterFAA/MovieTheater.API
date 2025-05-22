using MovieTheater.Domain;
using MovieTheater.Domain.Entities;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly MovieTheaterDbContext _dbContext;

    public UnitOfWork(MovieTheaterDbContext dbContext,
        IGenericRepository<User> userRepository,
        IGenericRepository<OtpStorage> otpStorage)
    {
        _dbContext = dbContext;
        Users = userRepository;
        OtpStorages = otpStorage;
    }

    public IGenericRepository<User> Users { get; }

    public IGenericRepository<OtpStorage> OtpStorages { get; }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _dbContext.SaveChangesAsync();
    }
}