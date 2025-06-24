using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.ShowTimeDTOs;
using MovieTheater.Domain.Entities;
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

            var room = await _unitOfWork.CinemaRooms.GetByIdAsync(dto.CinemaRoomId);
            if (room == null)
                throw new InvalidOperationException("Cinema room not found.");

            var movieIds = dto.ShowTimes.Select(s => s.MovieId).Distinct().ToList();
            var movies = await _unitOfWork.Movies.GetQueryable()
                            .Where(m => movieIds.Contains(m.Id))
                            .ToDictionaryAsync(m => m.Id);

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
                    ShowDate = entry.StartTime,
                    Duration = duration,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _claimsService.GetCurrentUserId
                });
            }

            await _unitOfWork.ShowTimes.AddRangeAsync(showTimes);
            await _unitOfWork.SaveChangesAsync();
            await _redisService.RemoveAsync($"showtime:room:{dto.CinemaRoomId}:date:{dto.ShowTimes.First().StartTime:yyyyMMdd}");

            return showTimes.Select(st => new ShowtimeResponseDTO
            {
                Id = st.Id,
                MovieId = st.MovieId,
                CinemaRoomId = st.CinemaRoomId,
                ShowDate = st.ShowDate,
                Duration = st.Duration
            }).ToList();
        }

        public async Task<ShowtimeResponseDTO> AddSingleShowTimeAsync(ShowTimeRequestDto showTimeRequestDto)
        {
            _loggerService.Info($"[AddShowTimeAsync] Start adding showtime for MovieId {showTimeRequestDto.MovieId} and CinemaRoomId {showTimeRequestDto.CinemaRoomId}");

            // Kiểm tra MovieId có tồn tại không
            var movie = await _unitOfWork.Movies.GetByIdAsync(showTimeRequestDto.MovieId);
            if (movie == null)
            {
                _loggerService.Warn($"[AddShowTimeAsync] MovieId {showTimeRequestDto.MovieId} not found.");
                throw new InvalidOperationException("Movie not found.");
            }

            // Kiểm tra CinemaRoomId có tồn tại không
            var cinemaRoom = await _unitOfWork.CinemaRooms.GetByIdAsync(showTimeRequestDto.CinemaRoomId);
            if (cinemaRoom == null)
            {
                _loggerService.Warn($"[AddShowTimeAsync] CinemaRoomId {showTimeRequestDto.CinemaRoomId} not found.");
                throw new InvalidOperationException("Cinema Room not found.");
            }

            // Tính toán Duration = RunningTime của Movie + 15 phút
            var movieRunningTime = movie.RunningTime ?? 0; // Lấy RunningTime của Movie, mặc định là 0 nếu không có
            var duration = TimeSpan.FromMinutes(movieRunningTime + 15); // Cộng thêm 15 phút

            // Tạo mới ShowTime
            var showTime = new ShowTime
            {
                MovieId = showTimeRequestDto.MovieId,
                CinemaRoomId = showTimeRequestDto.CinemaRoomId,
                ShowDate = showTimeRequestDto.ShowDate,
                Duration = duration, // Gán Duration đã tính
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _claimsService.GetCurrentUserId // Gán CreatedBy từ ClaimsService
            };

            // Thêm ShowTime vào database
            await _unitOfWork.ShowTimes.AddAsync(showTime);
            try
            {
                await _unitOfWork.SaveChangesAsync();
                await _redisService.RemoveAsync($"showtime:movie:{showTime.MovieId}:date:{showTime.ShowDate:yyyyMMdd}");
            }
            catch (DbUpdateException dbEx)
            {
                _loggerService.Error($"DbUpdateException: {dbEx.InnerException?.Message ?? dbEx.Message}");
                throw;
            }

            _loggerService.Success($"[AddShowTimeAsync] Showtime for MovieId {showTime.MovieId} added successfully.");

            // Trả về ShowtimeResponseDTO
            var responseDto = new ShowtimeResponseDTO
            {
                Id = showTime.Id,
                MovieId = showTime.MovieId,
                CinemaRoomId = showTime.CinemaRoomId,
                ShowDate = showTime.ShowDate,
                Duration = showTime.Duration
            };

            return responseDto;
        }

        public async Task<List<ShowtimeResponseDTO>> GetShowTimesByDateAsync(DateTime date, Guid? movieId, Guid? roomId)
        {
            try
            {
                string cacheKey = $"showtime:date:{date:yyyyMMdd}:movie:{movieId?.ToString() ?? "null"}:room:{roomId?.ToString() ?? "null"}";
                var cached = await _redisService.GetAsync<List<ShowtimeResponseDTO>>(cacheKey);
                if (cached != null)
                {
                    _loggerService.Info($"[CACHE HIT] {cacheKey}");
                    return cached;
                }

                _loggerService.Info($"[CACHE MISS] {cacheKey} — Fetching from DB");
                _loggerService.Info($"[GetShowTimesByDateAsync] date: {date:yyyy-MM-dd}");

                var showTimes = await _unitOfWork.ShowTimes.GetQueryable()
                    .Where(st => st.ShowDate.Date == date.Date && !st.IsDeleted)
                    .ToListAsync();

                if (movieId.HasValue)
                {
                    showTimes = showTimes.Where(st => st.MovieId == movieId.Value).ToList();
                    _loggerService.Info($"[GetShowTimesByDateAsync] Filtering by MovieId: {movieId.Value}");
                }
                if (roomId.HasValue)
                {
                    showTimes = showTimes.Where(st => st.CinemaRoomId == roomId.Value).ToList();
                    _loggerService.Info($"[GetShowTimesByDateAsync] Filtering by CinemaRoomId: {roomId.Value}");
                }

                showTimes = showTimes.OrderBy(st => st.ShowDate).ToList();

                if (showTimes == null || !showTimes.Any())
                {
                    _loggerService.Warn($"[GetShowTimesByDateAsync] No showtimes found on date {date:yyyy-MM-dd}.");
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
        public async Task<List<ShowtimeResponseDTO>> GetShowTimesByMovieAndDateAsync(Guid movieId, DateTime date)
        {
            try
            {
                string cacheKey = $"showtime:movie:{movieId}:date:{date:yyyyMMdd}";
                var cached = await _redisService.GetAsync<List<ShowtimeResponseDTO>>(cacheKey);
                if (cached != null)
                {
                    _loggerService.Info($"[CACHE HIT] {cacheKey}");
                    return cached;
                }

                _loggerService.Info($"[CACHE MISS] {cacheKey} — Fetching from DB");
                _loggerService.Info($"[GetShowTimesByMovieAndDateAsync] movieId: {movieId}, date: {date:yyyy-MM-dd}");

                var showTimes = await _unitOfWork.ShowTimes.GetQueryable()
                    .Where(st => st.MovieId == movieId && st.ShowDate.Date == date.Date && !st.IsDeleted)
                    .ToListAsync();

                if (showTimes == null || !showTimes.Any())
                {
                    _loggerService.Warn($"[GetShowTimesByMovieAndDateAsync] No showtimes found for MovieId {movieId} on date {date:yyyy-MM-dd}.");
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
    }
}