using MovieTheater.Domain.DTOs.ShowTimeDTOs;
using static MovieTheater.Domain.DTOs.ShowTimeDTOs.BatchShowtimeRequestDto;

namespace MovieTheater.Application.Interfaces
{
    public interface IShowTimeService
    {
        Task<List<ShowtimeResponseDTO>> AddBatchShowTimesAsync(BatchShowTimeRequestDto dto);
        Task<ShowtimeResponseDTO> UpdateShowTimeAsync(Guid showTimeId, UpdateShowtimeDto dto);
        Task<bool> SoftDeleteShowTimeAsync(Guid showTimeId);
        Task<List<ShowtimeResponseDTO>> GetShowTimesByMovieAndDateAsync(Guid movieId, DateTime? date = null);

        Task<List<ShowtimeResponseDTO>> GetShowTimesByDateAsync(DateTime? date, Guid? movieId, Guid? roomId);

        Task<int> DeleteShowTimesByDateAsync(DateTime date);
    }
}