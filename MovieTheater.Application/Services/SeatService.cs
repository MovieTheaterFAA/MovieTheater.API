using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System.Collections.Concurrent;

namespace MovieTheater.Application.Services
{
    public class SeatService : ISeatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISeatNotificationService _seatNotificationService;
        // Biến static lưu trạng thái giữ ghế tạm thời
        private static readonly ConcurrentDictionary<Guid, (Guid userId, DateTime expireAt)> _holdingSeats = new();

        public SeatService(IUnitOfWork unitOfWork, ISeatNotificationService seatNotificationService)
        {
            _unitOfWork = unitOfWork;
            _seatNotificationService = seatNotificationService;
        }

        // Khi user xác nhận chọn ghế (chưa booking)
        public async Task<ApiResult<object>> HoldSeatsAsync(Guid userId, Guid showTimeId, List<Guid> seatIds)
        {
            var now = DateTime.UtcNow;
            var expireAt = now.AddMinutes(10);

            foreach (var seatId in seatIds)
            {
                // Kiểm tra ghế đã bị giữ hoặc bán chưa
                var seat = await _unitOfWork.Seats.GetByIdAsync(seatId);
                if (seat.Status != SeatStatus.Available)
                    return ApiResult<object>.Failure("409", $"Seat {seat.Row}{seat.Number} is not available.");

                // Đánh dấu giữ ghế
                seat.Status = SeatStatus.Booked;
                _holdingSeats[seatId] = (userId, expireAt);
                await _unitOfWork.Seats.Update(seat);
            }
            await _unitOfWork.SaveChangesAsync();

            // Gửi realtime cho các client khác
            await _seatNotificationService.NotifySeatsUpdated(
                showTimeId,
                seatIds.Select(id => new { SeatId = id, Status = "Booked" })
            );

            // Bắt đầu timer tự động trả ghế sau 10 phút nếu chưa booking
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(10));
                foreach (var seatId in seatIds)
                {
                    if (_holdingSeats.TryGetValue(seatId, out var holdInfo) && holdInfo.expireAt <= DateTime.UtcNow)
                    {
                        var seat = await _unitOfWork.Seats.GetByIdAsync(seatId);
                        if (seat.Status == SeatStatus.Booked)
                        {
                            seat.Status = SeatStatus.Available;
                            await _unitOfWork.Seats.Update(seat);
                            await _unitOfWork.SaveChangesAsync();
                            _holdingSeats.TryRemove(seatId, out _);

                            // Gửi realtime trả ghế
                            await _seatNotificationService.NotifySeatsUpdated(
                                showTimeId,
                                new[] { new { SeatId = seatId, Status = "Available" } }
                            );
                        }
                    }
                }
            });

            return ApiResult<object>.Success(null, "200", "Seats are held for 10 minutes.");
        }

        // Khi user booking thành công
        public async Task<ApiResult<object>> ConfirmBookingAsync(Guid userId, Guid showTimeId, List<Guid> seatIds)
        {
            foreach (var seatId in seatIds)
            {
                var seat = await _unitOfWork.Seats.GetByIdAsync(seatId);
                if (seat.Status != SeatStatus.Booked)
                    return ApiResult<object>.Failure("409", $"Seat {seat.Row}{seat.Number} is not holding.");

                seat.Status = SeatStatus.Sold;
                await _unitOfWork.Seats.Update(seat);
                _holdingSeats.TryRemove(seatId, out _);
            }
            await _unitOfWork.SaveChangesAsync();

            // Gửi realtime cập nhật ghế đã bán
            await _seatNotificationService.NotifySeatsUpdated(
                showTimeId,
                seatIds.Select(id => new { SeatId = id, Status = "Sold" })
            );

            return ApiResult<object>.Success(null, "200", "Booking confirmed.");
        }
    }
}
