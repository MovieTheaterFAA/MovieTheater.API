using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.ShowTimeDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Application.Services
{
    public class ShowTimeService : IShowTimeService
    {
        private readonly ILoggerService _loggerService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimsService _claimsService;

        public ShowTimeService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
            _claimsService = claimsService;
        }

        public async Task<ShowtimeResponseDTO> AddShowTimeAsync(ShowTimeRequestDto showTimeRequestDto)
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

        public async Task<List<ShowtimeResponseDTO>> GetShowTimesByMovieAndDateAsync(Guid movieId, DateTime date)
        {
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

            _loggerService.Success($"[GetShowTimesByMovieAndDateAsync] Found {result.Count} showtimes.");
            return result;
        }

        public async Task<List<ShowtimeResponseDTO>> GetShowTimesByDateAsync(DateTime date)
        {
            _loggerService.Info($"[GetShowTimesByDateAsync] date: {date:yyyy-MM-dd}");

            var showTimes = await _unitOfWork.ShowTimes.GetQueryable()
                .Where(st => st.ShowDate.Date == date.Date && !st.IsDeleted)
                .ToListAsync();

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

            _loggerService.Success($"[GetShowTimesByDateAsync] Found {result.Count} showtimes.");
            return result;
        }
    }
}
