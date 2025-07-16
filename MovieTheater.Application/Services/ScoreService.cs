using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Application.Services
{
    public class ScoreService  : IScoreService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _loggerService;
        private readonly IClaimsService _claimsService;

        public ScoreService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
            _claimsService = claimsService;
        }

        public async Task AddScoreForBookingAsync(User user, Booking booking)
        {
            try
            {
                if (user == null || booking == null)
                {
                    _loggerService.Error("[AddScoreForBookingAsync] User or Booking is null.");
                    throw new ArgumentNullException("User or Booking is invalid.");
                }

                int totalPoints = 0;
                if (booking.Tickets == null)
                {
                    _loggerService.Warn($"[AddScoreForBookingAsync] Booking {booking.Id} has no tickets.");
                }
                else
                {
                    foreach (var ticket in booking.Tickets)
                    {
                        foreach (var ticketSeat in ticket.TicketSeats)
                        {
                            var seat = ticketSeat.Seat;
                            if (seat == null)
                            {
                                seat = await _unitOfWork.Seats.GetByIdAsync(ticketSeat.SeatId);
                                if (seat == null)
                                {
                                    _loggerService.Warn($"[AddScoreForBookingAsync] Seat not found for TicketSeatId: {ticketSeat.Id}");
                                    continue;
                                }
                            }
                            totalPoints += seat.Type switch
                            {
                                SeatType.Normal => 50,
                                SeatType.VIP => 100,
                                SeatType.Couple => 200,
                                _ => 0
                            };
                        }
                    }
                }

                if (totalPoints > 0)
                {
                    user.ScoreBalance += totalPoints;
                    await _unitOfWork.Users.Update(user);

                    var history = new ScoreHistory
                    {
                        MemberId = user.Id,
                        ChangeDate = DateTime.UtcNow,
                        ChangeType = ScoreChangeType.Add,
                        ScoreValue = totalPoints,
                        RelatedBookingId = booking.Id
                    };
                    await _unitOfWork.ScoreHistories.AddAsync(history);
                    await _unitOfWork.SaveChangesAsync();

                    _loggerService.Success($"[AddScoreForBookingAsync] Added {totalPoints} points to user {user.Email} (BookingId: {booking.Id}).");
                }
                else
                {
                    _loggerService.Info($"[AddScoreForBookingAsync] No points added for booking {booking.Id}.");
                }
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[AddScoreForBookingAsync] Exception: {ex.Message}");
                throw;
            }
        }

        public (decimal discountPercent, int usedPoints) CalculateDiscount(int availablePoints, int requestedPoints)
        {
            if (availablePoints <= 0 || requestedPoints <= 0)
            {
                _loggerService.Warn("[CalculateDiscount] availablePoints or requestedPoints <= 0.");
                return (0, 0);
            }

            int usedPoints = Math.Min(availablePoints, requestedPoints);
            int percent = usedPoints / 10;
            decimal discountPercent = percent;

            _loggerService.Info($"[CalculateDiscount] Using {usedPoints} points, discount {discountPercent}%.");
            return (discountPercent, usedPoints);
        }

        public async Task UseScoreForBookingAsync(User user, Booking booking, int usedPoints)
        {
            try
            {
                if (user == null || booking == null)
                {
                    _loggerService.Error("[UseScoreForBookingAsync] User or Booking is null.");
                    throw new ArgumentNullException("User or Booking is invalid.");
                }

                if (usedPoints <= 0)
                {
                    _loggerService.Warn("[UseScoreForBookingAsync] Used points <= 0.");
                    throw new ArgumentException("Used points must be greater than 0.");
                }

                if (usedPoints > user.ScoreBalance)
                {
                    _loggerService.Warn($"[UseScoreForBookingAsync] User {user.Email} does not have enough points. Current: {user.ScoreBalance}, requested: {usedPoints}.");
                    throw new ArgumentException("Not enough points to use.");
                }

                user.ScoreBalance -= usedPoints;
                await _unitOfWork.Users.Update(user);

                var history = new ScoreHistory
                {
                    MemberId = user.Id,
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = ScoreChangeType.Use,
                    ScoreValue = -usedPoints,
                    RelatedBookingId = booking.Id
                };
                await _unitOfWork.ScoreHistories.AddAsync(history);
                await _unitOfWork.SaveChangesAsync();

                _loggerService.Success($"[UseScoreForBookingAsync] Deducted {usedPoints} points from user {user.Email} (BookingId: {booking.Id}).");
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[UseScoreForBookingAsync] Exception: {ex.Message}");
                throw;
            }
        }

        public async Task<int> GetCurrentScoreAsync()
        {
            var userId = _claimsService.GetCurrentUserId;
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null || user.IsDeleted)
            {
                _loggerService.Warn($"[GetCurrentScoreAsync] User not found: {userId}");
                throw new KeyNotFoundException("User not found.");
            }
            _loggerService.Info($"[GetCurrentScoreAsync] User {user.Email} has {user.ScoreBalance} points.");
            return user.ScoreBalance;
        }

        public async Task<List<ScoreHistory>> GetScoreHistoryAsync()
        {
            var userId = _claimsService.GetCurrentUserId;
            var histories = await _unitOfWork.ScoreHistories.GetAllAsync(h => h.MemberId == userId);
            _loggerService.Info($"[GetScoreHistoryAsync] Found {histories.Count} score history records for user {userId}.");
            return histories.OrderByDescending(h => h.ChangeDate).ToList();
        }
    }
}
