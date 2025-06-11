using MovieTheater.Domain.DTOs.EventDTOs;

namespace MovieTheater.Application.Interfaces
{
    public interface IEventService
    {
        Task<EventResponseDto?> AddEventAsync(EventRequestDto dto);

    }
}
