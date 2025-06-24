using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MovieTheater.Domain
{
    // Just using for migrations and design-time services - not using for real app pipine
    public class MovieTheaterDbContextFactory : IDesignTimeDbContextFactory<MovieTheaterDbContext>
    {
        public MovieTheaterDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MovieTheaterDbContext>();
            optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=movietheater_db;Username=postgres;Password=postgres");

            return new MovieTheaterDbContext(optionsBuilder.Options);
        }
    }
}
