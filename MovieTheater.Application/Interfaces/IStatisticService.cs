using MovieTheater.Domain.DTOs.StatisticDTOs;

namespace MovieTheater.Application.Interfaces
{
    public interface IStatisticService
    {
        Task<List<MonthlyRegisterDto>> GetRegisterPerMonthAsync();
        Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync();
        Task<List<MonthlyMovieRevenueDto>> GetMonthlyRevenueMovieAsync(MonthYearDto monthYear);
    }
}
