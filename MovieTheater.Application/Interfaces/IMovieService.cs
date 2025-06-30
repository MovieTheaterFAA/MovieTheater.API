using MovieTheater.Domain.DTOs.MovieDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;

namespace MovieTheater.Application.Interfaces
{
    public interface IMovieService
    {
        Task<MovieUpdateDto> UpdateMovieInfoAsync(Guid movieId, MovieUpdateDto movieUpdateDto);

        Task<MovieResponseDto> AddMovieAsync(MovieRequestDTO movieRequestDto);
        Task<MovieResponseDto> AddMovieWithImagesAndCastsAsync(MovieWithImagesAndCastsRequestDto dto);

        Task<Pagination<MovieResponseDto>> GetAllMoviesAsync(
            string? search,
            string? sortBy,
            bool isDescending,
            int page,
            int pageSize,
            List<string>? genres = null,
            MovieStatus? status = null
            );

        Task<MovieResponseDto> GetMovieDetailAsync(Guid movieId);
        Task<List<MovieResponseDto>> GetMovieByNameAsync(string? Name);
        Task<bool> DeleteMovieAsync(Guid movieId);
    }
}