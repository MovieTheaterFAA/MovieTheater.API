using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.StatisticDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Application.Services
{
    public class StatisticService : IStatisticService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _loggerService;
        public StatisticService(IUnitOfWork unitOfWork, ILoggerService loggerService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
        }
        public async Task<List<MonthlyRegisterDto>> GetRegisterPerMonthAsync()
        {
            try
            {
                _loggerService.Info("Starting GetRegisterPerMonthAsync");
                var currentYear = DateTime.UtcNow.Year;
                var currentMonth = DateTime.UtcNow.Month;

                var Registers = _unitOfWork.Users.GetQueryable()
                    .Where(r => r.Role == RoleType.Member && !r.IsDeleted && r.CreatedAt != DateTime.MinValue);
                var monthlyData = new List<MonthlyRegisterDto>();
                // Assuming you want to get data for the last 12 months  
                for (int i = 0; i < 12; i++)
                {
                    var month = (currentMonth - i + 12) % 12;
                    var year = currentYear - (currentMonth - i < 1 ? 1 : 0);
                    if (month == 0) month = 12; // Adjust for zero-based month
                    var dataPerMonth = new MonthlyRegisterDto
                    {
                        Month = month,
                        Year = year,
                        TotalRegisters = await Registers.CountAsync(u => u.CreatedAt.Year == year && u.CreatedAt.Month == month)
                    };
                    monthlyData.Add(dataPerMonth);
                }
                return monthlyData;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error in GetRegisterPerMonthAsync: {ex.Message}");
                throw;
            }
        }
    }
}