using MovieTheater.Domain.DTOs.MovieDTOs;

namespace MovieTheater.Application.Interfaces
{
    public interface IMovieService
    {
        Task<MovieUpdateDto> UpdateMovieInfoAsync(Guid movieId, MovieUpdateDto movieUpdateDto);
    }
}
