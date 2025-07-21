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
        public async Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync()
        {
            try
            {
                _loggerService.Info("Starting GetMonthlyRevenueAsync");
                var currentYear = DateTime.UtcNow.Year;
                var currentMonth = DateTime.UtcNow.Month;

                var tickets = _unitOfWork.Tickets.GetQueryable()
                    .Where(t => !t.IsDeleted);

                var monthlyData = new List<MonthlyRevenueDto>();
                for (int i = 0; i < 12; i++)
                {
                    var month = (currentMonth - i + 12) % 12;
                    var year = currentYear - (currentMonth - i < 1 ? 1 : 0);
                    if (month == 0) month = 12;

                    var totalRevenue = await tickets
                        .Where(t => t.CreatedAt.Year == year && t.CreatedAt.Month == month)
                        .SumAsync(t => (decimal?)t.TotalPrice) ?? 0;

                    monthlyData.Add(new MonthlyRevenueDto
                    {
                        Month = month,
                        Year = year,
                        TotalRevenue = totalRevenue
                    });
                }
                return monthlyData;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error in GetMonthlyRevenueAsync: {ex.Message}");
                throw;
            }
        }
        public async Task<List<MonthlyMovieRevenueDto>> GetMonthlyRevenueMovieAsync(MonthYearDto monthYear)
        {
            try
            {
                _loggerService.Info("Starting GetMonthlyRevenueMovieAsync");

                var tickets = await _unitOfWork.Tickets.GetQueryable()
                    .Where(t => !t.IsDeleted
                        && t.CreatedAt != DateTime.MinValue
                        && t.CreatedAt.Year == monthYear.Year
                        && t.CreatedAt.Month == monthYear.Month)
                    .Include(t => t.Showtime)
                    .ThenInclude(st => st.Movie)
                    .Include(t => t.TicketSeats)
                    .ToListAsync();

                var movieGroups = tickets
                    .Where(t => t.Showtime != null && t.Showtime.Movie != null)
                    .GroupBy(t => new
                    {
                        t.Showtime.MovieId,
                        t.Showtime.Movie.Name
                    });

                var monthlyData = new List<MonthlyMovieRevenueDto>();

                foreach (var g in movieGroups)
                {
                    decimal totalSeatRevenue = 0;

                    // Offline tickets: sum seat prices directly
                    var offlineTickets = g.Where(t => t.TicketType == TicketType.Offline);
                    totalSeatRevenue += offlineTickets
                        .SelectMany(t => t.TicketSeats)
                        .Sum(ts => ts.PricePerSeat);

                    // Online tickets: apply promotion and score discounts
                    var onlineTickets = g.Where(t => t.TicketType == TicketType.Online);
                    foreach (var ticket in onlineTickets)
                    {
                        var seatRevenue = ticket.TicketSeats.Sum(ts => ts.PricePerSeat);
                        var originalRevenue = seatRevenue;

                        if (!ticket.BookingId.HasValue)
                        {
                            _loggerService.Warn($"Ticket with ID {ticket.Id} has no BookingId, skipping online seat revenue calculation.");
                            continue;
                        }

                        // Get invoice and apply promotion discount
                        var invoice = await _unitOfWork.Invoices.GetQueryable()
                            .FirstOrDefaultAsync(i => i.BookingId == ticket.BookingId);

                        if (invoice != null && invoice.PromotionId.HasValue)
                        {
                            var promotion = await _unitOfWork.Promotions.GetByIdAsync(invoice.PromotionId.Value);
                            if (promotion != null)
                            {
                                seatRevenue -= originalRevenue * promotion.DiscountValue;
                            }
                        }

                        // Apply score history discount
                        var scoreHistory = await _unitOfWork.ScoreHistories.GetQueryable()
                            .FirstOrDefaultAsync(sh => sh.RelatedBookingId == ticket.BookingId);
                        if (scoreHistory != null)
                        {
                            seatRevenue -= originalRevenue * (scoreHistory.ScoreValue / 100m);
                        }

                        totalSeatRevenue += seatRevenue;
                    }

                    var totalTickets = g.Count();

                    monthlyData.Add(new MonthlyMovieRevenueDto
                    {
                        MovieId = g.Key.MovieId,
                        MovieName = g.Key.Name,
                        TotalRevenue = totalSeatRevenue,
                        TotalTickets = totalTickets
                    });
                }

                return monthlyData.OrderBy(x => x.TotalRevenue).ToList();
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error in GetMonthlyRevenueMovieAsync: {ex.Message}");
                throw;
            }
        }
    }
}