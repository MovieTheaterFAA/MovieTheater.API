using Moq;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.DTOs.TicketDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System.Text.Json;

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
    public async Task GenerateTicketFromBookingAsync_ValidBookingWithFoodItems_ReturnsTicketResponseWithFoodItems()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var foodAndDrinkId1 = Guid.NewGuid();
        var foodAndDrinkId2 = Guid.NewGuid();

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

        // Create food and drink items
        var foodAndDrink1 = new FoodAndDrink
        {
            Id = foodAndDrinkId1,
            Name = "Popcorn",
            Price = 15000,
            Type = FoodType.Food
        };

        var foodAndDrink2 = new FoodAndDrink
        {
            Id = foodAndDrinkId2,
            Name = "Coca Cola",
            Price = 10000,
            Type = FoodType.Drink
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
            BookingFoods = new List<BookingFood>
            {
                new() { FoodAndDrinkId = foodAndDrinkId1, BookingId = bookingId, Quantity = 2 },
                new() { FoodAndDrinkId = foodAndDrinkId2, BookingId = bookingId, Quantity = 1 }
            },
            Invoice = new Invoice { Amount = 115000 } // 80000 (seat) + 30000 (2x popcorn) + 10000 (1x drink) - 5000 (discount)
        };

        // Create the ticket that will be returned by AddAsync and GetByIdAsync
        var createdTicket = new Ticket
        {
            Id = ticketId,
            BookingId = bookingId,
            IssuedAt = DateTime.UtcNow,
            GuestPhoneNumber = user.PhoneNumber,
            TotalPrice = 115000,
            TicketType = TicketType.Online,
            TicketSeats = new List<TicketSeat>
            {
                new() { SeatId = seatId, PricePerSeat = 80000 }
            },
            TicketFoodAndDrinks = new List<TicketFoodAndDrink>
            {
                new() { FoodAndDrinkId = foodAndDrinkId1, Quantity = 2 },
                new() { FoodAndDrinkId = foodAndDrinkId2, Quantity = 1 }
            },
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
            .ReturnsAsync(new List<FoodAndDrink> { foodAndDrink1, foodAndDrink2 });

        // Act
        var result = await _ticketService.GenerateTicketFromBookingAsync(bookingId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.PhoneNumber, result.GuestPhoneNumber);
        Assert.Equal(115000, result.TotalPrice);
        Assert.Equal("Online", result.TicketType);

        // Verify food items are included in the response
        Assert.NotNull(result.FoodItems);
        Assert.Equal(2, result.FoodItems.Count);

        var popcornItem = result.FoodItems.FirstOrDefault(f => f.FoodId == foodAndDrinkId1);
        Assert.NotNull(popcornItem);
        Assert.Equal("Popcorn", popcornItem.Name);
        Assert.Equal(2, popcornItem.Quantity);
        Assert.Equal(15000, popcornItem.Price);

        var drinkItem = result.FoodItems.FirstOrDefault(f => f.FoodId == foodAndDrinkId2);
        Assert.NotNull(drinkItem);
        Assert.Equal("Coca Cola", drinkItem.Name);
        Assert.Equal(1, drinkItem.Quantity);
        Assert.Equal(10000, drinkItem.Price);

        _mockLoggerService.Verify(
            l => l.Success($"Ticket generated successfully for booking ID: {bookingId}"),
            Times.Once);

        // Verify that AddAsync was called with a ticket containing food items
        _mockTicketRepository.Verify(r => r.AddAsync(It.Is<Ticket>(t =>
            t.TicketFoodAndDrinks != null &&
            t.TicketFoodAndDrinks.Count == 2 &&
            t.TicketFoodAndDrinks.Any(tf => tf.FoodAndDrinkId == foodAndDrinkId1 && tf.Quantity == 2) &&
            t.TicketFoodAndDrinks.Any(tf => tf.FoodAndDrinkId == foodAndDrinkId2 && tf.Quantity == 1)
        )), Times.Once);
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
    public async Task CreateOfflineTicketAsync_WithVIPSeat_ReturnsCorrectPriceForVIPSeat()
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
            Name = "VIP Room 1"
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            Movie = movie,
            CinemaRoom = cinemaRoom,
            ShowDate = DateTime.UtcNow.AddDays(1)
        };

        var vipSeat = new Seat
        {
            Id = seatId,
            Row = "V",
            Number = 1,
            Type = SeatType.VIP // VIP seat type
        };

        var createdTicket = new Ticket
        {
            Id = ticketId,
            IssuedAt = DateTime.UtcNow,
            GuestPhoneNumber = "+1234567890",
            TotalPrice = 120000, // VIP seat price
            TicketType = TicketType.Offline,
            TicketSeats = new List<TicketSeat>
        {
            new() { SeatId = seatId, PricePerSeat = 120000 } // VIP price
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
            .ReturnsAsync(new List<Seat> { vipSeat });

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

        // Verify VIP seat details in the response
        Assert.NotNull(result.Seats);
        Assert.Single(result.Seats);
        var seatDto = result.Seats.First();
        Assert.Equal("V", seatDto.Row);
        Assert.Equal(1, seatDto.Number);
        Assert.Equal(120000, seatDto.PricePerSeat); // VIP seat price

        // Verify that AddAsync was called with a ticket containing VIP seat pricing
        _mockTicketRepository.Verify(r => r.AddAsync(It.Is<Ticket>(t =>
            t.TicketSeats != null &&
            t.TicketSeats.Count == 1 &&
            t.TicketSeats.Any(ts => ts.SeatId == seatId && ts.PricePerSeat == 120000)
        )), Times.Once);

        _mockLoggerService.Verify(
            l => l.Info($"Creating offline ticket for guest phone number: {request.GuestPhoneNumber}, Showtime ID: {request.ShowtimeId}"),
            Times.Once);
    }

    [Fact]
    public async Task CreateOfflineTicketAsync_WithCoupleSeat_ReturnsCorrectPriceForCoupleSeat()
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
            Name = "Romantic Movie",
            PosterImage = "romantic.jpg"
        };

        var cinemaRoom = new CinemaRoom
        {
            Id = cinemaRoomId,
            Name = "Couple Room 1"
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            Movie = movie,
            CinemaRoom = cinemaRoom,
            ShowDate = DateTime.UtcNow.AddDays(1)
        };

        var coupleSeat = new Seat
        {
            Id = seatId,
            Row = "C",
            Number = 1,
            Type = SeatType.Couple // Couple seat type
        };

        var createdTicket = new Ticket
        {
            Id = ticketId,
            IssuedAt = DateTime.UtcNow,
            GuestPhoneNumber = "+1234567890",
            TotalPrice = 200000, // Couple seat price
            TicketType = TicketType.Offline,
            TicketSeats = new List<TicketSeat>
        {
            new() { SeatId = seatId, PricePerSeat = 200000 } // Couple price
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
            .ReturnsAsync(new List<Seat> { coupleSeat });

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

        // Verify Couple seat details in the response
        Assert.NotNull(result.Seats);
        Assert.Single(result.Seats);
        var seatDto = result.Seats.First();
        Assert.Equal("C", seatDto.Row);
        Assert.Equal(1, seatDto.Number);
        Assert.Equal(200000, seatDto.PricePerSeat); // Couple seat price

        // Verify that AddAsync was called with a ticket containing Couple seat pricing
        _mockTicketRepository.Verify(r => r.AddAsync(It.Is<Ticket>(t =>
            t.TicketSeats != null &&
            t.TicketSeats.Count == 1 &&
            t.TicketSeats.Any(ts => ts.SeatId == seatId && ts.PricePerSeat == 200000)
        )), Times.Once);

        _mockLoggerService.Verify(
            l => l.Info($"Creating offline ticket for guest phone number: {request.GuestPhoneNumber}, Showtime ID: {request.ShowtimeId}"),
            Times.Once);
    }

    [Fact]
    public async Task CreateOfflineTicketAsync_WithMixedSeatTypes_ReturnsCorrectPricesForAllSeatTypes()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var normalSeatId = Guid.NewGuid();
        var vipSeatId = Guid.NewGuid();
        var coupleSeatId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();

        var request = new CreateOfflineTicketRequest
        {
            GuestPhoneNumber = "+1234567890",
            ShowtimeId = showTimeId,
            SeatIds = new List<Guid> { normalSeatId, vipSeatId, coupleSeatId },
            FoodItems = new List<FoodItemRequest>()
        };

        var movie = new Movie
        {
            Id = movieId,
            Name = "Premium Movie",
            PosterImage = "premium.jpg"
        };

        var cinemaRoom = new CinemaRoom
        {
            Id = cinemaRoomId,
            Name = "Premium Room 1"
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            Movie = movie,
            CinemaRoom = cinemaRoom,
            ShowDate = DateTime.UtcNow.AddDays(1)
        };

        var normalSeat = new Seat
        {
            Id = normalSeatId,
            Row = "A",
            Number = 1,
            Type = SeatType.Normal
        };

        var vipSeat = new Seat
        {
            Id = vipSeatId,
            Row = "V",
            Number = 1,
            Type = SeatType.VIP
        };

        var coupleSeat = new Seat
        {
            Id = coupleSeatId,
            Row = "C",
            Number = 1,
            Type = SeatType.Couple
        };

        var totalPrice = 80000 + 120000 + 200000; // Normal + VIP + Couple
        var createdTicket = new Ticket
        {
            Id = ticketId,
            IssuedAt = DateTime.UtcNow,
            GuestPhoneNumber = "+1234567890",
            TotalPrice = totalPrice,
            TicketType = TicketType.Offline,
            TicketSeats = new List<TicketSeat>
        {
            new() { SeatId = normalSeatId, PricePerSeat = 80000 },
            new() { SeatId = vipSeatId, PricePerSeat = 120000 },
            new() { SeatId = coupleSeatId, PricePerSeat = 200000 }
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
            .ReturnsAsync(new List<Seat> { normalSeat, vipSeat, coupleSeat });

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

        // Verify all seat types and their prices in the response
        Assert.NotNull(result.Seats);
        Assert.Equal(3, result.Seats.Count);

        var normalSeatDto = result.Seats.FirstOrDefault(s => s.Row == "A");
        Assert.NotNull(normalSeatDto);
        Assert.Equal(80000, normalSeatDto.PricePerSeat);

        var vipSeatDto = result.Seats.FirstOrDefault(s => s.Row == "V");
        Assert.NotNull(vipSeatDto);
        Assert.Equal(120000, vipSeatDto.PricePerSeat);

        var coupleSeatDto = result.Seats.FirstOrDefault(s => s.Row == "C");
        Assert.NotNull(coupleSeatDto);
        Assert.Equal(200000, coupleSeatDto.PricePerSeat);

        // Verify that AddAsync was called with a ticket containing all seat types with correct pricing
        _mockTicketRepository.Verify(r => r.AddAsync(It.Is<Ticket>(t =>
            t.TicketSeats != null &&
            t.TicketSeats.Count == 3 &&
            t.TicketSeats.Any(ts => ts.SeatId == normalSeatId && ts.PricePerSeat == 80000) &&
            t.TicketSeats.Any(ts => ts.SeatId == vipSeatId && ts.PricePerSeat == 120000) &&
            t.TicketSeats.Any(ts => ts.SeatId == coupleSeatId && ts.PricePerSeat == 200000)
        )), Times.Once);

        _mockLoggerService.Verify(
            l => l.Info($"Creating offline ticket for guest phone number: {request.GuestPhoneNumber}, Showtime ID: {request.ShowtimeId}"),
            Times.Once);
    }

    [Fact]
    public async Task CreateOfflineTicketAsync_WithDefaultSeatType_ReturnsDefaultPrice()
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

        // Create a seat with an unknown/undefined seat type to test the default case
        var unknownTypeSeat = new Seat
        {
            Id = seatId,
            Row = "X",
            Number = 1,
            Type = (SeatType)999 // Invalid enum value to trigger default case
        };

        var createdTicket = new Ticket
        {
            Id = ticketId,
            IssuedAt = DateTime.UtcNow,
            GuestPhoneNumber = "+1234567890",
            TotalPrice = 80000, // Default price
            TicketType = TicketType.Offline,
            TicketSeats = new List<TicketSeat>
        {
            new() { SeatId = seatId, PricePerSeat = 80000 } // Default price
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
            .ReturnsAsync(new List<Seat> { unknownTypeSeat });

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

        // Verify default seat pricing in the response
        Assert.NotNull(result.Seats);
        Assert.Single(result.Seats);
        var seatDto = result.Seats.First();
        Assert.Equal("X", seatDto.Row);
        Assert.Equal(1, seatDto.Number);
        Assert.Equal(80000, seatDto.PricePerSeat); // Default price

        // Verify that AddAsync was called with a ticket containing default seat pricing
        _mockTicketRepository.Verify(r => r.AddAsync(It.Is<Ticket>(t =>
            t.TicketSeats != null &&
            t.TicketSeats.Count == 1 &&
            t.TicketSeats.Any(ts => ts.SeatId == seatId && ts.PricePerSeat == 80000)
        )), Times.Once);

        _mockLoggerService.Verify(
            l => l.Info($"Creating offline ticket for guest phone number: {request.GuestPhoneNumber}, Showtime ID: {request.ShowtimeId}"),
            Times.Once);
    }

    [Fact]
    public async Task CreateOfflineTicketAsync_ValidRequestWithFoodItems_ReturnsTicketResponseWithFoodItems()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var foodAndDrinkId1 = Guid.NewGuid();
        var foodAndDrinkId2 = Guid.NewGuid();

        var request = new CreateOfflineTicketRequest
        {
            GuestPhoneNumber = "+1234567890",
            ShowtimeId = showTimeId,
            SeatIds = new List<Guid> { seatId },
            FoodItems = new List<FoodItemRequest>
            {
                new() { FoodAndDrinkId = foodAndDrinkId1, Quantity = 2 },
                new() { FoodAndDrinkId = foodAndDrinkId2, Quantity = 1 }
            }
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

        // Create food and drink items for price calculation
        var foodAndDrink1 = new FoodAndDrink
        {
            Id = foodAndDrinkId1,
            Name = "Popcorn",
            Price = 15000,
            Type = FoodType.Food
        };

        var foodAndDrink2 = new FoodAndDrink
        {
            Id = foodAndDrinkId2,
            Name = "Coca Cola",
            Price = 10000,
            Type = FoodType.Drink
        };

        var createdTicket = new Ticket
        {
            Id = ticketId,
            IssuedAt = DateTime.UtcNow,
            GuestPhoneNumber = "+1234567890",
            TotalPrice = 120000, // 80000 (seat) + 30000 (2x popcorn) + 10000 (1x drink)
            TicketType = TicketType.Offline,
            TicketSeats = new List<TicketSeat>
            {
                new() { SeatId = seatId, PricePerSeat = 80000 }
            },
            TicketFoodAndDrinks = new List<TicketFoodAndDrink>
            {
                new() { FoodAndDrinkId = foodAndDrinkId1, Quantity = 2 },
                new() { FoodAndDrinkId = foodAndDrinkId2, Quantity = 1 }
            },
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

        // Mock FoodAndDrinks repository for price calculation during ticket creation
        _mockFoodAndDrinkRepository.Setup(r => r.GetByIdAsync(foodAndDrinkId1,
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()))
            .ReturnsAsync(foodAndDrink1);

        _mockFoodAndDrinkRepository.Setup(r => r.GetByIdAsync(foodAndDrinkId2,
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()))
            .ReturnsAsync(foodAndDrink2);

        // Mock FoodAndDrinks repository for GetTicketDetailsAsync
        _mockFoodAndDrinkRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()))
            .ReturnsAsync(new List<FoodAndDrink> { foodAndDrink1, foodAndDrink2 });

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

        // Verify food items are included in the response
        Assert.NotNull(result.FoodItems);
        Assert.Equal(2, result.FoodItems.Count);

        var popcornItem = result.FoodItems.FirstOrDefault(f => f.FoodId == foodAndDrinkId1);
        Assert.NotNull(popcornItem);
        Assert.Equal("Popcorn", popcornItem.Name);
        Assert.Equal(2, popcornItem.Quantity);
        Assert.Equal(15000, popcornItem.Price);

        var drinkItem = result.FoodItems.FirstOrDefault(f => f.FoodId == foodAndDrinkId2);
        Assert.NotNull(drinkItem);
        Assert.Equal("Coca Cola", drinkItem.Name);
        Assert.Equal(1, drinkItem.Quantity);
        Assert.Equal(10000, drinkItem.Price);

        _mockLoggerService.Verify(
            l => l.Info($"Creating offline ticket for guest phone number: {request.GuestPhoneNumber}, Showtime ID: {request.ShowtimeId}"),
            Times.Once);

        // Verify that AddAsync was called with a ticket containing food items
        _mockTicketRepository.Verify(r => r.AddAsync(It.Is<Ticket>(t =>
            t.TicketFoodAndDrinks != null &&
            t.TicketFoodAndDrinks.Count == 2 &&
            t.TicketFoodAndDrinks.Any(tf => tf.FoodAndDrinkId == foodAndDrinkId1 && tf.Quantity == 2) &&
            t.TicketFoodAndDrinks.Any(tf => tf.FoodAndDrinkId == foodAndDrinkId2 && tf.Quantity == 1)
        )), Times.Once);

        // Verify ShowTimeSeat entities were created for seat availability tracking
        _mockShowTimeSeatRepository.Verify(r => r.AddAsync(It.Is<ShowTimeSeat>(sts =>
            sts.ShowTimeId == showTimeId &&
            sts.SeatId == seatId &&
            sts.Status == SeatStatus.Sold
        )), Times.Once);
    }

    [Fact]
    public async Task CreateOfflineTicketAsync_SomeSeatsInvalid_ThrowsInvalidOperationException()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var seatId1 = Guid.NewGuid();
        var seatId2 = Guid.NewGuid(); // This seat will not exist in the database
        var movieId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();

        var request = new CreateOfflineTicketRequest
        {
            GuestPhoneNumber = "+1234567890",
            ShowtimeId = showTimeId,
            SeatIds = new List<Guid> { seatId1, seatId2 }, // Requesting 2 seats
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

        var seat1 = new Seat
        {
            Id = seatId1,
            Row = "A",
            Number = 1,
            Type = SeatType.Normal
        };
        // Note: seat2 is intentionally not included in the returned seats

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(showTime);

        // Mock seat repository to return only 1 seat when 2 are requested
        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat> { seat1 }); // Only returning 1 seat out of 2 requested

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _ticketService.CreateOfflineTicketAsync(request));

        Assert.Equal("Failed to create offline ticket", ex.Message);
        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Equal("Some seats are invalid.", ex.InnerException.Message);

        // Verify that the showtime was validated
        _mockShowTimeRepository.Verify(r => r.GetByIdAsync(showTimeId,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()),
            Times.Once);

        // Verify that seat validation was attempted
        _mockSeatRepository.Verify(r => r.GetAllAsync(
            It.Is<System.Linq.Expressions.Expression<Func<Seat, bool>>>(expr => true),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()),
            Times.Once);

        // Verify no tickets were created
        _mockTicketRepository.Verify(r => r.AddAsync(It.IsAny<Ticket>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
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
    public async Task GetAllTicketsAsync_WithSearchByMovieName_ReturnsFilteredResults()
    {
        // Arrange
        var movieId1 = Guid.NewGuid();
        var movieId2 = Guid.NewGuid();
        var showTimeId1 = Guid.NewGuid();
        var showTimeId2 = Guid.NewGuid();

        var movie1 = new Movie
        {
            Id = movieId1,
            Name = "Avengers Endgame",
            IsDeleted = false
        };

        var movie2 = new Movie
        {
            Id = movieId2,
            Name = "Spider Man",
            IsDeleted = false
        };

        var showTime1 = new ShowTime
        {
            Id = showTimeId1,
            MovieId = movieId1,
            Movie = movie1,
            IsDeleted = false
        };

        var showTime2 = new ShowTime
        {
            Id = showTimeId2,
            MovieId = movieId2,
            Movie = movie2,
            IsDeleted = false
        };

        var tickets = new List<Ticket>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Online,
                IssuedAt = DateTime.UtcNow,
                TotalPrice = 100,
                GuestPhoneNumber = "+1234567890",
                Showtime = showTime1
            },
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Offline,
                IssuedAt = DateTime.UtcNow.AddDays(-1),
                TotalPrice = 150,
                GuestPhoneNumber = "+0987654321",
                Showtime = showTime2
            }
        };

        var searchTerm = "avengers";

        _mockTicketRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(tickets);

        // Mock ShowTimes repository to return showtimes that match the search
        _mockShowTimeRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(new List<ShowTime> { showTime1 }); // Only showTime1 matches "avengers"

        SetupGetTicketDetailsMocks();

        // Act
        var result = await _ticketService.GetAllTicketsAsync(search: searchTerm);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items); // Only one ticket should match
        Assert.Equal(1, result.TotalCount);

        // Verify search logging
        _mockLoggerService.Verify(
            l => l.Info($"Searching tickets with term: {searchTerm.ToLower()}"),
            Times.Once);

        _mockLoggerService.Verify(
            l => l.Info($"Filtered tickets by search term '{searchTerm.ToLower()}', total count: 1"),
            Times.Once);

        // Verify ShowTimes repository was called for movie search
        _mockShowTimeRepository.Verify(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllTicketsAsync_WithSearchByPhoneNumber_ReturnsFilteredResults()
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

        var searchTerm = "1234"; // Should match first phone number

        _mockTicketRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(tickets);

        // Mock ShowTimes repository to return empty list for movie search
        _mockShowTimeRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(new List<ShowTime>());

        SetupGetTicketDetailsMocks();

        // Act
        var result = await _ticketService.GetAllTicketsAsync(search: searchTerm);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items); // Only one ticket should match
        Assert.Equal(1, result.TotalCount);

        // Verify search logging
        _mockLoggerService.Verify(
            l => l.Info($"Searching tickets with term: {searchTerm.ToLower()}"),
            Times.Once);

        _mockLoggerService.Verify(
            l => l.Info($"Filtered tickets by search term '{searchTerm.ToLower()}', total count: 1"),
            Times.Once);
    }

    [Fact]
    public async Task GetAllTicketsAsync_WithSearchNoMatches_ReturnsEmptyResults()
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
            }
        };

        var searchTerm = "nonexistent"; // Should not match anything

        _mockTicketRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(tickets);

        // Mock ShowTimes repository to return empty list for movie search
        _mockShowTimeRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(new List<ShowTime>());

        SetupGetTicketDetailsMocks();

        // Act
        var result = await _ticketService.GetAllTicketsAsync(search: searchTerm);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);

        // Verify search logging
        _mockLoggerService.Verify(
            l => l.Info($"Searching tickets with term: {searchTerm.ToLower()}"),
            Times.Once);

        _mockLoggerService.Verify(
            l => l.Info($"Filtered tickets by search term '{searchTerm.ToLower()}', total count: 0"),
            Times.Once);
    }

    [Fact]
    public async Task GetAllTicketsAsync_WithSearchBothMovieAndPhone_ReturnsAllMatches()
    {
        // Arrange
        var movieId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();

        var movie = new Movie
        {
            Id = movieId,
            Name = "Test Movie",
            IsDeleted = false
        };

        var showTime = new ShowTime
        {
            Id = showTimeId,
            MovieId = movieId,
            Movie = movie,
            IsDeleted = false
        };

        var tickets = new List<Ticket>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Online,
                IssuedAt = DateTime.UtcNow,
                TotalPrice = 100,
                GuestPhoneNumber = "+1234567890",
                Showtime = showTime // Matches by movie name
            },
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Offline,
                IssuedAt = DateTime.UtcNow.AddDays(-1),
                TotalPrice = 150,
                GuestPhoneNumber = "+test987654", // Matches by phone containing "test"
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            },
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Online,
                IssuedAt = DateTime.UtcNow.AddDays(-2),
                TotalPrice = 200,
                GuestPhoneNumber = "+9999999999", // Should not match
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            }
        };

        var searchTerm = "test"; // Should match both movie name and phone number

        _mockTicketRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(tickets);

        // Mock ShowTimes repository to return showtime that matches the search
        _mockShowTimeRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(new List<ShowTime> { showTime });

        SetupGetTicketDetailsMocks();

        // Act
        var result = await _ticketService.GetAllTicketsAsync(search: searchTerm);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count); // Two tickets should match
        Assert.Equal(2, result.TotalCount);

        // Verify search logging
        _mockLoggerService.Verify(
            l => l.Info($"Searching tickets with term: {searchTerm.ToLower()}"),
            Times.Once);

        _mockLoggerService.Verify(
            l => l.Info($"Filtered tickets by search term '{searchTerm.ToLower()}', total count: 2"),
            Times.Once);
    }

    [Fact]
    public async Task GetAllTicketsAsync_WithEmptySearch_ReturnsAllTickets()
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

        SetupGetTicketDetailsMocks();

        // Act - empty search string should not trigger search logic
        var result = await _ticketService.GetAllTicketsAsync(search: "");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count); // All tickets should be returned
        Assert.Equal(2, result.TotalCount);

        // Verify search logging was NOT called (empty search doesn't trigger search logic)
        _mockLoggerService.Verify(
            l => l.Info(It.Is<string>(msg => msg.Contains("Searching tickets with term"))),
            Times.Never);

        // Verify ShowTimes repository was NOT called for search
        _mockShowTimeRepository.Verify(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()),
            Times.Never);
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
    public async Task GetAllTicketsAsync_SortByDateAscending_ReturnsSortedResults()
    {
        // Arrange
        var baseDate = DateTime.UtcNow;
        var tickets = new List<Ticket>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Online,
                IssuedAt = baseDate.AddDays(2), // Latest
                TotalPrice = 100,
                GuestPhoneNumber = "+1234567890",
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            },
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Offline,
                IssuedAt = baseDate.AddDays(-1), // Earliest
                TotalPrice = 150,
                GuestPhoneNumber = "+0987654321",
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            },
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Online,
                IssuedAt = baseDate.AddDays(1), // Middle
                TotalPrice = 200,
                GuestPhoneNumber = "+1111111111",
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            }
        };

        _mockTicketRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(tickets);

        SetupGetTicketDetailsMocks();

        // Act
        var result = await _ticketService.GetAllTicketsAsync(sortBy: "date", isDescending: false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count);

        // Verify ascending order by date (earliest first)
        Assert.True(result.Items[0].IssuedAt <= result.Items[1].IssuedAt);
        Assert.True(result.Items[1].IssuedAt <= result.Items[2].IssuedAt);
    }

    [Fact]
    public async Task GetAllTicketsAsync_SortByDateDescending_ReturnsSortedResults()
    {
        // Arrange
        var baseDate = DateTime.UtcNow;
        var tickets = new List<Ticket>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Online,
                IssuedAt = baseDate.AddDays(-1), // Earliest
                TotalPrice = 100,
                GuestPhoneNumber = "+1234567890",
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            },
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Offline,
                IssuedAt = baseDate.AddDays(2), // Latest
                TotalPrice = 150,
                GuestPhoneNumber = "+0987654321",
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            },
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Online,
                IssuedAt = baseDate.AddDays(1), // Middle
                TotalPrice = 200,
                GuestPhoneNumber = "+1111111111",
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            }
        };

        _mockTicketRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(tickets);

        SetupGetTicketDetailsMocks();

        // Act
        var result = await _ticketService.GetAllTicketsAsync(sortBy: "date", isDescending: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count);

        // Verify descending order by date (latest first)
        Assert.True(result.Items[0].IssuedAt >= result.Items[1].IssuedAt);
        Assert.True(result.Items[1].IssuedAt >= result.Items[2].IssuedAt);
    }

    [Fact]
    public async Task GetAllTicketsAsync_SortByPriceAscending_ReturnsSortedResults()
    {
        // Arrange
        var tickets = new List<Ticket>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Online,
                IssuedAt = DateTime.UtcNow,
                TotalPrice = 300, // Highest
                GuestPhoneNumber = "+1234567890",
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            },
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Offline,
                IssuedAt = DateTime.UtcNow.AddDays(-1),
                TotalPrice = 100, // Lowest
                GuestPhoneNumber = "+0987654321",
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            },
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Online,
                IssuedAt = DateTime.UtcNow.AddDays(1),
                TotalPrice = 200, // Middle
                GuestPhoneNumber = "+1111111111",
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            }
        };

        _mockTicketRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(tickets);

        SetupGetTicketDetailsMocks();

        // Act
        var result = await _ticketService.GetAllTicketsAsync(sortBy: "price", isDescending: false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count);

        // Verify ascending order by price (lowest first)
        Assert.True(result.Items[0].TotalPrice <= result.Items[1].TotalPrice);
        Assert.True(result.Items[1].TotalPrice <= result.Items[2].TotalPrice);
    }

    [Fact]
    public async Task GetAllTicketsAsync_SortByPriceDescending_ReturnsSortedResults()
    {
        // Arrange
        var tickets = new List<Ticket>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Online,
                IssuedAt = DateTime.UtcNow,
                TotalPrice = 100, // Lowest
                GuestPhoneNumber = "+1234567890",
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            },
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Offline,
                IssuedAt = DateTime.UtcNow.AddDays(-1),
                TotalPrice = 300, // Highest
                GuestPhoneNumber = "+0987654321",
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            },
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Online,
                IssuedAt = DateTime.UtcNow.AddDays(1),
                TotalPrice = 200, // Middle
                GuestPhoneNumber = "+1111111111",
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            }
        };

        _mockTicketRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(tickets);

        SetupGetTicketDetailsMocks();

        // Act
        var result = await _ticketService.GetAllTicketsAsync(sortBy: "price", isDescending: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count);

        // Verify descending order by price (highest first)
        Assert.True(result.Items[0].TotalPrice >= result.Items[1].TotalPrice);
        Assert.True(result.Items[1].TotalPrice >= result.Items[2].TotalPrice);
    }

    [Fact]
    public async Task GetAllTicketsAsync_SortByInvalidField_DefaultsToDateDescending()
    {
        // Arrange
        var baseDate = DateTime.UtcNow;
        var tickets = new List<Ticket>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Online,
                IssuedAt = baseDate.AddDays(-1), // Earliest
                TotalPrice = 100,
                GuestPhoneNumber = "+1234567890",
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            },
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Offline,
                IssuedAt = baseDate.AddDays(1), // Latest
                TotalPrice = 150,
                GuestPhoneNumber = "+0987654321",
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            }
        };

        _mockTicketRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(tickets);

        SetupGetTicketDetailsMocks();

        // Act - using invalid sort field should default to date sorting
        var result = await _ticketService.GetAllTicketsAsync(sortBy: "invalid", isDescending: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);

        // Should default to date descending (latest first)
        Assert.True(result.Items[0].IssuedAt >= result.Items[1].IssuedAt);
    }

    [Fact]
    public async Task GetAllTicketsAsync_NoSortBySpecified_DefaultsToDateDescending()
    {
        // Arrange
        var baseDate = DateTime.UtcNow;
        var tickets = new List<Ticket>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Online,
                IssuedAt = baseDate.AddDays(-1), // Earliest
                TotalPrice = 100,
                GuestPhoneNumber = "+1234567890",
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            },
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Offline,
                IssuedAt = baseDate.AddDays(1), // Latest
                TotalPrice = 150,
                GuestPhoneNumber = "+0987654321",
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            }
        };

        _mockTicketRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(tickets);

        SetupGetTicketDetailsMocks();

        // Act - no sortBy parameter should default to date sorting
        var result = await _ticketService.GetAllTicketsAsync(isDescending: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);

        // Should default to date descending (latest first)
        Assert.True(result.Items[0].IssuedAt >= result.Items[1].IssuedAt);
    }

    [Fact]
    public async Task GetAllTicketsAsync_SortByCaseSensitive_WorksCorrectly()
    {
        // Arrange
        var tickets = new List<Ticket>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Online,
                IssuedAt = DateTime.UtcNow,
                TotalPrice = 200, // Higher
                GuestPhoneNumber = "+1234567890",
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            },
            new()
            {
                Id = Guid.NewGuid(),
                TicketType = TicketType.Offline,
                IssuedAt = DateTime.UtcNow.AddDays(-1),
                TotalPrice = 100, // Lower
                GuestPhoneNumber = "+0987654321",
                Showtime = new ShowTime { Id = Guid.NewGuid() }
            }
        };

        _mockTicketRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(tickets);

        SetupGetTicketDetailsMocks();

        // Act - using uppercase "PRICE" should still work due to ToLower()
        var result = await _ticketService.GetAllTicketsAsync(sortBy: "PRICE", isDescending: false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);

        // Should sort by price ascending (lower first)
        Assert.True(result.Items[0].TotalPrice <= result.Items[1].TotalPrice);
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
    public async Task GenerateTicketQRCodeAsync_TicketDetailsNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Mock the ticket repository to return null, which will cause GetTicketDetailsAsync to return null
        _mockTicketRepository.Setup(r => r.GetByIdAsync(ticketId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync((Ticket)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _ticketService.GenerateTicketQRCodeAsync(ticketId));

        Assert.Equal($"Ticket with ID {ticketId} not found.", ex.Message);

        // Verify that the info log was called
        _mockLoggerService.Verify(
            l => l.Info($"Generating QR code for ticket ID: {ticketId}"),
            Times.Once);

        // Verify that the error log was called
        _mockLoggerService.Verify(
            l => l.Error($"Error generating QR code for ticket {ticketId}: Ticket with ID {ticketId} not found."),
            Times.Once);

        // Verify that no success log was called
        _mockLoggerService.Verify(
            l => l.Success(It.IsAny<string>()),
            Times.Never);
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

    [Fact]
    public async Task GenerateTicketQRCodeAsync_QRContentSizeValidation_CanHandleNormalContent()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        SetupGetTicketDetailsMocks();

        // Act
        var result = await _ticketService.GenerateTicketQRCodeAsync(ticketId);

        // Assert - Verify that normal content generates successfully and is within size limits
        Assert.NotNull(result);
        Assert.StartsWith("data:image/png;base64,", result);

        // Test that the QR payload size validation exists by examining the method behavior
        // Since HMAC-SHA256 produces fixed-length output (~44 chars base64) and GUIDs are 36 chars,
        // normal content should never exceed 2953 characters

        // Verify success logging occurred (indicating no size limit was hit)
        _mockLoggerService.Verify(
            l => l.Success($"QR code generated successfully for ticket ID: {ticketId}"),
            Times.Once);

        // Verify no size limit warning was logged
        _mockLoggerService.Verify(
            l => l.Warn(It.Is<string>(msg => msg.Contains("QR content too large"))),
            Times.Never);
    }

    // Alternative test that documents the edge case
    [Fact]
    public void QRCodePayload_SerializationSize_ShouldNormallyBeWithinLimits()
    {
        // Arrange - Create a typical QR payload
        var payload = new QrCodePayload
        {
            TicketId = Guid.NewGuid(),
            Hash = Convert.ToBase64String(new byte[32]), // Typical HMAC-SHA256 output size
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };

        // Act - Serialize using the same logic as the service
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        string qrContent = JsonSerializer.Serialize(payload, jsonOptions);

        // Assert - Normal content should be well within the 2953 character limit
        Assert.True(qrContent.Length < 2953,
            $"Normal QR content should be within size limits. Actual size: {qrContent.Length}");

        // Typical size should be around 150-200 characters
        Assert.True(qrContent.Length < 500,
            $"Normal QR content should be much smaller than limit. Actual size: {qrContent.Length}");
    }

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
    public async Task VerifyTicketQRCodeAsync_TicketDetailsNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var qrCodeData = new QrCodePayload
        {
            TicketId = ticketId,
            Hash = "valid-hash",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15) // Valid expiration
        };

        // Mock the ticket repository to return null, which will cause GetTicketDetailsAsync to throw KeyNotFoundException
        _mockTicketRepository.Setup(r => r.GetByIdAsync(ticketId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync((Ticket)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _ticketService.VerifyTicketQRCodeAsync(qrCodeData));

        Assert.Equal("QR code verification failed due to system error", ex.Message);
        Assert.IsType<KeyNotFoundException>(ex.InnerException);
        Assert.Equal($"Ticket with ID {ticketId} not found.", ex.InnerException.Message);

        // Verify that the info log was called at the start
        _mockLoggerService.Verify(
            l => l.Info($"Verifying ticket QR code for ticket ID: {ticketId}"),
            Times.Once);

        // Verify that the warning log was called when ticket details are null
        // The actual log message from GetTicketDetailsAsync is different than what's in VerifyTicketQRCodeAsync
        _mockLoggerService.Verify(
            l => l.Warn($"No ticket found with ID: {ticketId}"),
            Times.Once);

        // Verify that the error log was called in the catch block
        _mockLoggerService.Verify(
            l => l.Error($"Error verifying ticket QR code: Ticket with ID {ticketId} not found."),
            Times.Once);

        // Verify that no success log was called
        _mockLoggerService.Verify(
            l => l.Success(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task VerifyTicketQRCodeAsync_InvalidHash_ThrowsInvalidOperationException()
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

        // Create QR code data with an INVALID hash (this is the key difference from the valid test)
        var invalidHash = "invalid-hash-that-does-not-match-ticket-data";
        var qrCodeData = new QrCodePayload
        {
            TicketId = ticketId,
            Hash = invalidHash, // Using an invalid hash to trigger the validation failure
            ExpiresAt = DateTime.UtcNow.AddMinutes(15) // Valid expiration
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _ticketService.VerifyTicketQRCodeAsync(qrCodeData));

        Assert.Equal("QR code verification failed due to system error", ex.Message);
        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Equal("QR code hash does not match the ticket data", ex.InnerException.Message);

        // Verify that the info log was called at the start
        _mockLoggerService.Verify(
            l => l.Info($"Verifying ticket QR code for ticket ID: {ticketId}"),
            Times.Once);

        // Verify that the warning log was called when hash validation fails
        _mockLoggerService.Verify(
            l => l.Warn($"Hash validation failed for ticket {ticketId}"),
            Times.Once);

        // Verify that the error log was called in the catch block
        _mockLoggerService.Verify(
            l => l.Error($"Error verifying ticket QR code: QR code hash does not match the ticket data"),
            Times.Once);

        // Verify that no success log was called
        _mockLoggerService.Verify(
            l => l.Success(It.IsAny<string>()),
            Times.Never);
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

    [Fact]
    public async Task GetTicketByIdAsync_SeatNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
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

        // Setup ticket repository to return the ticket
        _mockTicketRepository.Setup(r => r.GetByIdAsync(ticketId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(ticket);

        // Setup seat repository to return empty list (no seats found) - this is the key part
        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat>()); // Empty list means seat not found

        // Setup showtime repository to return the showtime
        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(showTime);

        // Setup food and drink repository
        _mockFoodAndDrinkRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()))
            .ReturnsAsync(new List<FoodAndDrink>());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _ticketService.GetTicketByIdAsync(ticketId));

        Assert.Equal($"Seat with ID {seatId} not found for ticket {ticketId}.", ex.Message);

        // Verify that the warning was logged
        _mockLoggerService.Verify(
            l => l.Warn($"Seat with ID {seatId} not found for ticket {ticketId}"),
            Times.Once);

        // Verify that the error was logged in the GetTicketByIdAsync catch block
        _mockLoggerService.Verify(
            l => l.Error($"Error fetching ticket {ticketId}: Seat with ID {seatId} not found for ticket {ticketId}."),
            Times.Once);
    }

    [Fact]
    public async Task GetTicketByIdAsync_ShowtimeNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();

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

        // Setup ticket repository to return the ticket
        _mockTicketRepository.Setup(r => r.GetByIdAsync(ticketId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Ticket, object>>[]>()))
            .ReturnsAsync(ticket);

        // Setup seat repository to return the seat
        _mockSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Seat, object>>[]>()))
            .ReturnsAsync(new List<Seat> { seat });

        // Setup showtime repository to return null (showtime not found) - this is the key part
        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync((ShowTime)null!);

        // Setup food and drink repository
        _mockFoodAndDrinkRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<FoodAndDrink, object>>[]>()))
            .ReturnsAsync(new List<FoodAndDrink>());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _ticketService.GetTicketByIdAsync(ticketId));

        Assert.Equal($"Showtime for ticket with ID {ticketId} not found.", ex.Message);

        // Verify that the warning was logged when showtime is not found
        _mockLoggerService.Verify(
            l => l.Warn($"No showtime found for ticket with ID: {ticketId}"),
            Times.Once);

        // Verify that the error was logged in the GetTicketByIdAsync catch block
        _mockLoggerService.Verify(
            l => l.Error($"Error fetching ticket {ticketId}: Showtime for ticket with ID {ticketId} not found."),
            Times.Once);

        // Verify that the showtime repository was called with the correct showtime ID
        _mockShowTimeRepository.Verify(r => r.GetByIdAsync(showTimeId,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()),
            Times.Once);
    }

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

}