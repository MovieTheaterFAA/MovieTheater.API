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
                                && t.CreatedAt.Year == monthYear.Year
                                && t.CreatedAt.Month == monthYear.Month)
                    .Include(t => t.Showtime).ThenInclude(st => st.Movie)
                    .Include(t => t.TicketSeats)
                    .ToListAsync();

                var groupedByMovie = tickets
                    .GroupBy(t => new { t.Showtime.MovieId, MovieName = t.Showtime.Movie.Name });

                var monthlyData = new List<MonthlyMovieRevenueDto>();

                foreach (var group in groupedByMovie)
                {
                    decimal totalRevenue = 0;

                    // Offline tickets revenue
                    var offlineTickets = group.Where(t => t.TicketType == TicketType.Offline).ToList();

                    if (offlineTickets.Any())
                    {
                        var offlineTicketSeats = await _unitOfWork.TicketSeats.GetQueryable()
                        .Where(ts => offlineTickets.Select(t => t.Id).Contains(ts.TicketId))
                        .ToListAsync();
                        totalRevenue = offlineTicketSeats.Sum(ts => ts.PricePerSeat);
                        _loggerService.Info($"Offline tickets revenue for movie {group.Key.MovieName} (ID: {group.Key.MovieId}) in month {monthYear.Month}/{monthYear.Year}: {totalRevenue}");
                    }


                    // Online tickets revenue
                    var onlineTickets = group.Where(t => t.TicketType == TicketType.Online).ToList();

                    foreach (var ticket in onlineTickets)
                    {

                        var onlineTicketSeats = await _unitOfWork.TicketSeats.GetQueryable()
                            .Where(onts => onts.TicketId == ticket.Id).ToListAsync();

                        var seatRevenue = onlineTicketSeats.Sum(ts => ts.PricePerSeat);
                        var originalRevenue = seatRevenue;

                        if (!ticket.BookingId.HasValue)
                        {
                            _loggerService.Warn($"[RevenueCalc] Missing BookingId on Ticket {ticket.Id}");
                            throw new InvalidOperationException($"Ticket {ticket.Id} missing BookingId.");
                        }

                        var invoice = await _unitOfWork.Invoices.GetQueryable()
                            .FirstOrDefaultAsync(i => i.BookingId == ticket.BookingId);
                        if (invoice == null)
                        {
                            _loggerService.Warn($"[RevenueCalc] Invoice not found for Ticket {ticket.Id} with BookingId {ticket.BookingId}");
                            throw new InvalidOperationException($"Invoice not found for Ticket {ticket.Id} with BookingId {ticket.BookingId}.");
                        }

                        if (invoice.PromotionId.HasValue)
                        {
                            var promotion = await _unitOfWork.Promotions.GetByIdAsync(invoice.PromotionId.Value);
                            if (promotion == null)
                                throw new InvalidOperationException($"Promotion {invoice.PromotionId} not found for Invoice {invoice.Id}.");
                            seatRevenue -= originalRevenue * promotion.DiscountValue;
                            _loggerService.Info($"Applied promotion with discount {promotion.DiscountValue} to ticket {ticket.Id}. New seat revenue: {seatRevenue}");
                        }

                        var scoreHistory = await _unitOfWork.ScoreHistories.GetQueryable()
                            .FirstOrDefaultAsync(sh => sh.RelatedBookingId == ticket.BookingId && sh.ChangeType == ScoreChangeType.Use);

                        if (scoreHistory != null)
                        {
                            seatRevenue -= originalRevenue * (Math.Abs(scoreHistory.ScoreValue) / 100m);
                            _loggerService.Info($"Applied score deduction of {Math.Abs(scoreHistory.ScoreValue)} to ticket {ticket.Id}. New seat revenue: {seatRevenue}");
                        }

                        totalRevenue += seatRevenue;
                        _loggerService.Info($"Ticket {ticket.Id} revenue calculated: {seatRevenue}, Total Revenue: {totalRevenue}");
                    }
                    _loggerService.Info($"Total revenue for movie {group.Key.MovieName} (ID: {group.Key.MovieId}) in month {monthYear.Month}/{monthYear.Year}: {totalRevenue}");

                    monthlyData.Add(new MonthlyMovieRevenueDto
                    {
                        MovieId = group.Key.MovieId,
                        MovieName = group.Key.MovieName,
                        TotalRevenue = totalRevenue,
                        TotalTickets = group.Count()
                    });
                }

                return monthlyData.OrderBy(x => x.TotalRevenue).ToList();
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[RevenueCalc] Exception: {ex.Message}");
                throw;
            }
        }

    }
}