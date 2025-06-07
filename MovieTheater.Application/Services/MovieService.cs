using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.MovieDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Application.Services
{
    public class MovieService : IMovieService
    {

        private readonly ILoggerService _loggerService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimsService _claimsService;

        public MovieService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
            _claimsService = claimsService;
        }
        public async Task<MovieResponseDto> AddMovieAsync(MovieRequestDTO movieRequestDto)
        {
            _loggerService.Info($"[AddMovieAsync] Start adding movie {movieRequestDto.Name}");

            // Kiểm tra xem ngày `ToDate` có nhỏ hơn `FromDate` không
            if (movieRequestDto.FromDate > movieRequestDto.ToDate)
            {
                _loggerService.Warn($"[AddMovieAsync] ToDate cannot be earlier than FromDate for movie {movieRequestDto.Name}");
                throw new InvalidOperationException("ToDate cannot be earlier than FromDate.");
            }

            // Tạo mới đối tượng Movie từ MovieRequestDTO
            var movie = new Movie
            {
                Name = movieRequestDto.Name,
                FromDate = movieRequestDto.FromDate,
                ToDate = movieRequestDto.ToDate,
                Actors = movieRequestDto.Actors,
                ActorsUrl = movieRequestDto.ActorsUrl,
                Director = movieRequestDto.Director,
                RunningTime = movieRequestDto.RunningTime,
                Version = movieRequestDto.Version,
                TrailerUrl = movieRequestDto.TrailerUrl,
                Genres = movieRequestDto.Genres,
                Description = movieRequestDto.Description,
                PosterImage = movieRequestDto.PosterImage,
                BackgroundImage = movieRequestDto.BackgroundImage,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _claimsService.GetCurrentUserId // Gán giá trị CreatedBy từ service của Claims
            };

            // Thêm bộ phim vào cơ sở dữ liệu
            await _unitOfWork.Movies.AddAsync(movie);
            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                _loggerService.Error($"DbUpdateException: {dbEx.InnerException?.Message ?? dbEx.Message}");
                throw new Exception("An error occurred while saving the movie.");
            }

            _loggerService.Success($"[AddMovieAsync] Movie {movie.Name} added successfully.");
            // Trả về MovieResponseDto
            var responseDto = new MovieResponseDto
            {
                Id = movie.Id,
                Name = movie.Name,
                FromDate = movie.FromDate,
                ToDate = movie.ToDate,
                Actors = movie.Actors,
                Director = movie.Director,
                RunningTime = movie.RunningTime,
                Version = movie.Version,
                TrailerUrl = movie.TrailerUrl,
                Genres = movie.Genres,
                Description = movie.Description,
                PosterImage = movie.PosterImage,
                BackgroundImage = movie.BackgroundImage,
            };

            return responseDto;
        }
    }
}
