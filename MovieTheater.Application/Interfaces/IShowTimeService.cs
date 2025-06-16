using MovieTheater.Domain.DTOs.ShowTimeDTOs;

namespace MovieTheater.Application.Interfaces
{
    public interface IShowTimeService
    {
        Task<ShowtimeResponseDTO> AddShowTimeAsync(ShowTimeRequestDto showTimeRequestDto);
        Task<List<ShowtimeResponseDTO>> GetShowTimesByMovieAndDateAsync(Guid movieId, DateTime date);
        Task<List<ShowtimeResponseDTO>> GetShowTimesByDateAsync(DateTime date);
    }
}
