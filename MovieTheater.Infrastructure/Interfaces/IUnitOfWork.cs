using MovieTheater.Domain.Entities;

namespace MovieTheater.Infrastructure.Interfaces;

public interface IUnitOfWork : IDisposable
{

    Task<int> SaveChangesAsync();
}