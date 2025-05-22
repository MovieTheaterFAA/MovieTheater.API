using MovieTheater.Domain.Entities;

namespace MovieTheater.Infrastructure.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }
    Task<int> SaveChangesAsync();
}