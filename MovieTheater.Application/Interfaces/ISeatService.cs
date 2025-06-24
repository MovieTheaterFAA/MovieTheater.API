using MovieTheater.Domain.DTOs.SeatDTOs;

namespace MovieTheater.Application.Interfaces
{
    public interface ISeatService
    {
        // Crud methods for admin
        Task<List<SeatDto>> GetSeatsByCinemaRoomAsync(Guid cinemaRoomId);
        Task<List<SeatDto>> BatchCreateSeatsAsync(Guid cinemaRoomId, BatchCreateSeatDto dto, Guid adminId);
        Task<SeatDto?> UpdateSeatAsync(Guid seatId, UpdateSeatDto dto, Guid adminId);
        Task<bool> SoftDeleteSeatAsync(Guid seatId, Guid adminId);

        // Methods for users
        Task<List<SeatResponseDto>> HoldSeatsAsync(Guid userId, Guid showTimeId, List<Guid> seatIds);
        Task<List<ShowTimeSeatDto>> GetSeatsByShowTimeAsync(Guid showTimeId);
        Task<ShowTimeSeatDto> GetSeatByIdAsync(Guid seatId);
    }
}
