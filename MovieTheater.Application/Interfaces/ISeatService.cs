using MovieTheater.Domain.DTOs.SeatDTOs;

namespace MovieTheater.Application.Interfaces
{
    public interface ISeatService
    {
        Task<List<SeatResponseDto>> HoldSeatsAsync(Guid userId, Guid showTimeId, List<Guid> seatIds);
        Task<List<ShowTimeSeatDto>> GetSeatsByShowTimeAsync(Guid showTimeId);
        Task<ShowTimeSeatDto> GetSeatByIdAsync(Guid seatId);
    }
}
