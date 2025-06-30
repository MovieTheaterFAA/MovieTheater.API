using MovieTheater.Domain.DTOs.EventDTOs;

namespace MovieTheater.Application.Interfaces
{
    public interface IEventService
    {
        Task<EventResponseDto?> AddEventAsync(EventRequestDto dto);
        Task<EventResponseDto?> UpdateEventAsync(Guid eventId, EventUpdateDto dto);
        Task<Pagination<EventResponseDto>> GetAllEventsAsync(string? search, string? sortBy, bool isDescending, int page, int pageSize);
        Task<bool> DeleteEventByIdAsync(Guid eventId);
    }
}
