using Moq;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.DTOs.BookingDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.UnitTest.Services;

public class BookingServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILoggerService> _mockLoggerService;
    private readonly Mock<IRedisService> _mockRedisService;
    private readonly Mock<IGenericRepository<Booking>> _mockBookingRepository;
    private readonly Mock<IGenericRepository<User>> _mockUserRepository;
    private readonly Mock<IGenericRepository<Movie>> _mockMovieRepository;
    private readonly Mock<IGenericRepository<ShowTime>> _mockShowTimeRepository;
    private readonly Mock<IGenericRepository<Seat>> _mockSeatRepository;
    private readonly Mock<IGenericRepository<ShowTimeSeat>> _mockShowTimeSeatRepository;
    private readonly Mock<IGenericRepository<FoodAndDrink>> _mockFoodAndDrinkRepository;
    private readonly BookingService _bookingService;

    public BookingServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLoggerService = new Mock<ILoggerService>();
        _mockRedisService = new Mock<IRedisService>();
        _mockBookingRepository = new Mock<IGenericRepository<Booking>>();
        _mockUserRepository = new Mock<IGenericRepository<User>>();
        _mockMovieRepository = new Mock<IGenericRepository<Movie>>();
        _mockShowTimeRepository = new Mock<IGenericRepository<ShowTime>>();
        _mockSeatRepository = new Mock<IGenericRepository<Seat>>();
        _mockShowTimeSeatRepository = new Mock<IGenericRepository<ShowTimeSeat>>();
        _mockFoodAndDrinkRepository = new Mock<IGenericRepository<FoodAndDrink>>();

        _mockUnitOfWork.Setup(u => u.Bookings).Returns(_mockBookingRepository.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.Movies).Returns(_mockMovieRepository.Object);
        _mockUnitOfWork.Setup(u => u.ShowTimes).Returns(_mockShowTimeRepository.Object);
        _mockUnitOfWork.Setup(u => u.Seats).Returns(_mockSeatRepository.Object);
        _mockUnitOfWork.Setup(u => u.ShowTimeSeats).Returns(_mockShowTimeSeatRepository.Object);
        _mockUnitOfWork.Setup(u => u.FoodAndDrinks).Returns(_mockFoodAndDrinkRepository.Object);

        _bookingService = new BookingService(
            _mockUnitOfWork.Object,
            _mockLoggerService.Object,
            _mockRedisService.Object
        );
    }


    [Fact]
    public async Task GetBookingByIdAsync_ValidBookingId_ReturnsBookingResponse()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var foodId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            FullName = "John Doe"
        };

        var movie = new Movie
        {
            Id = movieId,
            Name = "Test Movie"
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            MovieId = movieId
        };

        var seat = new Seat
        {
            Id = seatId,
            Row = "A",
            Number = 1,
            Type = SeatType.Normal
        };

        var food = new FoodAndDrink
        {
            Id = foodId,
            Name = "Popcorn",
            Price = 15000,
            Type = FoodType.Food
        };

        var booking = new Booking
        {
            Id = bookingId,
            MemberId = userId,
            Member = user,
            ShowtimeId = showTimeId,
            Showtime = showTime,
            BookingDate = DateTime.UtcNow,
            TotalAmount = 95000,
            Status = "Created",
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = seatId, BookingId = bookingId }
            },
            BookingFoods = new List<BookingFood>
            {
                new() { FoodAndDrinkId = foodId, BookingId = bookingId, Quantity = 1 }
            }
        };

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        _mockMovieRepository.Setup(r => r.GetByIdAsync(movieId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Movie, object>>[]>()))
            .ReturnsAsync(movie);

        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat> { seat });

        _mockFoodAndDrinkRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()))
            .ReturnsAsync(new List<FoodAndDrink> { food });

        // Act
        var result = await _bookingService.GetBookingByIdAsync(bookingId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(bookingId, result.Id);
        Assert.Equal("John Doe", result.MemberName);
        Assert.Equal("Test Movie", result.Movie);
        Assert.Equal(95000, result.TotalAmount);
        Assert.Equal("Created", result.Status);
        Assert.Single(result.BookingSeats);
        Assert.Single(result.BookingFoods);

        _mockLoggerService.Verify(
            l => l.Info($"Booking found with ID: {bookingId}, User ID: {userId}"),
            Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_EmptyBookingId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _bookingService.GetBookingByIdAsync(Guid.Empty));

        Assert.Equal("Invalid booking ID.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn("Attempted to fetch booking with an empty GUID."),
            Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_BookingNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync((Booking)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _bookingService.GetBookingByIdAsync(bookingId));

        Assert.Equal($"Booking with ID {bookingId} not found.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"No booking found with ID: {bookingId}"),
            Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_DeletedBooking_ThrowsKeyNotFoundException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var deletedBooking = new Booking
        {
            Id = bookingId,
            IsDeleted = true
        };

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(deletedBooking);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _bookingService.GetBookingByIdAsync(bookingId));

        Assert.Equal($"Booking with ID {bookingId} not found.", ex.Message);
    }

    [Fact]
    public async Task GetBookingByIdAsync_MemberNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var booking = new Booking
        {
            Id = bookingId,
            Member = null!,
            Showtime = new ShowTime()
        };

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _bookingService.GetBookingByIdAsync(bookingId));

        Assert.Equal($"Member for booking ID {bookingId} not found.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"No member found for booking ID: {bookingId}"),
            Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShowtimeNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var booking = new Booking
        {
            Id = bookingId,
            Member = new User { FullName = "Test User" },
            Showtime = null!
        };

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _bookingService.GetBookingByIdAsync(bookingId));

        Assert.Equal($"Showtime for booking ID {bookingId} not found.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"No showtime found for booking ID: {bookingId}"),
            Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_MovieNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var booking = new Booking
        {
            Id = bookingId,
            Member = new User { FullName = "Test User" },
            Showtime = new ShowTime { MovieId = movieId }
        };

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        _mockMovieRepository.Setup(r => r.GetByIdAsync(movieId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Movie, object>>[]>()))
            .ReturnsAsync((Movie)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _bookingService.GetBookingByIdAsync(bookingId));

        Assert.Equal($"Movie for showtime ID {booking.Showtime.Id} not found.", ex.Message);
    }

    [Fact]
    public async Task GetBookingByIdAsync_NoSeatsFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var booking = new Booking
        {
            Id = bookingId,
            Member = new User { FullName = "Test User" },
            Showtime = new ShowTime { MovieId = Guid.NewGuid() },
            BookingSeats = null!
        };

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        _mockMovieRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Movie, object>>[]>()))
            .ReturnsAsync(new Movie { Name = "Test Movie" });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _bookingService.GetBookingByIdAsync(bookingId));

        Assert.Equal($"No seats found for booking ID {bookingId}.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"No seats found for booking ID: {bookingId}"),
            Times.Once);
    }

    [Fact]
    public async Task GetUserBookingsAsync_ValidUserId_ReturnsUserBookings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            FullName = "John Doe"
        };

        var movie = new Movie
        {
            Id = movieId,
            Name = "Test Movie"
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            MovieId = movieId
        };

        var booking = new Booking
        {
            Id = bookingId,
            MemberId = userId,
            Member = user,
            ShowtimeId = showTimeId,
            Showtime = showTime,
            BookingDate = DateTime.UtcNow,
            TotalAmount = 80000,
            Status = "Created",
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = Guid.NewGuid(), BookingId = bookingId }
            },
            BookingFoods = new List<BookingFood>()
        };

        var bookings = new List<Booking> { booking };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(bookings);

        _mockMovieRepository.Setup(r => r.GetByIdAsync(movieId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Movie, object>>[]>()))
            .ReturnsAsync(movie);

        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat>
            {
                new() { Id = booking.BookingSeats.First().SeatId, Row = "A", Number = 1 }
            });

        // Act
        var result = await _bookingService.GetUserBookingsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var bookingDto = result.First();
        Assert.Equal(bookingId, bookingDto.Id);
        Assert.Equal("John Doe", bookingDto.MemberName);
        Assert.Equal("Test Movie", bookingDto.Movie);

        _mockLoggerService.Verify(
            l => l.Info($"Fetching bookings for user ID: {userId}"),
            Times.Once);

        _mockLoggerService.Verify(
            l => l.Success($"Successfully retrieved 1 bookings for user ID: {userId}"),
            Times.Once);
    }

    [Fact]
    public async Task GetUserBookingsAsync_EmptyUserId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _bookingService.GetUserBookingsAsync(Guid.Empty));

        Assert.Equal("Invalid user ID.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn("Attempted to fetch bookings with an empty user GUID."),
            Times.Once);
    }

    [Fact]
    public async Task GetUserBookingsAsync_NoBookingsFound_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(new List<Booking>());

        // Act
        var result = await _bookingService.GetUserBookingsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        _mockLoggerService.Verify(
            l => l.Info($"No bookings found for user ID: {userId}"),
            Times.Once);
    }
    [Fact]
    public async Task GetUserBookingsAsync_ShowtimeNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            FullName = "John Doe"
        };

        var booking = new Booking
        {
            Id = bookingId,
            MemberId = userId,
            Member = user,
            ShowtimeId = Guid.NewGuid(),
            Showtime = null!, // This is the key - showtime is null
            BookingDate = DateTime.UtcNow,
            TotalAmount = 80000,
            Status = "Created",
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = Guid.NewGuid(), BookingId = bookingId }
            },
            BookingFoods = new List<BookingFood>()
        };

        var bookings = new List<Booking> { booking };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(bookings);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _bookingService.GetUserBookingsAsync(userId));

        Assert.Equal($"Showtime for booking ID {bookingId} not found.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"No showtime found for booking ID: {bookingId}"),
            Times.Once);
    }

    [Fact]
    public async Task GetUserBookingsAsync_MovieNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            FullName = "John Doe"
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            MovieId = movieId // Valid showtime with movieId
        };

        var booking = new Booking
        {
            Id = bookingId,
            MemberId = userId,
            Member = user,
            ShowtimeId = showTimeId,
            Showtime = showTime, // Valid showtime
            BookingDate = DateTime.UtcNow,
            TotalAmount = 80000,
            Status = "Created",
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = Guid.NewGuid(), BookingId = bookingId }
            },
            BookingFoods = new List<BookingFood>()
        };

        var bookings = new List<Booking> { booking };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(bookings);

        // Setup movie repository to return null - this is the key part
        _mockMovieRepository.Setup(r => r.GetByIdAsync(movieId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Movie, object>>[]>()))
            .ReturnsAsync((Movie)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _bookingService.GetUserBookingsAsync(userId));

        Assert.Equal($"Movie for showtime ID {showTimeId} not found.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"No movie found for showtime ID: {showTimeId}"),
            Times.Once);
    }

    [Fact]
    public async Task GetUserBookingsAsync_MemberNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();

        var showTime = new ShowTime
        {
            Id = showTimeId,
            MovieId = movieId
        };

        var movie = new Movie
        {
            Id = movieId,
            Name = "Test Movie"
        };

        var booking = new Booking
        {
            Id = bookingId,
            MemberId = userId,
            Member = null, // This is the key - member is null
            ShowtimeId = showTimeId,
            Showtime = showTime,
            BookingDate = DateTime.UtcNow,
            TotalAmount = 80000,
            Status = "Created",
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = Guid.NewGuid(), BookingId = bookingId }
            },
            BookingFoods = new List<BookingFood>()
        };

        var bookings = new List<Booking> { booking };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(bookings);

        _mockMovieRepository.Setup(r => r.GetByIdAsync(movieId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Movie, object>>[]>()))
            .ReturnsAsync(movie);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _bookingService.GetUserBookingsAsync(userId));

        Assert.Equal($"Member for booking ID {bookingId} not found.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"No member found for booking ID: {bookingId}"),
            Times.Once);
    }

    [Fact]
    public async Task GetUserBookingsAsync_EmptySeatsCollection_LogsWarningAndContinuesProcessing()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            FullName = "John Doe"
        };

        var movie = new Movie
        {
            Id = movieId,
            Name = "Test Movie"
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            MovieId = movieId
        };

        var booking = new Booking
        {
            Id = bookingId,
            MemberId = userId,
            Member = user,
            ShowtimeId = showTimeId,
            Showtime = showTime,
            BookingDate = DateTime.UtcNow,
            TotalAmount = 80000,
            Status = "Created",
            BookingSeats = new List<BookingSeat>(), // Empty list instead of null
            BookingFoods = new List<BookingFood>()
        };

        var bookings = new List<Booking> { booking };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(bookings);

        _mockMovieRepository.Setup(r => r.GetByIdAsync(movieId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Movie, object>>[]>()))
            .ReturnsAsync(movie);

        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat>());

        _mockFoodAndDrinkRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()))
            .ReturnsAsync(new List<FoodAndDrink>());

        // Act
        var result = await _bookingService.GetUserBookingsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var bookingDto = result.First();
        Assert.Equal(bookingId, bookingDto.Id);
        Assert.Equal("John Doe", bookingDto.MemberName);
        Assert.Equal("Test Movie", bookingDto.Movie);
        Assert.Empty(bookingDto.BookingSeats); // Should be empty list

        // Verify that the warning was logged
        _mockLoggerService.Verify(
            l => l.Warn($"No seats found for booking ID: {bookingId}"),
            Times.Once);

        // Verify that processing continued and success was logged
        _mockLoggerService.Verify(
            l => l.Success($"Successfully retrieved 1 bookings for user ID: {userId}"),
            Times.Once);
    }
    [Fact]
    public async Task GetUserBookingsAsync_WithBookingFoods_ReturnsBookingsWithFoodDetails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var foodId1 = Guid.NewGuid();
        var foodId2 = Guid.NewGuid();
        var seatId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            FullName = "John Doe"
        };

        var movie = new Movie
        {
            Id = movieId,
            Name = "Test Movie"
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            MovieId = movieId
        };

        var food1 = new FoodAndDrink
        {
            Id = foodId1,
            Name = "Popcorn",
            Price = 15000,
            Type = FoodType.Food
        };

        var food2 = new FoodAndDrink
        {
            Id = foodId2,
            Name = "Cola",
            Price = 8000,
            Type = FoodType.Drink
        };

        var booking = new Booking
        {
            Id = bookingId,
            MemberId = userId,
            Member = user,
            ShowtimeId = showTimeId,
            Showtime = showTime,
            BookingDate = DateTime.UtcNow,
            TotalAmount = 100000,
            Status = "Created",
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = seatId, BookingId = bookingId }
            },
            BookingFoods = new List<BookingFood>
            {
                new() { FoodAndDrinkId = foodId1, BookingId = bookingId, Quantity = 2 },
                new() { FoodAndDrinkId = foodId2, BookingId = bookingId, Quantity = 1 }
            }
        };

        var bookings = new List<Booking> { booking };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(bookings);

        _mockMovieRepository.Setup(r => r.GetByIdAsync(movieId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Movie, object>>[]>()))
            .ReturnsAsync(movie);

        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat>
            {
                new() { Id = seatId, Row = "A", Number = 1 }
            });

        // This is the key setup - mock the food repository to return foods
        _mockFoodAndDrinkRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()))
            .ReturnsAsync(new List<FoodAndDrink> { food1, food2 });

        // Act
        var result = await _bookingService.GetUserBookingsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);

        var bookingDto = result.First();
        Assert.Equal(bookingId, bookingDto.Id);
        Assert.Equal("John Doe", bookingDto.MemberName);
        Assert.Equal("Test Movie", bookingDto.Movie);

        // Verify booking foods are properly populated
        Assert.NotNull(bookingDto.BookingFoods);
        Assert.Equal(2, bookingDto.BookingFoods.Count);

        var popcornFood = bookingDto.BookingFoods.FirstOrDefault(f => f.Name == "Popcorn");
        Assert.NotNull(popcornFood);
        Assert.Equal(foodId1, popcornFood.FoodId);
        Assert.Equal(2, popcornFood.Quantity);
        Assert.Equal(15000, popcornFood.Price);

        var colaFood = bookingDto.BookingFoods.FirstOrDefault(f => f.Name == "Cola");
        Assert.NotNull(colaFood);
        Assert.Equal(foodId2, colaFood.FoodId);
        Assert.Equal(1, colaFood.Quantity);
        Assert.Equal(8000, colaFood.Price);

        // Verify that the food repository was called with the correct food IDs
        _mockFoodAndDrinkRepository.Verify(r => r.GetAllAsync(
            It.Is<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(
                expr => expr != null), // Verify the predicate was provided
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()),
            Times.Once);

        _mockLoggerService.Verify(
            l => l.Success($"Successfully retrieved 1 bookings for user ID: {userId}"),
            Times.Once);
    }
    [Fact]
    public async Task GetUserBookingsAsync_WithBookingFoodsButFoodsNotFound_ReturnsBookingsWithEmptyFoodList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var foodId1 = Guid.NewGuid();
        var seatId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            FullName = "John Doe"
        };

        var movie = new Movie
        {
            Id = movieId,
            Name = "Test Movie"
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            MovieId = movieId
        };

        var booking = new Booking
        {
            Id = bookingId,
            MemberId = userId,
            Member = user,
            ShowtimeId = showTimeId,
            Showtime = showTime,
            BookingDate = DateTime.UtcNow,
            TotalAmount = 100000,
            Status = "Created",
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = seatId, BookingId = bookingId }
            },
            BookingFoods = new List<BookingFood>
            {
                new() { FoodAndDrinkId = foodId1, BookingId = bookingId, Quantity = 2 }
            }
        };

        var bookings = new List<Booking> { booking };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(bookings);

        _mockMovieRepository.Setup(r => r.GetByIdAsync(movieId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Movie, object>>[]>()))
            .ReturnsAsync(movie);

        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat>
            {
                new() { Id = seatId, Row = "A", Number = 1 }
            });

        // Mock food repository to return empty list (foods not found or deleted)
        _mockFoodAndDrinkRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()))
            .ReturnsAsync(new List<FoodAndDrink>());

        // Act
        var result = await _bookingService.GetUserBookingsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);

        var bookingDto = result.First();
        Assert.Equal(bookingId, bookingDto.Id);
        Assert.Equal("John Doe", bookingDto.MemberName);
        Assert.Equal("Test Movie", bookingDto.Movie);

        // Verify booking foods list is empty when no foods are found
        Assert.NotNull(bookingDto.BookingFoods);
        Assert.Empty(bookingDto.BookingFoods);

        // Verify that the food repository was still called
        _mockFoodAndDrinkRepository.Verify(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()),
            Times.Once);
    }
    [Fact]
    public async Task GetUserBookingsAsync_DatabaseError_LogsErrorAndThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var exceptionMessage = "Database connection error";

        // Setup the booking repository to throw an exception
        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _bookingService.GetUserBookingsAsync(userId));

        Assert.Equal(exceptionMessage, ex.Message);

        // Verify that the error was logged with the correct message
        _mockLoggerService.Verify(
            l => l.Error($"An unexpected error occurred while fetching bookings for user ID {userId}: {exceptionMessage}"),
            Times.Once);

        // Verify that the info log was called before the exception
        _mockLoggerService.Verify(
            l => l.Info($"Fetching bookings for user ID: {userId}"),
            Times.Once);
    }

    [Fact]
    public async Task GetAllBookingsAsync_ValidParameters_ReturnsPaginatedResults()
    {
        // Arrange
        var bookings = new List<Booking>
        {
            CreateTestBooking(),
            CreateTestBooking()
        };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(bookings);

        SetupBookingDetailsMocks();

        // Act
        var result = await _bookingService.GetAllBookingsAsync(page: 1, pageSize: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(10, result.PageSize);

        _mockLoggerService.Verify(
            l => l.Info($"Fetching bookings - Page 1, PageSize 10, Status: , Search: "),
            Times.Once);

        _mockLoggerService.Verify(
            l => l.Success(It.Is<string>(msg => msg.Contains("Retrieved") && msg.Contains("bookings on page 1 successfully"))),
            Times.Once);
    }

    [Fact]
    public async Task GetAllBookingsAsync_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange
        var bookings = new List<Booking>
        {
            new() { Id = Guid.NewGuid(), Status = "Created", BookingDate = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Status = "Confirmed", BookingDate = DateTime.UtcNow }
        };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(bookings);

        SetupBookingDetailsMocks();

        // Act
        var result = await _bookingService.GetAllBookingsAsync(status: BookingStatus.Created);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
    }
    [Fact]
    public async Task GetAllBookingsAsync_MemberNotFound_ThrowsException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        var booking = new Booking
        {
            Id = bookingId,
            BookingDate = DateTime.UtcNow,
            TotalAmount = 80000,
            Status = "Created"
        };

        var bookings = new List<Booking> { booking };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(bookings);

        // Setup the GetByIdAsync to return a booking with null member
        var completeBooking = new Booking
        {
            Id = bookingId,
            BookingDate = DateTime.UtcNow,
            TotalAmount = 80000,
            Status = "Created",
            Member = null, // This is the key - member is null
            Showtime = new ShowTime { Id = Guid.NewGuid() },
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = Guid.NewGuid() }
            },
            BookingFoods = new List<BookingFood>()
        };

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(completeBooking);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _bookingService.GetAllBookingsAsync());

        Assert.Equal("An error occurred while retrieving booking items. Please try again later.", ex.Message);

        // Verify that the error was logged with the original KeyNotFoundException message
        _mockLoggerService.Verify(
            l => l.Error(It.Is<string>(msg => msg.Contains("Failed to retrieve bookings") && msg.Contains($"Member for booking ID {bookingId} not found"))),
            Times.Once);

        // Verify that the initial info log was called
        _mockLoggerService.Verify(
            l => l.Info($"Fetching bookings - Page 1, PageSize 10, Status: , Search: "),
            Times.Once);
    }

    [Fact]
    public async Task GetAllBookingsAsync_ShowtimeNotFound_ThrowsException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        var booking = new Booking
        {
            Id = bookingId,
            BookingDate = DateTime.UtcNow,
            TotalAmount = 80000,
            Status = "Created"
        };

        var bookings = new List<Booking> { booking };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(bookings);

        // Setup the GetByIdAsync to return a booking with null showtime
        var completeBooking = new Booking
        {
            Id = bookingId,
            BookingDate = DateTime.UtcNow,
            TotalAmount = 80000,
            Status = "Created",
            Member = new User { Id = Guid.NewGuid(), FullName = "Test User" }, // Valid member
            Showtime = null, // This is the key - showtime is null
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = Guid.NewGuid() }
            },
            BookingFoods = new List<BookingFood>()
        };

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(completeBooking);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _bookingService.GetAllBookingsAsync());

        Assert.Equal("An error occurred while retrieving booking items. Please try again later.", ex.Message);

        // Verify that the error was logged with the original KeyNotFoundException message
        _mockLoggerService.Verify(
            l => l.Error(It.Is<string>(msg => msg.Contains("Failed to retrieve bookings") && msg.Contains($"Showtime for booking ID {bookingId} not found"))),
            Times.Once);

        // Verify that the initial info log was called
        _mockLoggerService.Verify(
            l => l.Info($"Fetching bookings - Page 1, PageSize 10, Status: , Search: "),
            Times.Once);
    }

    [Fact]
    public async Task GetAllBookingsAsync_MovieNotFound_ThrowsException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var movieId = Guid.NewGuid();

        var booking = new Booking
        {
            Id = bookingId,
            BookingDate = DateTime.UtcNow,
            TotalAmount = 80000,
            Status = "Created"
        };

        var bookings = new List<Booking> { booking };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(bookings);

        // Setup the GetByIdAsync to return a booking with valid member and showtime, but movie will be null
        var completeBooking = new Booking
        {
            Id = bookingId,
            BookingDate = DateTime.UtcNow,
            TotalAmount = 80000,
            Status = "Created",
            Member = new User { Id = Guid.NewGuid(), FullName = "Test User" }, // Valid member
            Showtime = new ShowTime { Id = showTimeId, MovieId = movieId }, // Valid showtime with movieId
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = Guid.NewGuid() }
            },
            BookingFoods = new List<BookingFood>()
        };

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(completeBooking);

        // Setup movie repository to return null - this is the key part
        _mockMovieRepository.Setup(r => r.GetByIdAsync(movieId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Movie, object>>[]>()))
            .ReturnsAsync((Movie)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _bookingService.GetAllBookingsAsync());

        Assert.Equal("An error occurred while retrieving booking items. Please try again later.", ex.Message);

        // Verify that the error was logged with the original KeyNotFoundException message
        _mockLoggerService.Verify(
            l => l.Error(It.Is<string>(msg => msg.Contains("Failed to retrieve bookings") && msg.Contains($"Movie for showtime ID {showTimeId} not found"))),
            Times.Once);

        // Verify that the initial info log was called
        _mockLoggerService.Verify(
            l => l.Info($"Fetching bookings - Page 1, PageSize 10, Status: , Search: "),
            Times.Once);
    }
    [Fact]
    public async Task GetAllBookingsAsync_WithBookingFoods_ReturnsBookingsWithFoodDetails()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var foodId1 = Guid.NewGuid();
        var foodId2 = Guid.NewGuid();

        var booking = new Booking
        {
            Id = bookingId,
            BookingDate = DateTime.UtcNow,
            TotalAmount = 120000,
            Status = "Created"
        };

        var bookings = new List<Booking> { booking };

        var food1 = new FoodAndDrink
        {
            Id = foodId1,
            Name = "Popcorn",
            Price = 15000,
            Type = FoodType.Food
        };

        var food2 = new FoodAndDrink
        {
            Id = foodId2,
            Name = "Cola",
            Price = 8000,
            Type = FoodType.Drink
        };

        // Setup the GetByIdAsync to return a booking with food items
        var completeBooking = new Booking
        {
            Id = bookingId,
            BookingDate = DateTime.UtcNow,
            TotalAmount = 120000,
            Status = "Created",
            Member = new User { Id = Guid.NewGuid(), FullName = "Test User" },
            Showtime = new ShowTime { Id = Guid.NewGuid(), MovieId = Guid.NewGuid() },
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = Guid.NewGuid() }
            },
            BookingFoods = new List<BookingFood>
            {
                new() { FoodAndDrinkId = foodId1, Quantity = 2 },
                new() { FoodAndDrinkId = foodId2, Quantity = 1 }
            }
        };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(bookings);

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(completeBooking);

        _mockMovieRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Movie, object>>[]>()))
            .ReturnsAsync(new Movie { Id = Guid.NewGuid(), Name = "Test Movie" });

        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat>
            {
                new() { Id = Guid.NewGuid(), Row = "A", Number = 1 }
            });

        // This is the key setup - mock the food repository to return foods
        _mockFoodAndDrinkRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()))
            .ReturnsAsync(new List<FoodAndDrink> { food1, food2 });

        // Act
        var result = await _bookingService.GetAllBookingsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);

        var bookingDto = result.Items.First();
        Assert.Equal(bookingId, bookingDto.Id);
        Assert.Equal("Test User", bookingDto.MemberName);
        Assert.Equal("Test Movie", bookingDto.Movie);

        // Verify booking foods are properly populated
        Assert.NotNull(bookingDto.BookingFoods);
        Assert.Equal(2, bookingDto.BookingFoods.Count);

        var popcornFood = bookingDto.BookingFoods.FirstOrDefault(f => f.Name == "Popcorn");
        Assert.NotNull(popcornFood);
        Assert.Equal(foodId1, popcornFood.FoodId);
        Assert.Equal(2, popcornFood.Quantity);
        Assert.Equal(15000, popcornFood.Price);

        var colaFood = bookingDto.BookingFoods.FirstOrDefault(f => f.Name == "Cola");
        Assert.NotNull(colaFood);
        Assert.Equal(foodId2, colaFood.FoodId);
        Assert.Equal(1, colaFood.Quantity);
        Assert.Equal(8000, colaFood.Price);

        // Verify that the food repository was called with the correct food IDs
        _mockFoodAndDrinkRepository.Verify(r => r.GetAllAsync(
            It.Is<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(
                expr => expr != null), // Verify the predicate was provided
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()),
            Times.Once);

        _mockLoggerService.Verify(
            l => l.Success(It.Is<string>(msg => msg.Contains("Retrieved 1 bookings on page 1 successfully"))),
            Times.Once);
    }
    [Fact]
    public async Task GetAllBookingsAsync_WithSearch_ReturnsFilteredResults()
    {
        // Arrange
        var searchTerm = "john";
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();

        // Create bookings with specific user and showtime IDs that match the search
        var bookings = new List<Booking>
    {
        new()
        {
            Id = Guid.NewGuid(),
            MemberId = userId, // Use the same user ID that matches the search
            ShowtimeId = Guid.NewGuid(),
            BookingDate = DateTime.UtcNow,
            TotalAmount = 80000,
            Status = "Created",
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = Guid.NewGuid() }
            },
            BookingFoods = new List<BookingFood>()
        },
        new()
        {
            Id = Guid.NewGuid(),
            MemberId = userId, // Use the same user ID that matches the search
            ShowtimeId = Guid.NewGuid(),
            BookingDate = DateTime.UtcNow,
            TotalAmount = 80000,
            Status = "Created",
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = Guid.NewGuid() }
            },
            BookingFoods = new List<BookingFood>()
        }
    };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(bookings);

        // Mock user repository to return a user with the search term in the name
        _mockUserRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync(new List<User>
            {
            new() { Id = userId, FullName = "John Doe" } // Use the same user ID
            });

        _mockShowTimeRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(new List<ShowTime>());

        SetupBookingDetailsMocks();

        // Act
        var result = await _bookingService.GetAllBookingsAsync(search: searchTerm);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetAllBookingsAsync_SortByDate_ReturnsSortedResults()
    {
        // Arrange
        var baseDate = DateTime.UtcNow;
        var bookings = new List<Booking>
        {
            new() { Id = Guid.NewGuid(), BookingDate = baseDate.AddDays(-1), TotalAmount = 100 },
            new() { Id = Guid.NewGuid(), BookingDate = baseDate.AddDays(1), TotalAmount = 200 }
        };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(bookings);

        SetupBookingDetailsMocks();

        // Act
        var result = await _bookingService.GetAllBookingsAsync(sortBy: "date", isDescending: false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.True(result.Items[0].BookingDate <= result.Items[1].BookingDate);
    }

    [Fact]
    public async Task GetAllBookingsAsync_SortByAmount_ReturnsSortedResults()
    {
        // Arrange
        var bookings = new List<Booking>
        {
            new() { Id = Guid.NewGuid(), BookingDate = DateTime.UtcNow, TotalAmount = 200 },
            new() { Id = Guid.NewGuid(), BookingDate = DateTime.UtcNow, TotalAmount = 100 }
        };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(bookings);

        SetupBookingDetailsMocks();

        // Act
        var result = await _bookingService.GetAllBookingsAsync(sortBy: "amount", isDescending: false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.True(result.Items[0].TotalAmount <= result.Items[1].TotalAmount);
    }

    [Fact]
    public async Task GetAllBookingsAsync_DatabaseError_ThrowsException()
    {
        // Arrange
        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _bookingService.GetAllBookingsAsync());

        Assert.Equal("An error occurred while retrieving booking items. Please try again later.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Error(It.Is<string>(msg => msg.Contains("Failed to retrieve bookings"))),
            Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WithVIPSeat_CalculatesCorrectTotalAmount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var seatId = Guid.NewGuid();

        var request = new CreateBookingRequest
        {
            ShowTimeId = showTimeId,
            SeatIds = new List<Guid> { seatId },
            FoodItems = new List<FoodOrderItem>()
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            MovieId = Guid.NewGuid()
        };

        var vipSeat = new Seat
        {
            Id = seatId,
            Row = "V",
            Number = 1,
            Type = SeatType.VIP // VIP seat type
        };

        var createdBooking = new Booking
        {
            Id = Guid.NewGuid(),
            MemberId = userId,
            ShowtimeId = showTimeId,
            BookingDate = DateTime.UtcNow,
            Status = "Created",
            TotalAmount = 120000, // VIP seat price
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = seatId, Seat = vipSeat }
            },
            BookingFoods = new List<BookingFood>()
        };

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(showTime);

        _mockShowTimeSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, object>>[]>()))
            .ReturnsAsync(new List<ShowTimeSeat>());

        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat> { vipSeat });

        _mockFoodAndDrinkRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()))
            .ReturnsAsync(new List<FoodAndDrink>());

        _mockBookingRepository.Setup(r => r.AddAsync(It.IsAny<Booking>()))
            .ReturnsAsync(createdBooking);

        _mockBookingRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(createdBooking);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _bookingService.CreateBookingAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(120000, result.TotalAmount); // VIP seat price

        _mockLoggerService.Verify(
            l => l.Success(It.Is<string>(msg => msg.Contains("Booking created successfully with ID:"))),
            Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WithCoupleSeat_CalculatesCorrectTotalAmount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var seatId = Guid.NewGuid();

        var request = new CreateBookingRequest
        {
            ShowTimeId = showTimeId,
            SeatIds = new List<Guid> { seatId },
            FoodItems = new List<FoodOrderItem>()
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            MovieId = Guid.NewGuid()
        };

        var coupleSeat = new Seat
        {
            Id = seatId,
            Row = "C",
            Number = 1,
            Type = SeatType.Couple // Couple seat type
        };

        var createdBooking = new Booking
        {
            Id = Guid.NewGuid(),
            MemberId = userId,
            ShowtimeId = showTimeId,
            BookingDate = DateTime.UtcNow,
            Status = "Created",
            TotalAmount = 200000, // Couple seat price
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = seatId, Seat = coupleSeat }
            },
            BookingFoods = new List<BookingFood>()
        };

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(showTime);

        _mockShowTimeSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, object>>[]>()))
            .ReturnsAsync(new List<ShowTimeSeat>());

        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat> { coupleSeat });

        _mockFoodAndDrinkRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()))
            .ReturnsAsync(new List<FoodAndDrink>());

        _mockBookingRepository.Setup(r => r.AddAsync(It.IsAny<Booking>()))
            .ReturnsAsync(createdBooking);

        _mockBookingRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(createdBooking);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _bookingService.CreateBookingAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200000, result.TotalAmount); // Couple seat price

        _mockLoggerService.Verify(
            l => l.Success(It.Is<string>(msg => msg.Contains("Booking created successfully with ID:"))),
            Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WithNormalSeat_CalculatesCorrectTotalAmount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var seatId = Guid.NewGuid();

        var request = new CreateBookingRequest
        {
            ShowTimeId = showTimeId,
            SeatIds = new List<Guid> { seatId },
            FoodItems = new List<FoodOrderItem>()
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            MovieId = Guid.NewGuid()
        };

        var normalSeat = new Seat
        {
            Id = seatId,
            Row = "A",
            Number = 1,
            Type = SeatType.Normal // Normal seat type (default case)
        };

        var createdBooking = new Booking
        {
            Id = Guid.NewGuid(),
            MemberId = userId,
            ShowtimeId = showTimeId,
            BookingDate = DateTime.UtcNow,
            Status = "Created",
            TotalAmount = 80000, // Normal seat price (default)
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = seatId, Seat = normalSeat }
            },
            BookingFoods = new List<BookingFood>()
        };

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(showTime);

        _mockShowTimeSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, object>>[]>()))
            .ReturnsAsync(new List<ShowTimeSeat>());

        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat> { normalSeat });

        _mockFoodAndDrinkRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()))
            .ReturnsAsync(new List<FoodAndDrink>());

        _mockBookingRepository.Setup(r => r.AddAsync(It.IsAny<Booking>()))
            .ReturnsAsync(createdBooking);

        _mockBookingRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(createdBooking);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _bookingService.CreateBookingAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(80000, result.TotalAmount); // Normal seat price

        _mockLoggerService.Verify(
            l => l.Success(It.Is<string>(msg => msg.Contains("Booking created successfully with ID:"))),
            Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WithMultipleDifferentSeatTypes_CalculatesCorrectTotalAmount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var vipSeatId = Guid.NewGuid();
        var normalSeatId = Guid.NewGuid();
        var coupleSeatId = Guid.NewGuid();

        var request = new CreateBookingRequest
        {
            ShowTimeId = showTimeId,
            SeatIds = new List<Guid> { vipSeatId, normalSeatId, coupleSeatId },
            FoodItems = new List<FoodOrderItem>()
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            MovieId = Guid.NewGuid()
        };

        var vipSeat = new Seat
        {
            Id = vipSeatId,
            Row = "V",
            Number = 1,
            Type = SeatType.VIP
        };

        var normalSeat = new Seat
        {
            Id = normalSeatId,
            Row = "A",
            Number = 1,
            Type = SeatType.Normal
        };

        var coupleSeat = new Seat
        {
            Id = coupleSeatId,
            Row = "C",
            Number = 1,
            Type = SeatType.Couple
        };

        var seats = new List<Seat> { vipSeat, normalSeat, coupleSeat };

        // Total: VIP (120000) + Normal (80000) + Couple (200000) = 400000
        var createdBooking = new Booking
        {
            Id = Guid.NewGuid(),
            MemberId = userId,
            ShowtimeId = showTimeId,
            BookingDate = DateTime.UtcNow,
            Status = "Created",
            TotalAmount = 400000,
            BookingSeats = seats.Select(seat => new BookingSeat
            {
                SeatId = seat.Id,
                Seat = seat
            }).ToList(),
            BookingFoods = new List<BookingFood>()
        };

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(showTime);

        _mockShowTimeSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, object>>[]>()))
            .ReturnsAsync(new List<ShowTimeSeat>());

        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(seats);

        _mockFoodAndDrinkRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()))
            .ReturnsAsync(new List<FoodAndDrink>());

        _mockBookingRepository.Setup(r => r.AddAsync(It.IsAny<Booking>()))
            .ReturnsAsync(createdBooking);

        _mockBookingRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(createdBooking);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _bookingService.CreateBookingAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400000, result.TotalAmount); // VIP + Normal + Couple = 120000 + 80000 + 200000
        Assert.Equal(3, result.BookingSeats.Count);

        _mockLoggerService.Verify(
            l => l.Success(It.Is<string>(msg => msg.Contains("Booking created successfully with ID:"))),
            Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_ValidRequest_ReturnsBookingDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var foodId = Guid.NewGuid();

        var request = new CreateBookingRequest
        {
            ShowTimeId = showTimeId,
            SeatIds = new List<Guid> { seatId },
            FoodItems = new List<FoodOrderItem>
            {
                new() { FoodId = foodId, Quantity = 1 }
            }
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            MovieId = Guid.NewGuid()
        };

        var seat = new Seat
        {
            Id = seatId,
            Row = "A",
            Number = 1,
            Type = SeatType.Normal
        };

        var food = new FoodAndDrink
        {
            Id = foodId,
            Name = "Popcorn",
            Price = 15000
        };

        var createdBooking = new Booking
        {
            Id = Guid.NewGuid(),
            MemberId = userId,
            ShowtimeId = showTimeId,
            BookingDate = DateTime.UtcNow,
            Status = "Created",
            TotalAmount = 95000,
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = seatId, Seat = seat }
            },
            BookingFoods = new List<BookingFood>
            {
                new() { FoodAndDrinkId = foodId, Quantity = 1, FoodAndDrink = food }
            }
        };

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(showTime);

        _mockShowTimeSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, object>>[]>()))
            .ReturnsAsync(new List<ShowTimeSeat>());

        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat> { seat });

        _mockFoodAndDrinkRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()))
            .ReturnsAsync(new List<FoodAndDrink> { food });

        _mockBookingRepository.Setup(r => r.AddAsync(It.IsAny<Booking>()))
            .ReturnsAsync(createdBooking);

        _mockBookingRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(createdBooking);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _bookingService.CreateBookingAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(showTimeId, result.ShowTimeId);
        Assert.Single(result.BookingSeats);
        Assert.Single(result.BookingFoods);

        _mockLoggerService.Verify(
            l => l.Info($"Starting booking creation for user: {userId}, showtime: {showTimeId}"),
            Times.Once);

        _mockLoggerService.Verify(
            l => l.Success(It.Is<string>(msg => msg.Contains("Booking created successfully with ID:"))),
            Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_ExistingSeatFound_UpdatesExistingSeatStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var foodId = Guid.NewGuid();

        var request = new CreateBookingRequest
        {
            ShowTimeId = showTimeId,
            SeatIds = new List<Guid> { seatId },
            FoodItems = new List<FoodOrderItem>
            {
                new() { FoodId = foodId, Quantity = 1 }
            }
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            MovieId = Guid.NewGuid()
        };

        var seat = new Seat
        {
            Id = seatId,
            Row = "A",
            Number = 1,
            Type = SeatType.Normal
        };

        var food = new FoodAndDrink
        {
            Id = foodId,
            Name = "Popcorn",
            Price = 15000
        };

        // Key part - existing ShowTimeSeat with Available status
        var existingShowTimeSeat = new ShowTimeSeat
        {
            ShowTimeId = showTimeId,
            SeatId = seatId,
            Status = SeatStatus.Available,
            Seat = seat // This is important for the condition check
        };

        var createdBooking = new Booking
        {
            Id = Guid.NewGuid(),
            MemberId = userId,
            ShowtimeId = showTimeId,
            BookingDate = DateTime.UtcNow,
            Status = "Created",
            TotalAmount = 95000,
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = seatId, Seat = seat }
            },
            BookingFoods = new List<BookingFood>
            {
                new() { FoodAndDrinkId = foodId, Quantity = 1, FoodAndDrink = food }
            }
        };

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(showTime);

        // Setup to return the existing ShowTimeSeat with Available status
        _mockShowTimeSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, object>>[]>()))
            .ReturnsAsync(new List<ShowTimeSeat> { existingShowTimeSeat });

        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat> { seat });

        _mockFoodAndDrinkRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()))
            .ReturnsAsync(new List<FoodAndDrink> { food });

        _mockBookingRepository.Setup(r => r.AddAsync(It.IsAny<Booking>()))
            .ReturnsAsync(createdBooking);

        _mockBookingRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(createdBooking);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _bookingService.CreateBookingAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(showTimeId, result.ShowTimeId);
        Assert.Single(result.BookingSeats);
        Assert.Single(result.BookingFoods);

        // Verify that the existing seat status was updated to Booked
        Assert.Equal(SeatStatus.Booked, existingShowTimeSeat.Status);

        // Verify that Update was called on the existing seat (not AddAsync)
        _mockShowTimeSeatRepository.Verify(r => r.Update(existingShowTimeSeat), Times.Once);

        // Verify that AddAsync was NOT called for ShowTimeSeats (since we're updating existing)
        _mockShowTimeSeatRepository.Verify(r => r.AddAsync(It.IsAny<ShowTimeSeat>()), Times.Never);

        _mockLoggerService.Verify(
            l => l.Info($"Starting booking creation for user: {userId}, showtime: {showTimeId}"),
            Times.Once);

        _mockLoggerService.Verify(
            l => l.Success(It.Is<string>(msg => msg.Contains("Booking created successfully with ID:"))),
            Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_EmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateBookingRequest
        {
            ShowTimeId = Guid.NewGuid(),
            SeatIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _bookingService.CreateBookingAsync(Guid.Empty, request));

        Assert.Equal("Invalid user ID.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn("Attempted to create booking with an empty user GUID."),
            Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_InvalidShowTime_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateBookingRequest
        {
            ShowTimeId = Guid.NewGuid(),
            SeatIds = new List<Guid> { Guid.NewGuid() }
        };

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(request.ShowTimeId,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync((ShowTime)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _bookingService.CreateBookingAsync(userId, request));

        Assert.Equal("Invalid showtime", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"Invalid showtime ID: {request.ShowTimeId}"),
            Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_SeatsNotAvailable_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var seatId = Guid.NewGuid();

        var request = new CreateBookingRequest
        {
            ShowTimeId = showTimeId,
            SeatIds = new List<Guid> { seatId }
        };

        var showTime = new ShowTime { Id = showTimeId };
        var bookedSeat = new ShowTimeSeat
        {
            ShowTimeId = showTimeId,
            SeatId = seatId,
            Status = SeatStatus.Booked
        };

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(showTime);

        _mockShowTimeSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, object>>[]>()))
            .ReturnsAsync(new List<ShowTimeSeat> { bookedSeat });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _bookingService.CreateBookingAsync(userId, request));

        Assert.Equal("One or more selected seats are not available", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"Attempted to book unavailable seats for showtime: {showTimeId}"),
            Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_ValidBookingId_ReturnsTrueAndCancelsBooking()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();

        var booking = new Booking
        {
            Id = bookingId,
            ShowtimeId = showTimeId,
            MemberId = Guid.NewGuid(),
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = seatId }
            }
        };

        var showTimeSeat = new ShowTimeSeat
        {
            ShowTimeId = showTimeId,
            SeatId = seatId,
            Status = SeatStatus.Booked
        };

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        _mockShowTimeSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, object>>[]>()))
            .ReturnsAsync(new List<ShowTimeSeat> { showTimeSeat });

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _bookingService.CancelBookingAsync(bookingId);

        // Assert
        Assert.True(result);

        _mockLoggerService.Verify(
            l => l.Info($"Starting booking cancellation for ID: {bookingId}"),
            Times.Once);

        _mockLoggerService.Verify(
            l => l.Success($"Booking {bookingId} cancelled successfully"),
            Times.Once);

        _mockBookingRepository.Verify(r => r.SoftRemove(booking), Times.Once);
        _mockShowTimeSeatRepository.Verify(r => r.UpdateRange(It.IsAny<List<ShowTimeSeat>>()), Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_DatabaseError_LogsErrorAndThrowsException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var exceptionMessage = "Database connection error";

        var booking = new Booking
        {
            Id = bookingId,
            ShowtimeId = showTimeId,
            MemberId = Guid.NewGuid(),
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = seatId }
            }
        };

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        // Setup ShowTimeSeats repository to throw an exception
        _mockShowTimeSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, object>>[]>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _bookingService.CancelBookingAsync(bookingId));

        Assert.Equal(exceptionMessage, ex.Message);

        // Verify that the error was logged with the correct message
        _mockLoggerService.Verify(
            l => l.Error($"Error cancelling booking {bookingId}: {exceptionMessage}"),
            Times.Once);

        // Verify that the info log was called before the exception
        _mockLoggerService.Verify(
            l => l.Info($"Starting booking cancellation for ID: {bookingId}"),
            Times.Once);

        // Verify that SaveChangesAsync was never called due to the exception
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelBookingAsync_EmptyBookingId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _bookingService.CancelBookingAsync(Guid.Empty));

        Assert.Equal("Invalid booking ID.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn("Attempted to cancel booking with an empty GUID."),
            Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_BookingNotFound_ReturnsFalse()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync((Booking)null!);

        // Act
        var result = await _bookingService.CancelBookingAsync(bookingId);

        // Assert
        Assert.False(result);

        _mockLoggerService.Verify(
            l => l.Warn($"No booking found with ID: {bookingId} or booking is already deleted"),
            Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_DeletedBooking_ReturnsFalse()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var deletedBooking = new Booking
        {
            Id = bookingId,
            IsDeleted = true
        };

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(deletedBooking);

        // Act
        var result = await _bookingService.CancelBookingAsync(bookingId);

        // Assert
        Assert.False(result);

        _mockLoggerService.Verify(
            l => l.Warn($"No booking found with ID: {bookingId} or booking is already deleted"),
            Times.Once);
    }

    private Booking CreateTestBooking()
    {
        return new Booking
        {
            Id = Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
            ShowtimeId = Guid.NewGuid(),
            BookingDate = DateTime.UtcNow,
            TotalAmount = 80000,
            Status = "Created",
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = Guid.NewGuid() }
            },
            BookingFoods = new List<BookingFood>()
        };
    }

    private void SetupBookingDetailsMocks()
    {
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            FullName = "Test User"
        };

        var movie = new Movie
        {
            Id = movieId,
            Name = "Test Movie"
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            MovieId = movieId
        };

        var booking = new Booking
        {
            Id = bookingId,
            MemberId = userId,
            Member = user,
            ShowtimeId = showTimeId,
            Showtime = showTime,
            BookingDate = DateTime.UtcNow,
            TotalAmount = 80000,
            Status = "Created",
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = Guid.NewGuid() }
            },
            BookingFoods = new List<BookingFood>()
        };

        _mockBookingRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        _mockMovieRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Movie, object>>[]>()))
            .ReturnsAsync(movie);

        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat>
            {
                new() { Id = Guid.NewGuid(), Row = "A", Number = 1 }
            });

        _mockFoodAndDrinkRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()))
            .ReturnsAsync(new List<FoodAndDrink>());
    }

}