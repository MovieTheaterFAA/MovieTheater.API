using MovieTheater.Domain.DTOs.BookingDTOs;
using MovieTheater.Domain.Enums;

namespace MovieTheater.Application.Interfaces;

public interface IBookingService
{
    Task<BookingResponseDto> GetBookingByIdAsync(Guid id);
    Task<IEnumerable<BookingResponseDto>> GetUserBookingsAsync(Guid userId);
    Task<BookingDto> CreateBookingAsync(Guid userId, CreateBookingRequest request);
    Task<bool> CancelBookingAsync(Guid bookingId);
    Task<Pagination<BookingResponseDto>> GetAllBookingsAsync(int page = 1, int pageSize = 10, BookingStatus? status = null,
    string? sortBy = null, bool isDescending = false, string? search = null);
}
