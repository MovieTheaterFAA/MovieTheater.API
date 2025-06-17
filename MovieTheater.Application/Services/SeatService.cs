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

                var seats = await _unitOfWork.Seats.GetQueryable()
                    .Where(s => s.CinemaRoomId == showTime.CinemaRoomId)
                    .ToListAsync();

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

        public async Task<List<SeatResponseDto>> HoldSeatsAsync(Guid userId, Guid showTimeId, List<Guid> seatIds)
        {
            try
            {
                _loggerService.Info($"User {userId} is attempting to hold seats for showtime {showTimeId}: {string.Join(", ", seatIds)}");

                CleanupExpiredHolds();

                var now = DateTime.UtcNow;
                var expireAt = now.AddMinutes(5);

                var currentHeldCount = _holdingSeats
                    .Count(h => h.Key.showTimeId == showTimeId && h.Value.userId == userId && h.Value.expireAt > now);

                var newSeatCount = seatIds
                    .Where(seatId =>
                        !_holdingSeats.TryGetValue((seatId, showTimeId), out var holdInfo) ||
                        holdInfo.userId != userId ||
                        holdInfo.expireAt <= now)
                    .ToList();

                if (currentHeldCount + newSeatCount.Count > 8)
                {
                    _loggerService.Warn($"User {userId} is trying to hold more than 8 seats for showtime {showTimeId}.");
                    return new List<SeatResponseDto>();
                }

                var showExist = await _unitOfWork.ShowTimes.GetByIdAsync(showTimeId);
                if (showExist == null)
                {
                    _loggerService.Warn($"Showtime not found: {showTimeId}");
                    throw new KeyNotFoundException("Showtime not found.");
                }

                var allSeatsInRoom = await _unitOfWork.Seats.GetQueryable()
                    .Where(s => s.CinemaRoomId == showExist.CinemaRoomId)
                    .ToListAsync();

                if (allSeatsInRoom.Count == 0)
                {
                    _loggerService.Warn($"No seats found for cinema room {showExist.CinemaRoomId}.");
                    throw new KeyNotFoundException("No seats found for the specified cinema room.");
                }

                var seatSet = allSeatsInRoom.Select(s => s.Id).ToHashSet();
                var seatDict = allSeatsInRoom.ToDictionary(s => s.Id, s => s);

                var showTimeSeats = await _unitOfWork.ShowTimeSeats.GetQueryable()
                    .Where(sts => sts.ShowTimeId == showTimeId)
                    .ToDictionaryAsync(sts => sts.SeatId, sts => sts);

                List<SeatResponseDto> heldSeats = new();

                foreach (var seatId in seatIds)
                {
                    if (!seatSet.Contains(seatId))
                    {
                        _loggerService.Warn($"Seat {seatId} is not in cinema room {showExist.CinemaRoomId}.");
                        throw new ArgumentException($"Seat {seatId} does not exist in the specified cinema room.");
                    }

                    var key = (seatId, showTimeId);
                    if (_holdingSeats.TryGetValue(key, out var holdInfo) &&
                        holdInfo.expireAt > now && holdInfo.userId != userId)
                    {
                        _loggerService.Warn($"Seat {seatId} is already held by another user.");
                        continue;
                    }

                    if (showTimeSeats.TryGetValue(seatId, out var sts) &&
                        (sts.Status == SeatStatus.Booked || sts.Status == SeatStatus.Sold))
                    {
                        _loggerService.Warn($"Seat {seatId} is already {sts.Status}.");
                        continue;
                    }
                    _holdingSeats[(seatId, showTimeId)] = (userId, expireAt);

                    var seat = seatDict[seatId];
                    heldSeats.Add(new SeatResponseDto
                    {
                        Row = seat.Row,
                        Number = seat.Number,
                        Type = seat.Type
                    });
                }

                _loggerService.Success($"User {userId} successfully held seats for showtime {showTimeId}: {string.Join(", ", heldSeats.Select(s => $"{s.Row}{s.Number}"))}");

                return heldSeats;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error holding seats for user {userId} for showtime {showTimeId}: {ex.Message}");
                throw new InvalidOperationException("An error occurred while holding seats.", ex);
            }
        }

        public async Task<ShowTimeSeatDto> GetSeatByIdAsync(Guid seatId)
        {
            try
            {
                _loggerService.Info($"Retrieving seat with ID {seatId}");
                var seat = await _unitOfWork.Seats.GetByIdAsync(seatId);
                if (seat == null)
                {
                    _loggerService.Warn($"Seat not found: {seatId}");
                    throw new KeyNotFoundException("Seat not found.");
                }
                var showTimeSeat = await _unitOfWork.ShowTimeSeats.GetQueryable()
                    .FirstOrDefaultAsync(sts => sts.SeatId == seatId);
                if (showTimeSeat == null)
                {
                    _loggerService.Warn($"ShowTimeSeat not found for seat ID: {seatId}");
                    throw new KeyNotFoundException("ShowTimeSeat not found for the specified seat.");
                }
                var result = new ShowTimeSeatDto
                {
                    SeatId = seat.Id,
                    Row = seat.Row,
                    Number = seat.Number,
                    Type = seat.Type,
                    Status = showTimeSeat.Status
                };
                _loggerService.Success($"Successfully retrieved seat with ID {seatId}");
                return result;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error retrieving seat with ID {seatId}: {ex.Message}");
                throw new InvalidOperationException("An error occurred while retrieving the seat.", ex);
            }
        }
    }
}