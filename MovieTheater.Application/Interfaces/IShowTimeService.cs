using MovieTheater.Domain.DTOs.ShowTimeDTOs;

namespace MovieTheater.Application.Interfaces
{
    public interface IShowTimeService
    {
        Task<ShowtimeResponseDTO> AddShowTimeAsync(ShowTimeRequestDto showTimeRequestDto);
    }
}
