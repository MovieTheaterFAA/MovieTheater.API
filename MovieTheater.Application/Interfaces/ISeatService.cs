using MovieTheater.Application.Utils;

namespace MovieTheater.Application.Interfaces
{
    public interface ISeatService
    {
        Task<ApiResult<object>> HoldSeatsAsync(Guid userId, Guid showTimeId, List<Guid> seatIds);
        Task<ApiResult<object>> ConfirmBookingAsync(Guid userId, Guid showTimeId, List<Guid> seatIds);
    }
}
