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
                        _loggerService.Info($"Calculating revenue for Ticket {ticket.Id} with {onlineTicketSeats.Count} seats. Initial seat revenue: {seatRevenue}");
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
                            _loggerService.Info($"Applied promotion with discount {promotion.DiscountValue} to tickets {ticket.Id}. New seat revenue: {seatRevenue}");
                        }

                        var scoreHistory = await _unitOfWork.ScoreHistories.GetQueryable()
                            .FirstOrDefaultAsync(sh => sh.RelatedBookingId == ticket.BookingId && sh.ChangeType == ScoreChangeType.Use);

                        if (scoreHistory != null)
                        {
                            var scoreDiscount = Math.Min(Math.Abs(scoreHistory.ScoreValue), 100m) / 100m;
                            seatRevenue -= originalRevenue * scoreDiscount;
                            _loggerService.Info($"Applied score deduction of {Math.Abs(scoreHistory.ScoreValue)} to tickets {ticket.Id}. New seat revenue: {seatRevenue}");
                        }

                        seatRevenue = Math.Max(0, seatRevenue);

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

        public async Task<List<MonthlyFoodAndDrinkRevenueDto>> GetMonthlyFoodAndDrinkRevenueAsync(MonthYearDto monthYear)
        {
            try
            {
                _loggerService.Info("Starting GetMonthlyFoodAndDrinkRevenueAsync");

                var tickets = await _unitOfWork.Tickets.GetQueryable()
                    .Where(t => !t.IsDeleted && t.CreatedAt.Year == monthYear.Year && t.CreatedAt.Month == monthYear.Month)
                    .Include(t => t.TicketFoodAndDrinks).ThenInclude(t => t.FoodAndDrink)
                    .ToListAsync();

                var groupedByFood = tickets
                    .Where(t => t.TicketFoodAndDrinks != null && t.TicketFoodAndDrinks.Any())
                    .SelectMany(t => t.TicketFoodAndDrinks)
                    .GroupBy(fd => new
                    {
                        fd.FoodAndDrinkId,
                        fd.FoodAndDrink.Name
                    });

                var monthlyData = new List<MonthlyFoodAndDrinkRevenueDto>();
                foreach (var group in groupedByFood)
                {
                    _loggerService.Info($"Calculating revenue for Food and Drink {group.Key.Name} (ID: {group.Key.FoodAndDrinkId}) in month {monthYear.Month}/{monthYear.Year}");

                    decimal totalRevenue = 0;
                    int totalSold = group.Sum(fd => fd.Quantity);
                    _loggerService.Info($"Total sold for Food and Drink {group.Key.Name} (ID: {group.Key.FoodAndDrinkId}) in month {monthYear.Month}/{monthYear.Year}: {totalSold}");

                    // Offline food and drinks revenue
                    var offlineTickets = group
                        .Where(fd => fd.Ticket.TicketType == TicketType.Offline)
                        .ToList();

                    if (offlineTickets.Any())
                    {
                        totalRevenue += offlineTickets.Sum(fd => fd.FoodAndDrink.Price * fd.Quantity);
                        _loggerService.Info($"Offline food and drinks revenue for {group.Key.Name} (ID: {group.Key.FoodAndDrinkId}) in month {monthYear.Month}/{monthYear.Year}: {totalRevenue}");
                    }

                    var onlineTickets = group
                        .Where(fd => fd.Ticket.TicketType == TicketType.Online)
                        .ToList();
                    if (onlineTickets.Any())
                    {
                        foreach (var ticketFoodAndDrink in onlineTickets)
                        {
                            var originalPrice = ticketFoodAndDrink.FoodAndDrink.Price * ticketFoodAndDrink.Quantity;
                            var seatRevenue = originalPrice;
                            _loggerService.Info($"Calculating revenue for Ticket {ticketFoodAndDrink.Ticket.Id} with Food and Drink {ticketFoodAndDrink.FoodAndDrink.Name}. Initial seat revenue: {seatRevenue}");

                            if (!ticketFoodAndDrink.Ticket.BookingId.HasValue)
                            {
                                _loggerService.Warn($"[FoodRevenueCalc] Missing BookingId on Ticket {ticketFoodAndDrink.Ticket.Id}");
                                throw new InvalidOperationException($"Ticket {ticketFoodAndDrink.Ticket.Id} missing BookingId.");
                            }

                            var invoice = await _unitOfWork.Invoices.GetQueryable()
                                .FirstOrDefaultAsync(i => i.BookingId == ticketFoodAndDrink.Ticket.BookingId);
                            if (invoice == null)
                            {
                                _loggerService.Warn($"[FoodRevenueCalc] Invoice not found for Ticket {ticketFoodAndDrink.Ticket.Id} with BookingId {ticketFoodAndDrink.Ticket.BookingId}");
                                throw new InvalidOperationException($"Invoice not found for Ticket {ticketFoodAndDrink.Ticket.Id} with BookingId {ticketFoodAndDrink.Ticket.BookingId}.");
                            }

                            if (invoice.PromotionId.HasValue)
                            {
                                var promotion = await _unitOfWork.Promotions.GetByIdAsync(invoice.PromotionId.Value);
                                if (promotion == null)
                                    throw new InvalidOperationException($"Promotion {invoice.PromotionId} not found for Invoice {invoice.Id}.");
                                seatRevenue -= originalPrice * promotion.DiscountValue;
                                _loggerService.Info($"Applied promotion with discount {promotion.DiscountValue} to food and drink {group.Key.Name}. New seat revenue: {seatRevenue}");
                            }
                            var scoreHistory = await _unitOfWork.ScoreHistories.GetQueryable()
                                .FirstOrDefaultAsync(sh => sh.RelatedBookingId == ticketFoodAndDrink.Ticket.BookingId && sh.ChangeType == ScoreChangeType.Use);
                            if (scoreHistory != null)
                            {
                                var scoreDiscount = Math.Min(Math.Abs(scoreHistory.ScoreValue), 100m) / 100m;
                                seatRevenue -= originalPrice * scoreDiscount;
                                _loggerService.Info($"Applied score deduction of {Math.Abs(scoreHistory.ScoreValue)} to food and drink {group.Key.Name}. New seat revenue: {seatRevenue}");
                            }

                            seatRevenue = Math.Max(0, seatRevenue);
                            totalRevenue += seatRevenue;
                            _loggerService.Info($"Ticket {ticketFoodAndDrink.Ticket.Id} revenue calculated: {seatRevenue}, Total Revenue: {totalRevenue}");
                        }
                    }
                    _loggerService.Info($"Total revenue for Food and Drink {group.Key.Name} (ID: {group.Key.FoodAndDrinkId}) in month {monthYear.Month}/{monthYear.Year}: {totalRevenue}");
                    monthlyData.Add(new MonthlyFoodAndDrinkRevenueDto
                    {
                        FoodAndDrinkId = group.Key.FoodAndDrinkId,
                        FoodAndDrinkName = group.Key.Name,
                        TotalRevenue = totalRevenue,
                        TotalSold = totalSold
                    });
                }
                return monthlyData.OrderBy(x => x.TotalRevenue).ToList();
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[FoodRevenueCalc] Exception: {ex.Message}");
                throw;
            }
        }
        public async Task<MonthlyTicketTypeStatisticDto> GetMonthlyTicketTypeStatisticsAsync(MonthYearDto monthYear)
        {
            try
            {
                _loggerService.Info($"Starting GetMonthlyTicketTypeStatisticsAsync for {monthYear.Month}/{monthYear.Year}");

                var tickets = await _unitOfWork.Tickets.GetQueryable()
                    .Where(t => !t.IsDeleted
                                && t.CreatedAt.Year == monthYear.Year
                                && t.CreatedAt.Month == monthYear.Month)
                    .ToListAsync();
                var totalTickets = tickets.Count;

                var memberPhoneNumbers = await _unitOfWork.Users.GetQueryable()
                    .Where(u => u.Role == RoleType.Member && !u.IsDeleted && !string.IsNullOrEmpty(u.PhoneNumber))
                    .Select(u => u.PhoneNumber)
                    .ToListAsync();

                int onlineCount = tickets.Count(t => t.TicketType == TicketType.Online);
                int offlineCount = tickets.Count(t => t.TicketType == TicketType.Offline);

                int guestCount = tickets.Count(t =>
                    !string.IsNullOrEmpty(t.GuestPhoneNumber) &&
                    !memberPhoneNumbers.Contains(t.GuestPhoneNumber)
                );

                return new MonthlyTicketTypeStatisticDto
                {
                    OnlineTicketCount = onlineCount,
                    OfflineTicketCount = offlineCount - guestCount,
                    GuestTicketCount = guestCount,
                    TicketCount = totalTickets
                };
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error in GetMonthlyTicketTypeStatisticsAsync: {ex.Message}");
                throw;
            }
        }
    }
}