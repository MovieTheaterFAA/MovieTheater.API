using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.BookingDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Application.Services;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggerService _loggerService;
    private readonly IRedisService _redisService;

    public BookingService(IUnitOfWork unitOfWork, ILoggerService loggerService, IRedisService redisService)
    {
        _unitOfWork = unitOfWork;
        _loggerService = loggerService;
        _redisService = redisService;
    }

    public async Task<BookingDto> GetBookingByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            _loggerService.Warn("Attempted to fetch booking with an empty GUID.");
            throw new ArgumentException("Invalid booking ID.");
        }

        try
        {
            string cacheKey = $"booking:detail:{id}";
            var cached = await _redisService.GetAsync<BookingDto>(cacheKey);
            if (cached != null) return cached;

            var booking = await _unitOfWork.Bookings.GetByIdAsync(id,
                b => b.BookingSeats,
                b => b.BookingFoods);

            if (booking == null)
            {
                _loggerService.Warn($"No booking found with ID: {id}");
                return null;
            }

            var result = MapToDto(booking);
            await _redisService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"An unexpected error occurred while fetching booking details for ID {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<IEnumerable<BookingDto>> GetUserBookingsAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            _loggerService.Warn("Attempted to fetch bookings with an empty user GUID.");
            throw new ArgumentException("Invalid user ID.");
        }

        try
        {
            string cacheKey = $"booking:user:{userId}";
            var cached = await _redisService.GetAsync<IEnumerable<BookingDto>>(cacheKey);
            if (cached != null) return cached;

            var bookings = await _unitOfWork.Bookings.GetAllAsync(
                b => b.MemberId == userId,
                b => b.BookingSeats,
                b => b.BookingFoods);

            var result = bookings.Select(MapToDto).ToList();
            await _redisService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"An unexpected error occurred while fetching bookings for user ID {userId}: {ex.Message}");
            throw;
        }
    }

    public async Task<BookingDto> CreateBookingAsync(Guid userId, CreateBookingRequest request)
    {
        if (userId == Guid.Empty)
        {
            _loggerService.Warn("Attempted to create booking with an empty user GUID.");
            throw new ArgumentException("Invalid user ID.");
        }

        try
        {
            _loggerService.Info($"Starting booking creation for user: {userId}, showtime: {request.ShowTimeId}");

            // Validate the seats are available
            var showTime = await _unitOfWork.ShowTimes.GetByIdAsync(request.ShowTimeId, st => st.ShowTimeSeats);
            if (showTime == null)
            {
                _loggerService.Warn($"Invalid showtime ID: {request.ShowTimeId}");
                throw new ArgumentException("Invalid showtime");
            }

            var selectedSeats = await _unitOfWork.ShowTimeSeats.GetAllAsync(
                sts => sts.ShowTimeId == request.ShowTimeId && request.SeatIds.Contains(sts.SeatId));

            decimal totalAmount = 0;

            // Check if any selected seat is already booked
            if (selectedSeats.Any(s => s.Status == SeatStatus.Booked || s.Status == SeatStatus.Sold))
            {
                _loggerService.Warn($"Attempted to book unavailable seats for showtime: {request.ShowTimeId}");
                throw new InvalidOperationException("One or more selected seats are not available");
            }
            foreach (var seat in selectedSeats)
            {
                totalAmount += GetSeatPrice(seat.Seat);
            }
            // Get food items
            var foodItems = new List<FoodAndDrink>();
            if (request.FoodItems.Any())
            {
                foodItems = await _unitOfWork.FoodAndDrinks.GetAllAsync(
                    f => request.FoodItems.Select(fi => fi.FoodId).Contains(f.Id));
            }
            foreach (var food in foodItems)
            {
                totalAmount += food.Price * request.FoodItems.First(fi => fi.FoodId == food.Id).Quantity;
            }

            // Create the booking
            var booking = new Booking
            {
                MemberId = userId,
                ShowtimeId = request.ShowTimeId,
                BookingDate = DateTime.UtcNow,
                Status = "Created",
                TotalAmount = totalAmount,
                BookingFoods = request.FoodItems.Select(fi => new BookingFood
                {
                    FoodAndDrinkId = fi.FoodId,
                    Quantity = fi.Quantity
                }).ToList(),
                BookingSeats = selectedSeats.Select(seat => new BookingSeat
                {
                    SeatId = seat.SeatId
                }).ToList()
            };

            await _unitOfWork.Bookings.AddAsync(booking);

            // Update seat status to booked
            foreach (var seat in selectedSeats)
            {
                var ShowTimeSeat = new ShowTimeSeat
                {
                    ShowTimeId = request.ShowTimeId,
                    SeatId = seat.SeatId,
                    Status = SeatStatus.Booked
                };
                await _unitOfWork.ShowTimeSeats.AddAsync(ShowTimeSeat);
            }

            await _unitOfWork.SaveChangesAsync();
            _loggerService.Success($"Booking created successfully with ID: {booking.Id}");

            // Clear related caches
            await _redisService.RemoveByPatternAsync($"booking:user:{userId}");

            // Reload the booking with relationships
            booking = await _unitOfWork.Bookings.GetByIdAsync(booking.Id,
                b => b.BookingSeats,
                b => b.BookingFoods);

            var result = MapToDto(booking);
            return result;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error creating booking for user {userId}: {ex.Message}");
            throw;
        }
    }

    //public async Task<BookingResult> CreateBookingWithInvoiceAsync(Guid userId, CreateBookingRequest request)
    //{
    //    if (string.IsNullOrEmpty(request.ReservationCode))
    //    {
    //        _loggerService.Warn("Attempted to create booking without reservation code");
    //        throw new ArgumentException("Reservation code is required");
    //    }

    //    try
    //    {
    //        // Begin transaction
    //        await using var transaction = await _unitOfWork.BeginTransactionAsync();

    //        // Verify reservation is valid
    //        var reservation = await _redisService.GetAsync<SeatReservation>($"reservation:{request.ReservationCode}");
    //        if (reservation == null || reservation.ExpiryTime < DateTime.UtcNow)
    //        {
    //            _loggerService.Warn($"Reservation {request.ReservationCode} not found or expired");
    //            throw new InvalidOperationException("Seat reservation has expired");
    //        }

    //        // Create the booking
    //        var booking = new Booking
    //        {
    //            MemberId = userId,
    //            ShowtimeId = request.ShowTimeId,
    //            BookingDate = DateTime.UtcNow,
    //            ReservationCode = request.ReservationCode,
    //            Status = BookingStatus.Created,
    //            BookingSeats = request.SeatIds.Select(seatId => new BookingSeat
    //            {
    //                SeatId = seatId
    //            }).ToList(),
    //            BookingFoods = request.FoodItems.Select(fi => new BookingFood
    //            {
    //                FoodAndDrinkId = fi.FoodId,
    //                Quantity = fi.Quantity
    //            }).ToList()
    //        };

    //        await _unitOfWork.Bookings.AddAsync(booking);
    //        await _unitOfWork.SaveChangesAsync();

    //        // Create invoice
    //        var invoice = new Invoice
    //        {
    //            BookingId = booking.Id,
    //            InvoiceDate = DateTime.UtcNow,
    //            Amount = CalculateTotalAmount(booking),
    //            Status = "Pending"
    //        };

    //        await _unitOfWork.Invoices.AddAsync(invoice);
    //        await _unitOfWork.SaveChangesAsync();

    //        // Update seat status to Booked only after successful invoice creation
    //        var showTimeSeats = await _unitOfWork.ShowTimeSeats.GetAllAsync(
    //            sts => sts.ShowTimeId == request.ShowTimeId &&
    //                  sts.ReservationCode == request.ReservationCode);

    //        foreach (var seat in showTimeSeats)
    //        {
    //            seat.Status = SeatStatus.Booked;
    //            seat.ReservationCode = null;
    //            seat.ReservationExpiry = null;
    //        }

    //        await _unitOfWork.ShowTimeSeats.UpdateRange(showTimeSeats.ToList());
    //        await _unitOfWork.SaveChangesAsync();

    //        // Commit transaction
    //        await transaction.CommitAsync();

    //        // Clear reservation from Redis
    //        await _redisService.RemoveAsync($"reservation:{request.ReservationCode}");

    //        _loggerService.Success($"Successfully created booking {booking.Id} with invoice {invoice.Id}");

    //        return new BookingResult
    //        {
    //            BookingId = booking.Id,
    //            InvoiceId = invoice.Id,
    //            Amount = invoice.Amount,
    //            Status = booking.Status
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        _loggerService.Error($"Error creating booking with invoice: {ex.Message}");
    //        throw;
    //    }
    //}

    public async Task<bool> CancelBookingAsync(Guid bookingId)
    {
        if (bookingId == Guid.Empty)
        {
            _loggerService.Warn("Attempted to cancel booking with an empty GUID.");
            throw new ArgumentException("Invalid booking ID.");
        }

        try
        {
            _loggerService.Info($"Starting booking cancellation for ID: {bookingId}");

            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId, b => b.BookingSeats);
            if (booking == null)
            {
                _loggerService.Warn($"No booking found with ID: {bookingId}");
                return false;
            }

            // Get the associated ShowTimeSeats to update their status
            var showTimeSeats = await _unitOfWork.ShowTimeSeats.GetAllAsync(
                sts => sts.ShowTimeId == booking.ShowtimeId &&
                      booking.BookingSeats.Select(bs => bs.SeatId).Contains(sts.SeatId));

            // Update seat status back to available
            foreach (var seat in showTimeSeats)
            {
                seat.Status = SeatStatus.Available;
            }
            await _unitOfWork.ShowTimeSeats.UpdateRange(showTimeSeats.ToList());

            // Soft delete the booking
            await _unitOfWork.Bookings.SoftRemove(booking);
            await _unitOfWork.SaveChangesAsync();

            // Clear related caches
            await _redisService.RemoveAsync($"booking:detail:{bookingId}");
            await _redisService.RemoveByPatternAsync($"booking:user:{booking.MemberId}");

            _loggerService.Success($"Booking {bookingId} cancelled successfully");
            return true;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error cancelling booking {bookingId}: {ex.Message}");
            throw;
        }
    }

    //public async Task<ReservationResult> ReserveSeatsAsync(Guid showTimeId, List<Guid> seatIds, TimeSpan reservationDuration = default)
    //{
    //    if (reservationDuration == default)
    //        reservationDuration = TimeSpan.FromMinutes(10);

    //    try
    //    {
    //        _loggerService.Info($"Attempting to reserve {seatIds.Count} seats for showtime {showTimeId}");

    //        var showTimeSeats = await _unitOfWork.ShowTimeSeats.GetAllAsync(
    //            sts => sts.ShowTimeId == showTimeId && seatIds.Contains(sts.SeatId));

    //        // Check if any seats are already reserved or booked
    //        if (showTimeSeats.Any(s => s.Status != SeatStatus.Available))
    //        {
    //            var unavailableSeats = showTimeSeats.Where(s => s.Status != SeatStatus.Available)
    //                .Select(s => s.SeatId).ToList();

    //            _loggerService.Warn($"Attempted to reserve unavailable seats: {string.Join(", ", unavailableSeats)}");
    //            return new ReservationResult { Success = false, UnavailableSeats = unavailableSeats };
    //        }

    //        // Create reservation code
    //        var reservationCode = OtpGenerator.GenerateAlphanumeric(8);
    //        var expiryTime = DateTime.UtcNow.Add(reservationDuration);

    //        // Update seat status to Reserved
    //        foreach (var seat in showTimeSeats)
    //        {
    //            seat.Status = SeatStatus.Reserved;
    //            seat.ReservationCode = reservationCode;
    //            seat.ReservationExpiry = expiryTime;
    //        }

    //        await _unitOfWork.ShowTimeSeats.UpdateRange(showTimeSeats.ToList());
    //        await _unitOfWork.SaveChangesAsync();

    //        // Store reservation in Redis for quick access
    //        var reservation = new SeatReservation
    //        {
    //            ReservationCode = reservationCode,
    //            ShowTimeId = showTimeId,
    //            SeatIds = seatIds,
    //            ExpiryTime = expiryTime
    //        };

    //        await _redisService.SetAsync(
    //            $"reservation:{reservationCode}",
    //            reservation,
    //            reservationDuration);

    //        _loggerService.Success($"Successfully reserved seats with code {reservationCode}");

    //        return new ReservationResult
    //        {
    //            Success = true,
    //            ReservationCode = reservationCode,
    //            ExpiryTime = expiryTime
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        _loggerService.Error($"Error reserving seats: {ex.Message}");
    //        throw;
    //    }
    //}

    private BookingDto MapToDto(Booking booking)
    {
        return new BookingDto
        {
            Id = booking.Id,
            UserId = booking.MemberId,
            ShowTimeId = booking.ShowtimeId,
            BookingDate = booking.BookingDate,
            TotalAmount = CalculateTotalAmount(booking),
            BookingSeats = booking.BookingSeats?.Select(bs => new BookingSeatDto
            {
                SeatId = bs.SeatId,
                Row = bs.Seat?.Row,
                Number = bs.Seat?.Number ?? 0,
            }).ToList() ?? new List<BookingSeatDto>(),
            BookingFoods = booking.BookingFoods?.Select(bf => new BookingFoodDto
            {
                FoodId = bf.FoodAndDrinkId,
                Name = bf.FoodAndDrink?.Name ?? "Unknown",
                Quantity = bf.Quantity,
                Price = bf.FoodAndDrink?.Price ?? 0
            }).ToList() ?? new List<BookingFoodDto>()
        };
    }

    private decimal CalculateTotalAmount(Booking booking)
    {
        decimal seatTotal = booking.BookingSeats?.Sum(bs => GetSeatPrice(bs.Seat)) ?? 0;
        decimal foodTotal = booking.BookingFoods?.Sum(bf => (bf.FoodAndDrink?.Price ?? 0) * bf.Quantity) ?? 0;

        return seatTotal + foodTotal;
    }

    private decimal GetSeatPrice(Seat seat)
    {
        // Logic to determine seat price based on seat type
        if (seat == null)
            return 0;

        return seat.Type switch
        {
            SeatType.Normal => 80000,  // Example prices
            SeatType.VIP => 120000,
            SeatType.Couple => 200000,
            _ => 80000
        };
    }
}
