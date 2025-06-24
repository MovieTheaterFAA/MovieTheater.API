using MovieTheater.Domain.DTOs.CinemaRoomDTOs;

namespace MovieTheater.Application.Interfaces
{
    public interface ICinemaRoomService
    {
        Task<Pagination<CinemaRoomDto>> GetAllCinemaRoomAsync(string? search, string? sortBy, bool isDescending, int page, int pageSize);
        Task<CinemaRoomDto?> GetCinemaRoomByIdAsync(Guid id);
        Task<CinemaRoomDto> CreateCinemaRoomAsync(CreateCinemaRoomDto dto, Guid adminId);
        Task<CinemaRoomDto?> UpdateCinemaRoomAsync(Guid id, UpdateCinemaRoomDto dto, Guid adminId);
        Task<bool> SoftDeleteCinemaRoomAsync(Guid id, Guid adminId);
    }
}
