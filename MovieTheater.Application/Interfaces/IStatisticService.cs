using MovieTheater.Domain.DTOs.StatisticDTOs;

namespace MovieTheater.Application.Interfaces
{
    public interface IStatisticService
    {
        Task<List<MonthlyRegisterDto>> GetRegisterPerMonthAsync();
        Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync();
        Task<List<MonthlyMovieRevenueDto>> GetMonthlyRevenueMovieAsync(MonthYearDto monthYear);
        Task<List<MonthlyFoodAndDrinkRevenueDto>> GetMonthlyFoodAndDrinkRevenueAsync(MonthYearDto monthYear);
        Task<MonthlyTicketTypeStatisticDto> GetMonthlyTicketTypeStatisticsAsync(MonthYearDto monthYear);
    }
}
