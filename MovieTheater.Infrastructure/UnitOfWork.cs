using MovieTheater.Domain;
using MovieTheater.Domain.Entities;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly MovieTheaterDbContext _dbContext;

    public UnitOfWork(MovieTheaterDbContext dbContext,
        IGenericRepository<User> userRepository)
    {
        _dbContext = dbContext;
        Users = userRepository;
    }

    public IGenericRepository<User> Users { get; }
    //public IGenericRepository<OtpVerification> OtpVerifications { get; }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _dbContext.SaveChangesAsync();
    }
}