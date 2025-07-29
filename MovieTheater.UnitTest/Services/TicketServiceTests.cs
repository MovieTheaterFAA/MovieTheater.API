using Moq;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.DTOs.TicketDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.UnitTest.Services;

public class TicketServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILoggerService> _mockLoggerService;
    private readonly Mock<IScoreService> _mockScoreService;
    private readonly Mock<IGenericRepository<Ticket>> _mockTicketRepository;
    private readonly Mock<IGenericRepository<Booking>> _mockBookingRepository;
    private readonly Mock<IGenericRepository<User>> _mockUserRepository;
    private readonly Mock<IGenericRepository<ShowTime>> _mockShowTimeRepository;
    private readonly Mock<IGenericRepository<Seat>> _mockSeatRepository;
    private readonly Mock<IGenericRepository<ShowTimeSeat>> _mockShowTimeSeatRepository;
    private readonly Mock<IGenericRepository<FoodAndDrink>> _mockFoodAndDrinkRepository;
    private readonly TicketService _ticketService;

    public TicketServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLoggerService = new Mock<ILoggerService>();
        _mockScoreService = new Mock<IScoreService>();
        _mockTicketRepository = new Mock<IGenericRepository<Ticket>>();
        _mockBookingRepository = new Mock<IGenericRepository<Booking>>();
        _mockUserRepository = new Mock<IGenericRepository<User>>();
        _mockShowTimeRepository = new Mock<IGenericRepository<ShowTime>>();
        _mockSeatRepository = new Mock<IGenericRepository<Seat>>();
        _mockShowTimeSeatRepository = new Mock<IGenericRepository<ShowTimeSeat>>();
        _mockFoodAndDrinkRepository = new Mock<IGenericRepository<FoodAndDrink>>();

        _mockUnitOfWork.Setup(u => u.Tickets).Returns(_mockTicketRepository.Object);
        _mockUnitOfWork.Setup(u => u.Bookings).Returns(_mockBookingRepository.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.ShowTimes).Returns(_mockShowTimeRepository.Object);
        _mockUnitOfWork.Setup(u => u.Seats).Returns(_mockSeatRepository.Object);
        _mockUnitOfWork.Setup(u => u.ShowTimeSeats).Returns(_mockShowTimeSeatRepository.Object);
        _mockUnitOfWork.Setup(u => u.FoodAndDrinks).Returns(_mockFoodAndDrinkRepository.Object);

        _ticketService = new TicketService(
            _mockUnitOfWork.Object,
            _mockLoggerService.Object,
            _mockScoreService.Object
        );
    }

    #region GenerateTicketFromBookingAsync Tests

    [Fact]
    public async Task GenerateTicketFromBookingAsync_ValidBooking_ReturnsTicketResponse()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var ticketId = Guid.NewGuid(); // Add ticket ID

        var user = new User
        {
            Id = userId,
            PhoneNumber = "+1234567890"
        };

        var movie = new Movie
        {
            Id = movieId,
            Name = "Test Movie",
            PosterImage = "poster.jpg"
        };

        var cinemaRoom = new CinemaRoom
        {
            Id = cinemaRoomId,
            Name = "Room 1"
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            MovieId = movieId,
            Movie = movie,
            CinemaRoomId = cinemaRoomId,
            CinemaRoom = cinemaRoom,
            ShowDate = DateTime.UtcNow.AddDays(1)
        };

        var seat = new Seat
        {
            Id = seatId,
            Row = "A",
            Number = 1,
            Type = SeatType.Normal
        };

        var booking = new Booking
        {
            Id = bookingId,
            MemberId = userId,
            Member = user,
            ShowtimeId = showTimeId,
            Showtime = showTime,
            BookingSeats = new List<BookingSeat>
            {
                new() { SeatId = seatId, BookingId = bookingId }
            },
            BookingFoods = new List<BookingFood>(),
            Invoice = new Invoice { Amount = 80000 }
        };

        // Create the ticket that will be returned by AddAsync and GetByIdAsync
        var createdTicket = new Ticket
        {
            Id = ticketId,
            BookingId = bookingId,
            IssuedAt = DateTime.UtcNow,
            GuestPhoneNumber = user.PhoneNumber,
            TotalPrice = 80000,
            TicketType = TicketType.Online,
            TicketSeats = new List<TicketSeat>
            {
                new() { SeatId = seatId, PricePerSeat = 80000 }
            },
            TicketFoodAndDrinks = new List<TicketFoodAndDrink>(),
            Showtime = showTime
        };

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        _mockTicketRepository.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync((Ticket)null!);

        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat> { seat });

        // Mock AddAsync to set the ticket ID and return the created ticket
        _mockTicketRepository.Setup(r => r.AddAsync(It.IsAny<Ticket>()))
            .Callback<Ticket>(t => t.Id = ticketId)
            .ReturnsAsync(createdTicket);

        // Mock GetByIdAsync to return the created ticket for GetTicketDetailsAsync
        _mockTicketRepository.Setup(r => r.GetByIdAsync(ticketId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(createdTicket);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(showTime);

        // Mock FoodAndDrinks repository for GetTicketDetailsAsync
        _mockFoodAndDrinkRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()))
            .ReturnsAsync(new List<FoodAndDrink>());

        // Act
        var result = await _ticketService.GenerateTicketFromBookingAsync(bookingId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.PhoneNumber, result.GuestPhoneNumber);
        Assert.Equal(80000, result.TotalPrice);
        Assert.Equal("Online", result.TicketType);

        _mockLoggerService.Verify(
            l => l.Success($"Ticket generated successfully for booking ID: {bookingId}"),
            Times.Once);
    }

    [Fact]
    public async Task GenerateTicketFromBookingAsync_EmptyBookingId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _ticketService.GenerateTicketFromBookingAsync(Guid.Empty));

        Assert.Equal("Invalid booking ID.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn("Attempted to generate ticket with an empty booking GUID."),
            Times.Once);
    }

    [Fact]
    public async Task GenerateTicketFromBookingAsync_BookingNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync((Booking)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _ticketService.GenerateTicketFromBookingAsync(bookingId));

        Assert.Contains($"Booking with ID {bookingId} not found", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"No booking found with ID: {bookingId} or booking is already deleted"),
            Times.Once);
    }

    [Fact]
    public async Task GenerateTicketFromBookingAsync_TicketAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var booking = new Booking { Id = bookingId };
        var existingTicket = new Ticket { Id = Guid.NewGuid(), BookingId = bookingId };

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        _mockTicketRepository.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(existingTicket);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _ticketService.GenerateTicketFromBookingAsync(bookingId));

        Assert.Equal("Ticket already exists for this booking.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"Ticket already exists for booking ID: {bookingId}"),
            Times.Once);
    }

    [Fact]
    public async Task GenerateTicketFromBookingAsync_NoBookingSeats_ThrowsInvalidOperationException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var booking = new Booking
        {
            Id = bookingId,
            BookingSeats = null,
            Member = new User { PhoneNumber = "+1234567890" },
            Showtime = new ShowTime(),
            Invoice = new Invoice { Amount = 100 }
        };

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        _mockTicketRepository.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync((Ticket)null!);

        // Setup the Seats repository to return an empty list when queried with empty BookingSeats
        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat>());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _ticketService.GenerateTicketFromBookingAsync(bookingId));

        Assert.Equal("No seats associated with this booking.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"No seats found for booking ID: {bookingId}"),
            Times.Once);
    }

    #endregion

    #region CreateOfflineTicketAsync Tests

    [Fact]
    public async Task CreateOfflineTicketAsync_ValidRequest_ReturnsTicketResponse()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();

        var request = new CreateOfflineTicketRequest
        {
            GuestPhoneNumber = "+1234567890",
            ShowtimeId = showTimeId,
            SeatIds = new List<Guid> { seatId },
            FoodItems = new List<FoodItemRequest>()
        };

        var movie = new Movie
        {
            Id = movieId,
            Name = "Test Movie",
            PosterImage = "poster.jpg"
        };

        var cinemaRoom = new CinemaRoom
        {
            Id = cinemaRoomId,
            Name = "Room 1"
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            Movie = movie,
            CinemaRoom = cinemaRoom,
            ShowDate = DateTime.UtcNow.AddDays(1)
        };

        var seat = new Seat
        {
            Id = seatId,
            Row = "A",
            Number = 1,
            Type = SeatType.Normal
        };

        var createdTicket = new Ticket
        {
            Id = ticketId,
            IssuedAt = DateTime.UtcNow,
            GuestPhoneNumber = "+1234567890",
            TotalPrice = 80000,
            TicketType = TicketType.Offline,
            TicketSeats = new List<TicketSeat>
        {
            new() { SeatId = seatId, PricePerSeat = 80000 }
        },
            TicketFoodAndDrinks = new List<TicketFoodAndDrink>(),
            Showtime = showTime
        };

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(showTime);

        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat> { seat });

        _mockShowTimeSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, object>>[]>()))
            .ReturnsAsync(new List<ShowTimeSeat>());

        // Mock AddAsync to set the ticket ID when called
        _mockTicketRepository.Setup(r => r.AddAsync(It.IsAny<Ticket>()))
            .Callback<Ticket>(t => t.Id = ticketId)
            .ReturnsAsync(createdTicket);

        // Setup the GetByIdAsync for the created ticket with the specific ticket ID
        _mockTicketRepository.Setup(r => r.GetByIdAsync(ticketId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(createdTicket);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _ticketService.CreateOfflineTicketAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.GuestPhoneNumber, result.GuestPhoneNumber);
        Assert.Equal("Offline", result.TicketType);

        _mockLoggerService.Verify(
            l => l.Info($"Creating offline ticket for guest phone number: {request.GuestPhoneNumber}, Showtime ID: {request.ShowtimeId}"),
            Times.Once);
    }

    [Fact]
    public async Task CreateOfflineTicketAsync_EmptyPhoneNumber_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateOfflineTicketRequest
        {
            GuestPhoneNumber = "",
            ShowtimeId = Guid.NewGuid(),
            SeatIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _ticketService.CreateOfflineTicketAsync(request));

        Assert.Equal("Failed to create offline ticket", ex.Message);
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    [Fact]
    public async Task CreateOfflineTicketAsync_InvalidPhoneFormat_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateOfflineTicketRequest
        {
            GuestPhoneNumber = "invalid-phone",
            ShowtimeId = Guid.NewGuid(),
            SeatIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _ticketService.CreateOfflineTicketAsync(request));

        Assert.Equal("Failed to create offline ticket", ex.Message);
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    [Fact]
    public async Task CreateOfflineTicketAsync_ShowTimeNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var request = new CreateOfflineTicketRequest
        {
            GuestPhoneNumber = "+1234567890",
            ShowtimeId = Guid.NewGuid(),
            SeatIds = new List<Guid> { Guid.NewGuid() }
        };

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(request.ShowtimeId,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync((ShowTime)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _ticketService.CreateOfflineTicketAsync(request));

        Assert.Equal("Failed to create offline ticket", ex.Message);
        Assert.IsType<KeyNotFoundException>(ex.InnerException);
    }

    [Fact]
    public async Task CreateOfflineTicketAsync_SeatNotAvailable_ThrowsInvalidOperationException()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var seatId = Guid.NewGuid();

        var request = new CreateOfflineTicketRequest
        {
            GuestPhoneNumber = "+1234567890",
            ShowtimeId = showTimeId,
            SeatIds = new List<Guid> { seatId }
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            Movie = new Movie { Name = "Test Movie" },
            CinemaRoom = new CinemaRoom { Name = "Room 1" }
        };

        var seat = new Seat { Id = seatId };
        var bookedShowTimeSeat = new ShowTimeSeat
        {
            SeatId = seatId,
            ShowTimeId = showTimeId,
            Status = SeatStatus.Booked
        };

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(showTime);

        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat> { seat });

        _mockShowTimeSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, object>>[]>()))
            .ReturnsAsync(new List<ShowTimeSeat> { bookedShowTimeSeat });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _ticketService.CreateOfflineTicketAsync(request));

        Assert.Equal("Failed to create offline ticket", ex.Message);
        Assert.Contains("One or more selected seats are not available", ex.InnerException?.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"Attempted to book unavailable seats for showtime: {request.ShowtimeId}"),
            Times.Once);
    }

    #endregion

    #region GetAllTicketsAsync Tests

    [Fact]
    public async Task GetAllTicketsAsync_ValidParameters_ReturnsPaginatedResults()
    {
        // Arrange
        var tickets = new List<Ticket>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Online,
                IssuedAt = DateTime.UtcNow,
                TotalPrice = 100,
                GuestPhoneNumber = "+1234567890",
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            },
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Offline,
                IssuedAt = DateTime.UtcNow.AddDays(-1),
                TotalPrice = 150,
                GuestPhoneNumber = "+0987654321",
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            }
        };

        _mockTicketRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(tickets);

        // Mock GetTicketDetailsAsync dependencies
        SetupGetTicketDetailsMocks();

        // Act
        var result = await _ticketService.GetAllTicketsAsync(page: 1, pageSize: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(10, result.PageSize);

        _mockLoggerService.Verify(
            l => l.Info($"Fetching tickets - Page 1, PageSize 10, TicketType: , Search: "),
            Times.Once);

        _mockLoggerService.Verify(
            l => l.Success(It.Is<string>(msg => msg.Contains("Retrieved") && msg.Contains("tickets on page 1 successfully"))),
            Times.Once);
    }

    [Fact]
    public async Task GetAllTicketsAsync_WithTicketTypeFilter_ReturnsFilteredResults()
    {
        // Arrange
        var tickets = new List<Ticket>
        {
            new() { Id = Guid.NewGuid(), TicketType = TicketType.Online, Showtime = new ShowTime() },
            new() { Id = Guid.NewGuid(), TicketType = TicketType.Offline, Showtime = new ShowTime() }
        };

        _mockTicketRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(tickets);

        SetupGetTicketDetailsMocks();

        // Act
        var result = await _ticketService.GetAllTicketsAsync(ticketType: TicketType.Online);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetAllTicketsAsync_DatabaseError_ThrowsException()
    {
        // Arrange
        _mockTicketRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _ticketService.GetAllTicketsAsync());

        Assert.Equal("An error occurred while retrieving ticket items. Please try again later.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Error(It.Is<string>(msg => msg.Contains("Failed to retrieve tickets"))),
            Times.Once);
    }

    #endregion

    #region GetTicketByIdAsync Tests

    [Fact]
    public async Task GetTicketByIdAsync_ValidTicketId_ReturnsTicketResponse()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        SetupGetTicketDetailsMocks();

        // Act
        var result = await _ticketService.GetTicketByIdAsync(ticketId);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetTicketByIdAsync_EmptyTicketId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _ticketService.GetTicketByIdAsync(Guid.Empty));

        Assert.Equal("Invalid ticket ID.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn("Attempted to fetch ticket with an empty GUID."),
            Times.Once);
    }

    [Fact]
    public async Task GetTicketByIdAsync_TicketNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        _mockTicketRepository.Setup(r => r.GetByIdAsync(ticketId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync((Ticket)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _ticketService.GetTicketByIdAsync(ticketId));

        Assert.Contains($"Ticket with ID {ticketId} not found", ex.Message);

        _mockLoggerService.Verify(
            l => l.Error(It.Is<string>(msg => msg.Contains($"Error fetching ticket {ticketId}"))),
            Times.Once);
    }

    #endregion

    #region GetUserTicketsAsync Tests

    [Fact]
    public async Task GetUserTicketsAsync_ValidUserId_ReturnsUserTickets()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            PhoneNumber = "+1234567890"
        };

        var tickets = new List<Ticket>
        {
            new()
            {
                Id = Guid.NewGuid(),
                GuestPhoneNumber = "+1234567890",
                Showtime = new ShowTime()
            }
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId,
            It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync(user);

        _mockTicketRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(tickets);

        SetupGetTicketDetailsMocks();

        // Act
        var result = await _ticketService.GetUserTicketsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);

        _mockLoggerService.Verify(
            l => l.Success($"Successfully retrieved 1 tickets for user ID: {userId}"),
            Times.Once);
    }

    [Fact]
    public async Task GetUserTicketsAsync_EmptyUserId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _ticketService.GetUserTicketsAsync(Guid.Empty));

        Assert.Equal("Invalid user ID.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn("Attempted to fetch tickets with an empty user GUID."),
            Times.Once);
    }

    [Fact]
    public async Task GetUserTicketsAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId,
            It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync((User)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _ticketService.GetUserTicketsAsync(userId));

        Assert.Contains($"User with ID {userId} not found", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"No user found with ID: {userId}"),
            Times.Once);
    }

    [Fact]
    public async Task GetUserTicketsAsync_UserNoPhoneNumber_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            PhoneNumber = string.Empty
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId,
            It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync(user);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _ticketService.GetUserTicketsAsync(userId));

        Assert.Equal("User phone number is required to fetch tickets.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"User with ID {userId} has no phone number associated."),
            Times.Once);
    }

    [Fact]
    public async Task GetUserTicketsAsync_NoTicketsFound_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            PhoneNumber = "+1234567890"
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId,
            It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync(user);

        _mockTicketRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(new List<Ticket>());

        // Act
        var result = await _ticketService.GetUserTicketsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        _mockLoggerService.Verify(
            l => l.Info($"No tickets found for user ID: {userId}"),
            Times.Once);
    }

    #endregion

    #region GenerateTicketQRCodeAsync Tests

    [Fact]
    public async Task GenerateTicketQRCodeAsync_ValidTicketId_ReturnsQRCodeString()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        SetupGetTicketDetailsMocks();

        // Act
        var result = await _ticketService.GenerateTicketQRCodeAsync(ticketId);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("data:image/png;base64,", result);

        _mockLoggerService.Verify(
            l => l.Success($"QR code generated successfully for ticket ID: {ticketId}"),
            Times.Once);
    }

    [Fact]
    public async Task GenerateTicketQRCodeAsync_EmptyTicketId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _ticketService.GenerateTicketQRCodeAsync(Guid.Empty));

        Assert.Equal("Invalid ticket ID (Parameter 'ticketId')", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn("Attempted to generate QR code with empty ticket ID"),
            Times.Once);
    }

    [Fact]
    public async Task GenerateTicketQRCodeAsync_TicketNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        _mockTicketRepository.Setup(r => r.GetByIdAsync(ticketId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync((Ticket)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _ticketService.GenerateTicketQRCodeAsync(ticketId));

        Assert.Contains($"Ticket with ID {ticketId} not found", ex.Message);

        _mockLoggerService.Verify(
            l => l.Error(It.Is<string>(msg => msg.Contains($"Error generating QR code for ticket {ticketId}"))),
            Times.Once);
    }

    #endregion

    #region VerifyTicketQRCodeAsync Tests

    [Fact]
    public async Task VerifyTicketQRCodeAsync_ValidQRCode_ReturnsValidResult()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var issuedAt = DateTime.UtcNow;
        var totalPrice = 80000m;

        // Set up the mock to return a specific ticket with known values
        var ticket = new Ticket
        {
            Id = ticketId,
            IssuedAt = issuedAt,
            GuestPhoneNumber = "+1234567890",
            TotalPrice = totalPrice,
            TicketType = TicketType.Online,
            TicketSeats = new List<TicketSeat>
            {
                new() { SeatId = Guid.NewGuid(), PricePerSeat = 80000 }
            },
            TicketFoodAndDrinks = new List<TicketFoodAndDrink>(),
            Showtime = new ShowTime { Id = Guid.NewGuid() }
        };

        var seat = new Seat
        {
            Id = ticket.TicketSeats.First().SeatId,
            Row = "A",
            Number = 1,
            Type = SeatType.Normal
        };

        var movie = new Movie
        {
            Id = Guid.NewGuid(),
            Name = "Test Movie",
            PosterImage = "poster.jpg"
        };

        var cinemaRoom = new CinemaRoom
        {
            Id = Guid.NewGuid(),
            Name = "Room 1"
        };

        var showTime = new ShowTime
        {
            Id = ticket.Showtime.Id,
            MovieId = movie.Id,
            Movie = movie,
            CinemaRoomId = cinemaRoom.Id,
            CinemaRoom = cinemaRoom,
            ShowDate = DateTime.UtcNow.AddDays(1)
        };

        // Set up mocks with the specific ticket data
        _mockTicketRepository.Setup(r => r.GetByIdAsync(ticketId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(ticket);

        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat> { seat });

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(ticket.Showtime.Id,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(showTime);

        _mockFoodAndDrinkRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()))
            .ReturnsAsync(new List<FoodAndDrink>());

        // Generate the expected hash using the same logic as the service
        var dataToHash = $"{ticketId}|{issuedAt:yyyy-MM-dd-HH-mm-ss}|{totalPrice}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes("MovieTheater_QRCode_SecretKey_2024_VeryLongAndRandomString_ForHMACValidation_ShouldBe256BitsMinimum_NeverShareThis"));
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(dataToHash);
        byte[] hash = hmac.ComputeHash(bytes);
        string expectedHash = Convert.ToBase64String(hash);

        var qrCodeData = new QrCodePayload
        {
            TicketId = ticketId,
            Hash = expectedHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        // Act
        var result = await _ticketService.VerifyTicketQRCodeAsync(qrCodeData);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal("Ticket verified successfully", result.Message);

        _mockLoggerService.Verify(
            l => l.Success($"Ticket {ticketId} verified successfully"),
            Times.Once);
    }

    [Fact]
    public async Task VerifyTicketQRCodeAsync_NullQRCodeData_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _ticketService.VerifyTicketQRCodeAsync(null!));

        Assert.Equal("QR code verification failed due to system error", ex.Message);
        Assert.IsType<ArgumentException>(ex.InnerException);

        _mockLoggerService.Verify(
            l => l.Warn("Invalid QR code format or missing ticket ID"),
            Times.Once);
    }

    [Fact]
    public async Task VerifyTicketQRCodeAsync_ExpiredQRCode_ThrowsInvalidOperationException()
    {
        // Arrange
        var qrCodeData = new QrCodePayload
        {
            TicketId = Guid.NewGuid(),
            Hash = "valid-hash",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5) // Expired
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _ticketService.VerifyTicketQRCodeAsync(qrCodeData));

        Assert.Equal("QR code verification failed due to system error", ex.Message);
        Assert.Contains("QR code has expired", ex.InnerException?.Message);
    }

    [Fact]
    public async Task VerifyTicketQRCodeAsync_MissingHash_ThrowsArgumentException()
    {
        // Arrange
        var qrCodeData = new QrCodePayload
        {
            TicketId = Guid.NewGuid(),
            Hash = "", // Missing hash
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _ticketService.VerifyTicketQRCodeAsync(qrCodeData));

        Assert.Equal("QR code verification failed due to system error", ex.Message);
        Assert.IsType<ArgumentException>(ex.InnerException);

        _mockLoggerService.Verify(
            l => l.Warn($"Missing hash in QR code for ticket {qrCodeData.TicketId}"),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupGetTicketDetailsMocks()
    {
        var ticketId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();

        var ticket = new Ticket
        {
            Id = ticketId,
            IssuedAt = DateTime.UtcNow,
            GuestPhoneNumber = "+1234567890",
            TotalPrice = 80000,
            TicketType = TicketType.Online,
            TicketSeats = new List<TicketSeat>
            {
                new() { SeatId = seatId, PricePerSeat = 80000 }
            },
            TicketFoodAndDrinks = new List<TicketFoodAndDrink>(),
            Showtime = new ShowTime { Id = showTimeId }
        };

        var seat = new Seat
        {
            Id = seatId,
            Row = "A",
            Number = 1,
            Type = SeatType.Normal
        };

        var movie = new Movie
        {
            Id = movieId,
            Name = "Test Movie",
            PosterImage = "poster.jpg"
        };

        var cinemaRoom = new CinemaRoom
        {
            Id = cinemaRoomId,
            Name = "Room 1"
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            MovieId = movieId,
            Movie = movie,
            CinemaRoomId = cinemaRoomId,
            CinemaRoom = cinemaRoom,
            ShowDate = DateTime.UtcNow.AddDays(1)
        };

        _mockTicketRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(ticket);

        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat> { seat });

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(showTime);

        _mockFoodAndDrinkRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()))
            .ReturnsAsync(new List<FoodAndDrink>());
    }

    #endregion
}