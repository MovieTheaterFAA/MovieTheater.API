using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.BookingDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;
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

    public async Task<BookingResponseDto> GetBookingByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            _loggerService.Warn("Attempted to fetch booking with an empty GUID.");
            throw new ArgumentException("Invalid booking ID.");
        }

        try
        {
            //string cacheKey = $"booking:detail:{id}";
            //var cached = await _redisService.GetAsync<BookingResponseDto>(cacheKey);
            //if (cached != null) return cached;

            var bookingWithDetails = await _unitOfWork.Bookings.GetByIdAsync(
                id,
                b => b.BookingSeats,
                b => b.BookingFoods,
                b => b.Member,
                b => b.Showtime);

            if (bookingWithDetails == null || bookingWithDetails.IsDeleted)
            {
                _loggerService.Warn($"No booking found with ID: {id}");
                throw new KeyNotFoundException($"Booking with ID {id} not found.");
            }
            _loggerService.Info($"Booking found with ID: {id}, User ID: {bookingWithDetails.MemberId}");

            var member = bookingWithDetails.Member;
            if (member == null)
            {
                _loggerService.Warn($"No member found for booking ID: {id}");
                throw new KeyNotFoundException($"Member for booking ID {id} not found.");
            }

            var showTime = bookingWithDetails.Showtime;
            if (showTime == null)
            {
                _loggerService.Warn($"No showtime found for booking ID: {id}");
                throw new KeyNotFoundException($"Showtime for booking ID {id} not found.");
            }

            var movie = await _unitOfWork.Movies.GetByIdAsync(showTime.MovieId);
            if (movie == null)
            {
                _loggerService.Warn($"No movie found for showtime ID: {showTime.Id}");
                throw new KeyNotFoundException($"Movie for showtime ID {showTime.Id} not found.");
            }

            var bookingSeats = bookingWithDetails.BookingSeats;

            if (bookingSeats == null || !bookingSeats.Any())
            {
                _loggerService.Warn($"No seats found for booking ID: {id}");
                throw new KeyNotFoundException($"No seats found for booking ID {id}.");
            }

            var Seats = await _unitOfWork.Seats.GetAllAsync(s =>
                bookingSeats.Select(bs => bs.SeatId).Contains(s.Id) && !s.IsDeleted);

            var Foods = new List<FoodAndDrink>();
            var bookingFoods = bookingWithDetails.BookingFoods;
            if (bookingFoods != null || bookingFoods!.Any())
            {
                Foods = await _unitOfWork.FoodAndDrinks.GetAllAsync(
                    f => bookingFoods!.Select(bf => bf.FoodAndDrinkId).Contains(f.Id) && !f.IsDeleted);
            }

            var result = new BookingResponseDto
            {
                Id = bookingWithDetails.Id,
                MemberName = member.FullName,
                Movie = movie.Name,
                BookingDate = bookingWithDetails.BookingDate,
                TotalAmount = bookingWithDetails.TotalAmount,
                Status = bookingWithDetails.Status,
                BookingSeats = Seats.Select(seat => new BookingSeatDto
                {
                    SeatId = seat.Id,
                    Row = seat.Row,
                    Number = seat.Number
                }).ToList(),
                BookingFoods = Foods.Select(food => new BookingFoodDto
                {
                    FoodId = food.Id,
                    Name = food.Name,
                    Quantity = bookingFoods!.FirstOrDefault(bf => bf.FoodAndDrinkId == food.Id)?.Quantity ?? 0,
                    Price = food.Price
                }).ToList()
            };
            //await _redisService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"An unexpected error occurred while fetching booking details for ID {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<IEnumerable<BookingResponseDto>> GetUserBookingsAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            _loggerService.Warn("Attempted to fetch bookings with an empty user GUID.");
            throw new ArgumentException("Invalid user ID.");
        }

        try
        {
            //string cacheKey = $"booking:user:{userId}";
            //var cached = await _redisService.GetAsync<IEnumerable<BookingResponseDto>>(cacheKey);
            //if (cached != null) return cached;

            _loggerService.Info($"Fetching bookings for user ID: {userId}");

            var bookings = await _unitOfWork.Bookings.GetAllAsync(
                b => b.MemberId == userId && !b.IsDeleted,
                b => b.BookingSeats,
                b => b.BookingFoods,
                b => b.Member,
                b => b.Showtime);

            if (!bookings.Any())
            {
                _loggerService.Info($"No bookings found for user ID: {userId}");
                return new List<BookingResponseDto>();
            }

            var result = new List<BookingResponseDto>();

            foreach (var booking in bookings)
            {
                var showTime = booking.Showtime;
                if (showTime == null)
                {
                    _loggerService.Warn($"No showtime found for booking ID: {booking.Id}");
                    throw new KeyNotFoundException($"Showtime for booking ID {booking.Id} not found.");
                }

                var movie = await _unitOfWork.Movies.GetByIdAsync(showTime.MovieId);
                if (movie == null)
                {
                    _loggerService.Warn($"No movie found for showtime ID: {showTime.Id}");
                    throw new KeyNotFoundException($"Movie for showtime ID {showTime.Id} not found.");
                }

                var member = booking.Member;
                if (member == null)
                {
                    _loggerService.Warn($"No member found for booking ID: {booking.Id}");
                    throw new KeyNotFoundException($"Member for booking ID {booking.Id} not found.");
                }

                var bookingSeats = booking.BookingSeats;
                if (bookingSeats == null || !bookingSeats.Any())
                {
                    _loggerService.Warn($"No seats found for booking ID: {booking.Id}");
                }

                var seatIds = bookingSeats!.Select(bs => bs.SeatId).ToList();
                var seats = await _unitOfWork.Seats.GetAllAsync(s => seatIds.Contains(s.Id) && !s.IsDeleted);

                var foods = new List<FoodAndDrink>();
                var bookingFoods = booking.BookingFoods;
                if (bookingFoods != null && bookingFoods.Any())
                {
                    var foodIds = bookingFoods.Select(bf => bf.FoodAndDrinkId).ToList();
                    foods = await _unitOfWork.FoodAndDrinks.GetAllAsync(f => foodIds.Contains(f.Id) && !f.IsDeleted);
                }

                var bookingDto = new BookingResponseDto
                {
                    Id = booking.Id,
                    MemberName = member.FullName,
                    Movie = movie.Name,
                    BookingDate = booking.BookingDate,
                    Status = booking.Status,
                    TotalAmount = booking.TotalAmount,
                    BookingSeats = seats.Select(seat => new BookingSeatDto
                    {
                        SeatId = seat.Id,
                        Row = seat.Row,
                        Number = seat.Number
                    }).ToList(),
                    BookingFoods = foods.Select(food => new BookingFoodDto
                    {
                        FoodId = food.Id,
                        Name = food.Name,
                        Quantity = bookingFoods!.FirstOrDefault(bf => bf.FoodAndDrinkId == food.Id)?.Quantity ?? 0,
                        Price = food.Price
                    }).ToList()
                };

                result.Add(bookingDto);
            }

            _loggerService.Success($"Successfully retrieved {result.Count} bookings for user ID: {userId}");
            //await _redisService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"An unexpected error occurred while fetching bookings for user ID {userId}: {ex.Message}");
            throw;
        }
    }

    public async Task<Pagination<BookingResponseDto>> GetAllBookingsAsync(int page = 1, int pageSize = 10, BookingStatus? status = null,
    string? sortBy = null, bool isDescending = false, string? search = null)
    {
        try
        {
            _loggerService.Info($"Fetching bookings - Page {page}, PageSize {pageSize}, Status: {status}, Search: {search}");

            //string cacheKey = $"booking:list:{search}:{sortBy}:{isDescending}:{page}:{pageSize}:{status}";
            //var cached = await _redisService.GetAsync<Pagination<BookingResponseDto>>(cacheKey);
            //if (cached != null) return cached;

            var bookings = await _unitOfWork.Bookings.GetAllAsync(b => !b.IsDeleted);
            var query = bookings.AsQueryable();

            // Apply status filter if provided
            if (status.HasValue)
            {
                string statusString = status.ToString()!;
                query = query.Where(b => b.Status.Equals(statusString, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();

                var memberIds = (await _unitOfWork.Users.GetAllAsync(u =>
                    !u.IsDeleted && u.FullName != null && u.FullName.ToLower().Contains(lowerSearch)))
                    .Select(u => u.Id)
                    .ToList();

                var showtimeIds = (await _unitOfWork.ShowTimes.GetAllAsync(st =>
                    !st.IsDeleted && st.Movie != null && !st.Movie.IsDeleted &&
                    st.Movie.Name != null && st.Movie.Name.ToLower().Contains(lowerSearch)))
                    .Select(st => st.Id)
                    .ToList();

                query = query.Where(b =>
                    memberIds.Contains(b.MemberId) ||
                    showtimeIds.Contains(b.ShowtimeId));
            }

            var totalItems = query.Count();

            // Apply sorting
            query = sortBy?.ToLower() switch
            {
                "date" => isDescending ? query.OrderByDescending(b => b.BookingDate) : query.OrderBy(b => b.BookingDate),
                "amount" => isDescending ? query.OrderByDescending(b => b.TotalAmount) : query.OrderBy(b => b.TotalAmount),
                _ => isDescending ? query.OrderByDescending(b => b.BookingDate) : query.OrderBy(b => b.BookingDate)
            };

            // Apply pagination
            var pagedItems = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Load complete related data for each booking
            var result = new List<BookingResponseDto>();

            foreach (var booking in pagedItems)
            {
                // Get full booking details including seats and foods
                var completeBooking = await _unitOfWork.Bookings.GetByIdAsync(booking.Id,
                    b => b.BookingSeats,
                    b => b.BookingFoods,
                    b => b.Member,
                    b => b.Showtime);

                var member = completeBooking!.Member;
                if (member == null)
                {
                    _loggerService.Warn($"No member found for booking ID: {completeBooking.Id}");
                    continue;
                }

                var showTime = completeBooking.Showtime;
                if (showTime == null)
                {
                    _loggerService.Warn($"No showtime found for booking ID: {completeBooking.Id}");
                    continue;
                }

                var movie = await _unitOfWork.Movies.GetByIdAsync(showTime.MovieId);
                if (movie == null)
                {
                    _loggerService.Warn($"No movie found for showtime ID: {showTime.Id}");
                    continue;
                }

                var bookingSeats = completeBooking.BookingSeats;
                var seats = await _unitOfWork.Seats.GetAllAsync(s => bookingSeats.Select(bs => bs.SeatId).Contains(s.Id) && !s.IsDeleted);

                var foods = new List<FoodAndDrink>();
                var bookingFoods = completeBooking.BookingFoods;
                if (bookingFoods != null && bookingFoods.Any())
                {
                    foods = await _unitOfWork.FoodAndDrinks.GetAllAsync(
                        f => bookingFoods.Select(bf => bf.FoodAndDrinkId).Contains(f.Id) && !f.IsDeleted);
                }

                var bookingDto = new BookingResponseDto
                {
                    Id = completeBooking.Id,
                    MemberName = member.FullName,
                    Movie = movie.Name,
                    BookingDate = completeBooking.BookingDate,
                    Status = completeBooking.Status,
                    TotalAmount = completeBooking.TotalAmount,
                    BookingSeats = seats.Select(seat => new BookingSeatDto
                    {
                        SeatId = seat.Id,
                        Row = seat.Row,
                        Number = seat.Number
                    }).ToList(),
                    BookingFoods = foods.Select(food => new BookingFoodDto
                    {
                        FoodId = food.Id,
                        Name = food.Name,
                        Quantity = bookingFoods!.FirstOrDefault(bf => bf.FoodAndDrinkId == food.Id)?.Quantity ?? 0,
                        Price = food.Price
                    }).ToList()
                };

                result.Add(bookingDto);
            }

            var response = new Pagination<BookingResponseDto>(result, totalItems, page, pageSize);
            //await _redisService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));
            _loggerService.Success($"Retrieved {result.Count} bookings on page {page} successfully.");

            return response;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Failed to retrieve bookings. Exception: {ex.Message}");
            throw new Exception("An error occurred while retrieving booking items. Please try again later.");
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
            var showTime = await _unitOfWork.ShowTimes.GetByIdAsync(request.ShowTimeId);
            if (showTime == null)
            {
                _loggerService.Warn($"Invalid showtime ID: {request.ShowTimeId}");
                throw new ArgumentException("Invalid showtime");
            }

            var existSeat = await _unitOfWork.ShowTimeSeats.GetAllAsync(
                sts => sts.ShowTimeId == request.ShowTimeId && request.SeatIds.Contains(sts.SeatId));

            decimal totalAmount = 0;

            // Check if any selected seat is already booked
            if (existSeat.Any(s => s.Status == SeatStatus.Booked || s.Status == SeatStatus.Sold))
            {
                _loggerService.Warn($"Attempted to book unavailable seats for showtime: {request.ShowTimeId}");
                throw new InvalidOperationException("One or more selected seats are not available");
            }

            var selectedSeats = await _unitOfWork.Seats.GetAllAsync(s => request.SeatIds.Contains(s.Id));

            foreach (var seat in selectedSeats)
            {
                totalAmount += GetSeatPrice(seat);
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
                    SeatId = seat.Id
                }).ToList()
            };

            await _unitOfWork.Bookings.AddAsync(booking);

            // Update seat status to booked
            foreach (var seat in selectedSeats)
            {
                var ShowTimeSeat = new ShowTimeSeat
                {
                    ShowTimeId = request.ShowTimeId,
                    SeatId = seat.Id,
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

            var result = MapToDto(booking!);
            return result;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error creating booking for user {userId}: {ex.Message}");
            throw;
        }
    }

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

            if (booking == null || booking.IsDeleted)
            {
                _loggerService.Warn($"No booking found with ID: {bookingId} or booking is already deleted");
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
                Row = bs.Seat?.Row!,
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
