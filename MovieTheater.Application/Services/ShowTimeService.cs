using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.ShowTimeDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using static MovieTheater.Domain.DTOs.ShowTimeDTOs.BatchShowtimeRequestDto;

namespace MovieTheater.Application.Services
{
    public class ShowTimeService : IShowTimeService
    {
        private readonly ILoggerService _loggerService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimsService _claimsService;
        private readonly IRedisService _redisService;

        public ShowTimeService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService, IRedisService redisService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
            _claimsService = claimsService;
            _redisService = redisService;
        }

        //============================ Admin =============================
        public async Task<List<ShowtimeResponseDTO>> AddBatchShowTimesAsync(BatchShowTimeRequestDto dto)
        {
            _loggerService.Info($"[AddBatchShowTimesAsync] Start adding showtimes for room {dto.CinemaRoomId}");

            // Business rule: Only allow adding showtimes for movies in the next week (not in the current week)
            var today = DateTime.UtcNow.Date;
            var startOfThisWeek = today.AddDays(-(int)today.DayOfWeek);
            var startOfNextWeek = startOfThisWeek.AddDays(7);

            // All showtimes must be in next week (>= startOfNextWeek and < startOfNextWeek + 7)
            foreach (var entry in dto.ShowTimes)
            {
                if (entry.StartTime.Date < startOfNextWeek || entry.StartTime.Date >= startOfNextWeek.AddDays(7))
                {
                    throw new InvalidOperationException("Showtimes can only be added for the next week (not in the current week).");
                }
            }

            var room = await _unitOfWork.CinemaRooms.GetByIdAsync(dto.CinemaRoomId);
            if (room == null)
                throw new InvalidOperationException("Cinema room not found.");

            var movieIds = dto.ShowTimes.Select(s => s.MovieId).Distinct().ToList();
            var movies = await _unitOfWork.Movies.GetQueryable()
                            .Where(m => movieIds.Contains(m.Id))
                            .ToDictionaryAsync(m => m.Id);

            // ===== Overlap validation for batch =====
            // Prepare a list of (start, end, index) for each showtime in the batch
            var showtimeWindows = new List<(DateTime Start, DateTime End, int Index)>();
            for (int idx = 0; idx < dto.ShowTimes.Count; idx++)
            {
                var entry = dto.ShowTimes[idx];
                if (!movies.TryGetValue(entry.MovieId, out var movie))
                    throw new InvalidOperationException($"Movie {entry.MovieId} not found.");

                var runningTime = movie.RunningTime ?? 0;
                var duration = TimeSpan.FromMinutes(runningTime + 15); // movie + rest
                var start = entry.StartTime;
                var end = start.Add(duration);

                showtimeWindows.Add((start, end, idx));
            }

            // Check for overlap with existing showtimes in the same room and date (any movie)
            foreach (var entry in dto.ShowTimes)
            {
                var existingShowTimes = await GetShowTimesByRoomAndDateAsync(dto.CinemaRoomId, entry.StartTime.Date);

                var movie = movies[entry.MovieId];
                var newStart = entry.StartTime;
                var newEnd = newStart.Add(TimeSpan.FromMinutes((movie.RunningTime ?? 0) + 15));

                foreach (var existing in existingShowTimes)
                {
                    var existingStart = existing.ShowDate;
                    var existingEnd = existing.ShowDate.Add(existing.Duration);

                    if (newStart < existingEnd && existingStart < newEnd)
                    {
                        _loggerService.Error(
                            $"[AddBatchShowTimesAsync] Overlap detected with existing showtime: " +
                            $"New (MovieId: {entry.MovieId}, Start: {newStart:O}, End: {newEnd:O}) " +
                            $"Existing (Id: {existing.Id}, MovieId: {existing.MovieId}, Start: {existingStart:O}, End: {existingEnd:O})"
                        );
                        throw new InvalidOperationException("One or more showtimes overlap with existing showtimes in this room. Please check the start times and durations.");
                    }
                }
            }
            // === End overlap validation ===

            var showTimes = new List<ShowTime>();

            foreach (var entry in dto.ShowTimes)
            {
                if (!movies.TryGetValue(entry.MovieId, out var movie))
                    throw new InvalidOperationException($"Movie {entry.MovieId} not found.");

                var runningTime = movie.RunningTime ?? 0;
                var duration = TimeSpan.FromMinutes(runningTime + 15);  // Cộng thêm 15 phút

                showTimes.Add(new ShowTime
                {
                    MovieId = entry.MovieId,
                    CinemaRoomId = dto.CinemaRoomId,
                    ShowDate = DateTime.SpecifyKind(entry.StartTime, DateTimeKind.Utc),
                    Duration = duration,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _claimsService.GetCurrentUserId
                });
            }

            await _unitOfWork.ShowTimes.AddRangeAsync(showTimes);
            await _unitOfWork.SaveChangesAsync();

            // Remove all related showtime caches for this room and all affected movies/dates
            var affectedDates = dto.ShowTimes.Select(st => st.StartTime.Date).Distinct().ToList();
            var affectedMovieIds = dto.ShowTimes.Select(st => st.MovieId).Distinct().ToList();

            // Remove cache for GetShowTimesByDateAsync
            foreach (var date in affectedDates)
            {
                string cacheKey = $"showtime:date:{date:yyyyMMdd}:movie:all:room:{dto.CinemaRoomId}";
                await _redisService.RemoveAsync(cacheKey);

                foreach (var movieId in affectedMovieIds)
                {
                    string cacheKeyMovie = $"showtime:date:{date:yyyyMMdd}:movie:{movieId}:room:{dto.CinemaRoomId}";
                    await _redisService.RemoveAsync(cacheKeyMovie);
                }
            }

            // Remove cache for GetShowTimesByMovieAndDateAsync
            foreach (var movieId in affectedMovieIds)
            {
                foreach (var date in affectedDates)
                {
                    string cacheKey = $"showtime:movie:{movieId}:date:{date:yyyyMMdd}";
                    await _redisService.RemoveAsync(cacheKey);
                }
                // Remove the "all" date cache for this movie
                string cacheKeyAll = $"showtime:movie:{movieId}:all";
                await _redisService.RemoveAsync(cacheKeyAll);
            }

            // Remove the original cache for AddBatchShowTimesAsync (if any)
            await _redisService.RemoveAsync($"showtime:room:{dto.CinemaRoomId}:date:{dto.ShowTimes.First().StartTime:yyyyMMdd}");

            // Audit log: log only primitive properties
            var newShowTimeData = showTimes.Select(st => new
            {
                st.Id,
                st.MovieId,
                st.CinemaRoomId,
                st.ShowDate,
                st.Duration
            }).ToList();

            await _unitOfWork.AuditLogs.AddAsync(new AuditLog
            {
                AdminId = _claimsService.GetCurrentUserId,
                ActionType = AuditActionType.Create.ToString(),
                EntityType = "ShowTime",
                EntityId = dto.CinemaRoomId,
                OldValue = null,
                NewValue = System.Text.Json.JsonSerializer.Serialize(newShowTimeData),
                ChangedFields = System.Text.Json.JsonSerializer.Serialize(dto),
                Timestamp = DateTime.UtcNow,
                Reason = "Batch created showtimes"
            });
            await _unitOfWork.SaveChangesAsync();

            return showTimes.Select(st => new ShowtimeResponseDTO
            {
                Id = st.Id,
                MovieId = st.MovieId,
                CinemaRoomId = st.CinemaRoomId,
                ShowDate = st.ShowDate,
                Duration = st.Duration
            }).ToList();
        }

        public async Task<int> DeleteShowTimesByDateAsync(DateTime date)
        {
            _loggerService.Info($"[DeleteShowTimesByDateAsync] Deleting showtimes for date: {date:yyyy-MM-dd}");

            var showTimes = await _unitOfWork.ShowTimes.GetAllAsync(st => st.ShowDate.Date == date.Date && !st.IsDeleted);
            if (showTimes == null || !showTimes.Any())
            {
                _loggerService.Warn($"[DeleteShowTimesByDateAsync] No showtimes found for date: {date:yyyy-MM-dd}");
                return 0;
            }

            int deletedCount = 0;
            foreach (var showTime in showTimes)
            {
                // Soft delete (if supported)
                var result = await _unitOfWork.ShowTimes.SoftRemove(showTime);
                if (result) deletedCount++;
            }
            await _unitOfWork.SaveChangesAsync();

            // Invalidate related cache
            await _redisService.RemoveByPatternAsync("showtime:date:*");
            await _redisService.RemoveByPatternAsync("showtime:movie:*");

            // Audit log
            await _unitOfWork.AuditLogs.AddAsync(new AuditLog
            {
                AdminId = _claimsService.GetCurrentUserId,
                ActionType = AuditActionType.Delete.ToString(),
                EntityType = "ShowTime",
                EntityId = Guid.Empty,
                OldValue = System.Text.Json.JsonSerializer.Serialize(showTimes),
                NewValue = "",
                ChangedFields = $"ShowDate: {date:yyyy-MM-dd}",
                Timestamp = DateTime.UtcNow,
                Reason = $"Deleted all showtimes for date {date:yyyy-MM-dd}"
            });
            await _unitOfWork.SaveChangesAsync();

            _loggerService.Success($"[DeleteShowTimesByDateAsync] Deleted {deletedCount} showtimes for date: {date:yyyy-MM-dd}");
            return deletedCount;
        }

        public async Task<ShowtimeResponseDTO> UpdateShowTimeAsync(Guid showTimeId, UpdateShowtimeDto dto)
        {
            _loggerService.Info($"[UpdateShowTimeAsync] Updating showtime {showTimeId}");

            var showTime = await _unitOfWork.ShowTimes.GetByIdAsync(showTimeId);
            if (showTime == null || showTime.IsDeleted)
                throw new KeyNotFoundException("Showtime not found.");

            // Get the movie to determine running time
            var movie = await _unitOfWork.Movies.GetByIdAsync(dto.MovieId);
            if (movie == null)
                throw new KeyNotFoundException("Movie not found.");

            var newStart = dto.ShowDate;
            var newDuration = dto.Duration != default ? dto.Duration : TimeSpan.FromMinutes((movie.RunningTime ?? 0) + 15);
            var newEnd = newStart.Add(newDuration);

            // Overlap check: exclude this showtime itself
            var overlapping = await _unitOfWork.ShowTimes.GetQueryable()
                .Where(st => st.CinemaRoomId == dto.CinemaRoomId
                    && st.Id != showTimeId
                    && !st.IsDeleted
                    && (
                        (newStart < st.ShowDate.Add(st.Duration) && st.ShowDate < newEnd)
                    )
                )
                .AnyAsync();

            if (overlapping)
                throw new InvalidOperationException("The new showtime overlaps with another showtime in this room.");

            // Update fields
            showTime.MovieId = dto.MovieId;
            showTime.CinemaRoomId = dto.CinemaRoomId;
            showTime.ShowDate = dto.ShowDate;
            showTime.Duration = newDuration;
            showTime.UpdatedAt = DateTime.UtcNow;
            showTime.UpdatedBy = _claimsService.GetCurrentUserId;

            await _unitOfWork.ShowTimes.Update(showTime);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.Success($"[UpdateShowTimeAsync] Updated showtime {showTimeId}");

            return new ShowtimeResponseDTO
            {
                Id = showTime.Id,
                MovieId = showTime.MovieId,
                CinemaRoomId = showTime.CinemaRoomId,
                ShowDate = showTime.ShowDate,
                Duration = showTime.Duration
            };
        }

        public async Task<bool> SoftDeleteShowTimeAsync(Guid showTimeId)
        {
            _loggerService.Info($"[SoftDeleteShowTimeAsync] Soft deleting showtime {showTimeId}");

            var showTime = await _unitOfWork.ShowTimes.GetByIdAsync(showTimeId);
            if (showTime == null || showTime.IsDeleted)
                return false;

            var result = await _unitOfWork.ShowTimes.SoftRemove(showTime);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.Success($"[SoftDeleteShowTimeAsync] Soft deleted showtime {showTimeId}");
            return result;
        }

        public async Task<List<ShowtimeResponseDTO>> GetShowTimesByDateAsync(DateTime? date, Guid? movieId, Guid? roomId)
        {
            try
            {
                // Build cache key based on provided filters
                string cacheKey = $"showtime:date:{(date.HasValue ? date.Value.ToString("yyyyMMdd") : "all")}:movie:{movieId?.ToString() ?? "all"}:room:{roomId?.ToString() ?? "all"}";
                var cached = await _redisService.GetAsync<List<ShowtimeResponseDTO>>(cacheKey);
                if (cached != null)
                {
                    _loggerService.Info($"[CACHE HIT] {cacheKey}");
                    return cached;
                }

                _loggerService.Info($"[CACHE MISS] {cacheKey} — Fetching from DB");

                var query = _unitOfWork.ShowTimes.GetQueryable().Where(st => !st.IsDeleted);

                if (date.HasValue && date.Value != DateTime.MinValue)
                {
                    query = query.Where(st => st.ShowDate.Date == date.Value.Date);
                    _loggerService.Info($"[GetShowTimesByDateAsync] Filtering by date: {date.Value:yyyy-MM-dd}");
                }
                if (movieId.HasValue)
                {
                    query = query.Where(st => st.MovieId == movieId.Value);
                    _loggerService.Info($"[GetShowTimesByDateAsync] Filtering by MovieId: {movieId.Value}");
                }
                if (roomId.HasValue)
                {
                    query = query.Where(st => st.CinemaRoomId == roomId.Value);
                    _loggerService.Info($"[GetShowTimesByDateAsync] Filtering by CinemaRoomId: {roomId.Value}");
                }

                var showTimes = await query.OrderBy(st => st.ShowDate).ToListAsync();

                if (showTimes == null || !showTimes.Any())
                {
                    _loggerService.Warn($"[GetShowTimesByDateAsync] No showtimes found for the given filters.");
                    return new List<ShowtimeResponseDTO>();
                }

                var result = showTimes.Select(st => new ShowtimeResponseDTO
                {
                    Id = st.Id,
                    MovieId = st.MovieId,
                    CinemaRoomId = st.CinemaRoomId,
                    ShowDate = st.ShowDate,
                    Duration = st.Duration
                }).ToList();

                await _redisService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                _loggerService.Success($"[GetShowTimesByDateAsync] Found {result.Count} showtimes.");
                return result;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[GetShowTimesByDateAsync] Error: {ex.Message}");
                throw new InvalidOperationException("An error occurred while retrieving showtimes.", ex);
            }
        }

        //============================ User =============================
        public async Task<List<ShowtimeResponseDTO>> GetShowTimesByMovieAndDateAsync(Guid movieId, DateTime? date = null)
        {
            try
            {
                string cacheKey = date.HasValue
                    ? $"showtime:movie:{movieId}:date:{date.Value:yyyyMMdd}"
                    : $"showtime:movie:{movieId}:all";
                var cached = await _redisService.GetAsync<List<ShowtimeResponseDTO>>(cacheKey);
                if (cached != null)
                {
                    _loggerService.Info($"[CACHE HIT] {cacheKey}");
                    return cached;
                }

                _loggerService.Info($"[CACHE MISS] {cacheKey} — Fetching from DB");
                _loggerService.Info($"[GetShowTimesByMovieAndDateAsync] movieId: {movieId}, date: {(date.HasValue ? date.Value.ToString("yyyy-MM-dd") : "all")}");

                var query = _unitOfWork.ShowTimes.GetQueryable()
                    .Where(st => st.MovieId == movieId && !st.IsDeleted);

                if (date.HasValue)
                    query = query.Where(st => st.ShowDate.Date == date.Value.Date);

                var showTimes = await query.OrderBy(st => st.ShowDate).ToListAsync();

                if (showTimes == null || !showTimes.Any())
                {
                    _loggerService.Warn($"[GetShowTimesByMovieAndDateAsync] No showtimes found for MovieId {movieId}.");
                    return new List<ShowtimeResponseDTO>();
                }

                var result = showTimes.Select(st => new ShowtimeResponseDTO
                {
                    Id = st.Id,
                    MovieId = st.MovieId,
                    CinemaRoomId = st.CinemaRoomId,
                    ShowDate = st.ShowDate,
                    Duration = st.Duration
                }).ToList();

                await _redisService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                _loggerService.Success($"[GetShowTimesByMovieAndDateAsync] Found {result.Count} showtimes.");
                return result;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[GetShowTimesByMovieAndDateAsync] Error: {ex.Message}");
                throw new InvalidOperationException("An error occurred while retrieving showtimes.", ex);
            }
        }

        //============================ Helper =============================
        private async Task<List<ShowtimeResponseDTO>> GetShowTimesByRoomAndDateAsync(Guid roomId, DateTime date)
        {
            var showTimes = await _unitOfWork.ShowTimes.GetQueryable()
                .Where(st => st.CinemaRoomId == roomId && !st.IsDeleted && st.ShowDate.Date == date.Date)
                .OrderBy(st => st.ShowDate)
                .ToListAsync();

            return showTimes.Select(st => new ShowtimeResponseDTO
            {
                Id = st.Id,
                MovieId = st.MovieId,
                CinemaRoomId = st.CinemaRoomId,
                ShowDate = st.ShowDate,
                Duration = st.Duration
            }).ToList();
        }
    }
}