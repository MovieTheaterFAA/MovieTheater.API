using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.SeatDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System.Collections.Concurrent;

namespace MovieTheater.Application.Services
{
    public class SeatService : ISeatService
    {
        private readonly ILoggerService _loggerService;
        private readonly IUnitOfWork _unitOfWork;
        private static readonly ConcurrentDictionary<(Guid seatId, Guid showTimeId), (Guid userId, DateTime expireAt)> _holdingSeats = new();

        public SeatService(ILoggerService loggerService, IUnitOfWork unitOfWork)
        {
            _loggerService = loggerService;
            _unitOfWork = unitOfWork;
        }

        private static void CleanupExpiredHolds()
        {
            var now = DateTime.UtcNow;
            foreach (var entry in _holdingSeats)
            {
                if (entry.Value.expireAt <= now)
                {
                    _holdingSeats.TryRemove(entry.Key, out _);
                }
            }
        }

        public async Task<List<ShowTimeSeatDto>> GetSeatsByShowTimeAsync(Guid showTimeId)
        {
            try
            {
                _loggerService.Info($"Retrieving seats for showtime {showTimeId}");

                CleanupExpiredHolds();
                var showTime = await _unitOfWork.ShowTimes.GetByIdAsync(showTimeId);
                if (showTime == null)
                {
                    _loggerService.Warn($"Showtime not found for showTimeId: {showTimeId}");
                    throw new KeyNotFoundException("Showtime not found.");
                }

                // Get all seats in the cinema room
                var seats = await _unitOfWork.Seats.GetQueryable()
                    .Where(s => s.CinemaRoomId == showTime.CinemaRoomId)
                    .ToListAsync();

                // Get all ShowTimeSeat for this showtime
                var showTimeSeats = await _unitOfWork.ShowTimeSeats.GetQueryable()
                    .Where(sts => sts.ShowTimeId == showTimeId)
                    .ToListAsync();

                var result = seats.Select(seat =>
                {
                    var sts = showTimeSeats.FirstOrDefault(x => x.SeatId == seat.Id);
                    return new ShowTimeSeatDto
                    {
                        SeatId = seat.Id,
                        Row = seat.Row,
                        Number = seat.Number,
                        Type = seat.Type,
                        Status = sts?.Status ?? (_holdingSeats.Any(h =>
                                                h.Key.seatId == seat.Id &&
                                                h.Key.showTimeId == showTimeId &&
                                                h.Value.expireAt > DateTime.UtcNow)
                                                ? SeatStatus.Holding : SeatStatus.Available)


                    };
                }).ToList();

                _loggerService.Success($"Successfully retrieved seats for showtime {showTimeId} with {result.Count} seats.");
                return result;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error retrieving seats for showtime {showTimeId}: {ex.Message}");
                throw new InvalidOperationException("An error occurred while retrieving seats.", ex);
            }
        }

        public async Task<bool> HoldSeatsAsync(Guid userId, Guid showTimeId, List<Guid> seatIds)
        {
            try
            {
                _loggerService.Info($"User {userId} is attempting to hold seats for showtime {showTimeId}: {string.Join(", ", seatIds)}");

                CleanupExpiredHolds();

                var now = DateTime.UtcNow;
                var expireAt = now.AddMinutes(10);

                var showExist = await _unitOfWork.ShowTimes.GetByIdAsync(showTimeId);
                if (showExist == null)
                {
                    _loggerService.Warn($"Showtime not found: {showTimeId}");
                    throw new KeyNotFoundException("Showtime not found.");
                }

                var allSeatsInRoom = await _unitOfWork.Seats.GetQueryable()
                    .Where(s => s.CinemaRoomId == showExist.CinemaRoomId)
                    .ToListAsync();

                if (!allSeatsInRoom.Any())
                {
                    _loggerService.Warn($"No seats found for cinema room {showExist.CinemaRoomId}.");
                    throw new KeyNotFoundException("No seats found for the specified cinema room.");
                }

                var seatSet = allSeatsInRoom.Select(s => s.Id).ToHashSet();

                var showTimeSeats = await _unitOfWork.ShowTimeSeats.GetQueryable()
                    .Where(sts => sts.ShowTimeId == showTimeId)
                    .ToDictionaryAsync(sts => sts.SeatId, sts => sts);

                foreach (var seatId in seatIds)
                {
                    if (!seatSet.Contains(seatId))
                    {
                        _loggerService.Warn($"Seat {seatId} is not in cinema room {showExist.CinemaRoomId}.");
                        throw new ArgumentException($"Seat {seatId} does not exist in the specified cinema room.");
                    }

                    // Nếu seat đang bị giữ bởi user khác
                    var key = (seatId, showTimeId);
                    if (_holdingSeats.TryGetValue(key, out var holdInfo) &&
                        holdInfo.expireAt > now && holdInfo.userId != userId)
                    {
                        _loggerService.Warn($"Seat {seatId} is already held by another user.");
                        return false;
                    }

                    // Kiểm tra trạng thái nếu đã có ShowTimeSeat
                    if (showTimeSeats.TryGetValue(seatId, out var sts) &&
                        (sts.Status == SeatStatus.Booked || sts.Status == SeatStatus.Sold))
                    {
                        _loggerService.Warn($"Seat {seatId} is already {sts.Status}.");
                        return false;
                    }
                }

                foreach (var seatId in seatIds)
                    _holdingSeats[(seatId, showTimeId)] = (userId, expireAt);

                _loggerService.Success($"User {userId} successfully held seats for showtime {showTimeId}: {string.Join(", ", seatIds)}");

                return true;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error holding seats for user {userId} for showtime {showTimeId}: {ex.Message}");
                throw new InvalidOperationException("An error occurred while holding seats.", ex);
            }
        }
    }
}