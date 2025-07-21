using MovieTheater.Domain.DTOs.StatisticDTOs;

namespace MovieTheater.Application.Interfaces
{
    public interface IStatisticService
    {
        Task<List<MonthlyRegisterDto>> GetRegisterPerMonthAsync();
    }
}
