using MovieTheater.Domain.DTOs.SeatDTOs;

namespace MovieTheater.Application.Interfaces
{
    public interface ISeatService
    {
        Task<bool> HoldSeatsAsync(Guid userId, Guid showTimeId, List<Guid> seatIds);
        Task<List<ShowTimeSeatDto>> GetSeatsByShowTimeAsync(Guid showTimeId);
    }
}
