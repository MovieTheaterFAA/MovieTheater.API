using MovieTheater.Domain.DTOs.BookingDTOs;

namespace MovieTheater.Application.Interfaces;

public interface IBookingService
{
    Task<BookingDto> GetBookingByIdAsync(Guid id);
    Task<IEnumerable<BookingDto>> GetUserBookingsAsync(Guid userId);
    Task<BookingDto> CreateBookingAsync(Guid userId, CreateBookingRequest request);
    Task<bool> CancelBookingAsync(Guid bookingId);
}
