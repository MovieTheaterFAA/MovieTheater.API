using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Hubs;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.SeatDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;

namespace MovieTheater.Application.Services
{
    public class SeatService : ISeatService
    {
        private readonly ILoggerService _loggerService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogService _auditLogService;
        private readonly IRedisService _redisService;
        private readonly IHubContext<SeatHub> _seatHub;
        private static readonly ConcurrentDictionary<(Guid seatId, Guid showTimeId), (Guid userId, DateTime expireAt)> _holdingSeats = new();

        public SeatService(ILoggerService loggerService, IUnitOfWork unitOfWork, IHubContext<SeatHub> seatHub, IAuditLogService auditLogService, IRedisService redisService)
        {
            _loggerService = loggerService;
            _unitOfWork = unitOfWork;
            _seatHub = seatHub;
            _auditLogService = auditLogService;
            _redisService = redisService;
        }

        ///================== Admin Methods ===================///
        public async Task<List<SeatDto>> GetSeatsByCinemaRoomAsync(Guid cinemaRoomId)
        {
            string cacheKey = $"seat:list:cinemaroom:{cinemaRoomId}";
            try
            {
                var cached = await _redisService.GetAsync<List<SeatDto>>(cacheKey);
                if (cached != null)
                {
                    _loggerService.Info($"[CACHE HIT] {cacheKey}");
                    return cached;
                }

                _loggerService.Info($"[CACHE MISS] {cacheKey} — Fetching from DB");
                var seats = await _unitOfWork.Seats.GetQueryable()
                    .Where(s => s.CinemaRoomId == cinemaRoomId && !s.IsDeleted)
                    .Select(s => new SeatDto
                    {
                        Id = s.Id,
                        Row = s.Row,
                        Number = s.Number,
                        Type = s.Type,
                        CinemaRoomId = s.CinemaRoomId
                    })
                    .ToListAsync();

                await _redisService.SetAsync(cacheKey, seats, TimeSpan.FromMinutes(5));
                _loggerService.Success($"[SeatManagementService] Retrieved {seats.Count} seats for cinema room {cinemaRoomId}");
                return seats;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[SeatManagementService] Error fetching seats: {ex.Message}");
                throw new Exception("An error occurred while fetching seats for the cinema room.");
            }
        }

        public async Task<List<SeatDto>> BatchCreateSeatsAsync(Guid cinemaRoomId, BatchCreateSeatDto dto, Guid adminId)
        {
            try
            {
                _loggerService.Info($"[SeatManagementService] Batch creating seats for cinema room {cinemaRoomId}");

                var room = await _unitOfWork.CinemaRooms.GetByIdAsync(cinemaRoomId);
                if (room == null || room.IsDeleted)
                {
                    _loggerService.Warn($"[SeatManagementService] Cinema room {cinemaRoomId} not found.");
                    throw new KeyNotFoundException("Cinema room not found.");
                }

                var newSeats = dto.Seats.Select(s => new Seat
                {
                    Id = Guid.NewGuid(),
                    CinemaRoomId = cinemaRoomId,
                    Row = s.Row,
                    Number = s.Number,
                    Type = s.Type,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = adminId
                }).ToList();

                await _unitOfWork.Seats.AddRangeAsync(newSeats);
                await _unitOfWork.SaveChangesAsync();
                await _redisService.RemoveAsync($"seat:list:cinemaroom:{cinemaRoomId}");

                var newSeatData = newSeats.Select(s => new
                {
                    s.Id,
                    s.Row,
                    s.Number,
                    s.Type,
                    s.CinemaRoomId
                }).ToList();

                await _auditLogService.LogAsync(
                    adminId,
                    AuditActionType.Create,
                    "Seat",
                    cinemaRoomId,
                    null,
                    newSeatData,
                    JsonSerializer.Serialize(dto),
                    "Batch created seats"
                );

                _loggerService.Success($"[SeatManagementService] Batch created {newSeats.Count} seats for cinema room {cinemaRoomId}");

                return newSeats.Select(s => new SeatDto
                {
                    Id = s.Id,
                    Row = s.Row,
                    Number = s.Number,
                    Type = s.Type,
                    CinemaRoomId = s.CinemaRoomId
                }).ToList();
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[SeatManagementService] Error batch creating seats: {ex.Message}");
                throw new Exception("An error occurred while batch creating seats.");
            }
        }

        public async Task<SeatDto?> UpdateSeatAsync(Guid seatId, UpdateSeatDto dto, Guid adminId)
        {
            try
            {
                _loggerService.Info($"[SeatManagementService] Updating seat {seatId}");

                var seat = await _unitOfWork.Seats.GetByIdAsync(seatId);
                if (seat == null || seat.IsDeleted)
                {
                    _loggerService.Warn($"[SeatManagementService] Seat {seatId} not found.");
                    return null;
                }

                var oldData = new { seat.Row, seat.Number, seat.Type };

                seat.Row = dto.Row;
                seat.Number = dto.Number;
                seat.Type = dto.Type;
                seat.UpdatedAt = DateTime.UtcNow;
                seat.UpdatedBy = adminId;

                await _unitOfWork.Seats.Update(seat);
                await _unitOfWork.SaveChangesAsync();
                await _redisService.RemoveAsync($"seat:list:cinemaroom:{seat.CinemaRoomId}");

                await _auditLogService.LogAsync(
                    adminId,
                    AuditActionType.Update,
                    "Seat",
                    seat.Id,
                    oldData,
                    new { seat.Row, seat.Number, seat.Type },
                    JsonSerializer.Serialize(dto),
                    "Updated seat"
                );

                _loggerService.Success($"[SeatManagementService] Updated seat {seatId}");

                return new SeatDto
                {
                    Id = seat.Id,
                    Row = seat.Row,
                    Number = seat.Number,
                    Type = seat.Type,
                    CinemaRoomId = seat.CinemaRoomId
                };
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[SeatManagementService] Error updating seat: {ex.Message}");
                throw new Exception("An error occurred while updating the seat.");
            }
        }

        public async Task<bool> SoftDeleteSeatAsync(Guid seatId, Guid adminId)
        {
            try
            {
                _loggerService.Info($"[SeatManagementService] Soft deleting seat {seatId}");

                var seat = await _unitOfWork.Seats.GetByIdAsync(seatId);
                if (seat == null || seat.IsDeleted)
                {
                    _loggerService.Warn($"[SeatManagementService] Seat {seatId} not found.");
                    return false;
                }

                var oldData = new { seat.Row, seat.Number, seat.Type, seat.IsDeleted };

                seat.IsDeleted = true;
                seat.DeletedAt = DateTime.UtcNow;
                seat.DeletedBy = adminId;

                await _unitOfWork.Seats.Update(seat);
                await _unitOfWork.SaveChangesAsync();
                await _redisService.RemoveAsync($"seat:list:cinemaroom:{seat.CinemaRoomId}");

                await _auditLogService.LogAsync(
                    adminId,
                    AuditActionType.Delete,
                    "Seat",
                    seat.Id,
                    oldData,
                    new { seat.IsDeleted },
                    JsonSerializer.Serialize(new { seat.IsDeleted }),
                    "Soft deleted seat"
                );

                _loggerService.Success($"[SeatManagementService] Soft deleted seat {seatId}");

                return true;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[SeatManagementService] Error soft deleting seat: {ex.Message}");
                throw new Exception("An error occurred while deleting the seat.");
            }
        }

        //================== User & Admin Methods ===================///
        public async Task<List<ShowTimeSeatDto>> GetSeatsByShowTimeAsync(Guid showTimeId)
        {
            string cacheKey = $"seat:list:showtime:{showTimeId}";
            try
            {
                var cached = await _redisService.GetAsync<List<ShowTimeSeatDto>>(cacheKey);
                if (cached != null)
                {
                    _loggerService.Info($"[CACHE HIT] {cacheKey}");
                    return cached;
                }

                _loggerService.Info($"[CACHE MISS] {cacheKey} — Fetching from DB");

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

                await _redisService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
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

                if (seatIds == null || !seatIds.Any())
                {
                    _loggerService.Warn($"User {userId} provided empty seat list for holding");
                    return new List<SeatResponseDto>();
                }

                var uniqueSeatIds = seatIds.Distinct().ToList();
                if (uniqueSeatIds.Count != seatIds.Count)
                {
                    _loggerService.Warn($"User {userId} provided duplicate seat IDs, using unique set instead");
                    seatIds = uniqueSeatIds;
                }

                CleanupExpiredHolds();

                var now = DateTime.UtcNow;
                var expireAt = now.AddMinutes(5);

                var requestedSeatIds = seatIds.ToHashSet();

                var currentHeldSeats = _holdingSeats
                    .Where(h => h.Key.showTimeId == showTimeId &&
                           h.Value.userId == userId &&
                           h.Value.expireAt > now)
                    .Select(h => h.Key.seatId)
                    .Where(seatId => !requestedSeatIds.Contains(seatId))
                    .ToList();

                var totalSeatCount = currentHeldSeats.Count;
                _loggerService.Info($"User {userId} currently holds {totalSeatCount} other seats for showtime {showTimeId}");

                var existingBookingSeats = await _unitOfWork.BookingSeats
                    .GetQueryable()
                    .Where(bs => bs.Booking.MemberId == userId &&
                                 bs.Booking.ShowtimeId == showTimeId &&
                                 (bs.Booking.Status == "Completed" || bs.Booking.Status == "Created"))
                    .Select(bs => bs.SeatId)
                    .ToListAsync();

                totalSeatCount += existingBookingSeats.Count;
                _loggerService.Info($"User {userId} has {existingBookingSeats.Count} seats in existing bookings for showtime {showTimeId}");

                var alreadyHeldByUserSeats = _holdingSeats
                    .Where(h => h.Key.showTimeId == showTimeId &&
                           h.Value.userId == userId &&
                           h.Value.expireAt > now)
                    .Select(h => h.Key.seatId)
                    .Intersect(requestedSeatIds)
                    .ToList();

                var newSeatsToHold = requestedSeatIds.Except(alreadyHeldByUserSeats).ToList();
                totalSeatCount += newSeatsToHold.Count;

                if (totalSeatCount > 8)
                {
                    _loggerService.Warn($"User {userId} attempted to exceed 8-seat limit (total: {totalSeatCount}) for showtime {showTimeId}");
                    throw new InvalidOperationException($"You cannot hold more than 8 seats in total. You currently have {totalSeatCount - newSeatsToHold.Count} seats and are trying to hold {newSeatsToHold.Count} more.");
                }

                var showTime = await _unitOfWork.ShowTimes.GetByIdAsync(showTimeId);
                if (showTime == null)
                {
                    _loggerService.Warn($"Showtime {showTimeId} not found");
                    throw new KeyNotFoundException($"Showtime with ID {showTimeId} not found");
                }

                var allSeatsInRoom = await _unitOfWork.Seats.GetQueryable()
                    .Where(s => s.CinemaRoomId == showTime.CinemaRoomId && !s.IsDeleted)
                    .ToDictionaryAsync(s => s.Id, s => s);

                if (allSeatsInRoom.Count == 0)
                {
                    _loggerService.Warn($"No seats found for cinema room {showTime.CinemaRoomId}");
                    throw new KeyNotFoundException($"No seats found for cinema room {showTime.CinemaRoomId}");
                }

                foreach (var seatId in seatIds)
                {
                    if (!allSeatsInRoom.ContainsKey(seatId))
                    {
                        _loggerService.Warn($"Seat {seatId} does not exist in cinema room {showTime.CinemaRoomId}");
                        throw new ArgumentException($"Seat {seatId} does not exist in the specified cinema room");
                    }
                }

                var showTimeSeats = await _unitOfWork.ShowTimeSeats.GetQueryable()
                    .Where(sts => sts.ShowTimeId == showTimeId && requestedSeatIds.Contains(sts.SeatId))
                    .ToDictionaryAsync(sts => sts.SeatId, sts => sts);

                List<SeatResponseDto> heldSeats = new();

                foreach (var seatId in seatIds)
                {
                    if (_holdingSeats.TryGetValue((seatId, showTimeId), out var holdInfo) &&
                        holdInfo.userId != userId && holdInfo.expireAt > now)
                    {
                        _loggerService.Warn($"Seat {seatId} is already held by another user until {holdInfo.expireAt}");
                        throw new InvalidOperationException($"Seat {seatId} is already held by another user");
                    }

                    if (showTimeSeats.TryGetValue(seatId, out var sts) &&
                        (sts.Status == SeatStatus.Booked || sts.Status == SeatStatus.Sold))
                    {
                        _loggerService.Warn($"Seat {seatId} is already {sts.Status}");
                        throw new InvalidOperationException($"Seat {seatId} is already {sts.Status}");
                    }

                    _holdingSeats[(seatId, showTimeId)] = (userId, expireAt);

                    var seat = allSeatsInRoom[seatId];
                    heldSeats.Add(new SeatResponseDto
                    {
                        Id = seat.Id,
                        Row = seat.Row,
                        Number = seat.Number,
                        Type = seat.Type
                    });
                }

                _loggerService.Success($"User {userId} successfully held {heldSeats.Count} seats for showtime {showTimeId}: {string.Join(", ", heldSeats.Select(s => $"{s.Row}{s.Number}"))}");

                if (heldSeats.Any())
                {
                    await BroadcastSeatUpdateAsync(showTimeId, heldSeats);

                    await _redisService.RemoveAsync($"seat:list:showtime:{showTimeId}");
                }

                return heldSeats;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error holding seats for user {userId} for showtime {showTimeId}: {ex.Message}");
                throw new InvalidOperationException("An error occurred while holding seats. Please try again later.", ex);
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

        //=================== Helper Methods ===================///
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

        private async Task BroadcastSeatUpdateAsync(Guid showTimeId, List<SeatResponseDto> heldSeats)
        {
            await _seatHub.Clients
                .Group($"ShowTime_{showTimeId}")
                .SendAsync("ReceiveSeatUpdate", new
                {
                    ShowTimeId = showTimeId,
                    Seats = heldSeats.Select(s => new
                    {
                        SeatId = s.Id,
                        Status = SeatStatus.Holding
                    })
                });
        }
    }
}