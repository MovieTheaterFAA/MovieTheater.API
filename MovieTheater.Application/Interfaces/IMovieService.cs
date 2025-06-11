using MovieTheater.Domain.DTOs.MovieDTOs;
using MovieTheater.Infrastructure.Commons;

namespace MovieTheater.Application.Interfaces
{
    public interface IMovieService
    {
        Task<MovieUpdateDto> UpdateMovieInfoAsync(Guid movieId, MovieUpdateDto movieUpdateDto);
        Task<MovieResponseDto> AddMovieAsync(MovieRequestDTO movieRequestDto);
        Task<Pagination<MovieResponseDto>> GetAllMoviesAsync(
            string? search,
            string? sortBy,
            bool isDescending,
            int page,
            int pageSize
            );
        Task<MovieResponseDto> GetMovieDetailAsync(Guid movieId);
        Task<List<MovieResponseDto>> GetMovieByNameAsync(string? Name);
    }
}
