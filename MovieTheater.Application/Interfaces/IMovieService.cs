using MovieTheater.Domain.DTOs.MovieDTOs;

namespace MovieTheater.Application.Interfaces
{
    public interface IMovieService
    {
        Task<MovieResponseDto> AddMovieAsync(MovieRequestDTO movieRequestDto);
    }
}
