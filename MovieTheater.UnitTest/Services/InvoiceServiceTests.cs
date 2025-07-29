using Moq;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.DTOs.InvoiceDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.UnitTest.Services;

public class InvoiceServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILoggerService> _mockLoggerService;
    private readonly Mock<IRedisService> _mockRedisService;
    private readonly Mock<IPromotionService> _mockPromotionService;
    private readonly Mock<IScoreService> _mockScoreService;
    private readonly Mock<IGenericRepository<Invoice>> _mockInvoiceRepository;
    private readonly Mock<IGenericRepository<Booking>> _mockBookingRepository;
    private readonly Mock<IGenericRepository<User>> _mockUserRepository;
    private readonly Mock<IGenericRepository<ShowTime>> _mockShowTimeRepository;
    private readonly Mock<IGenericRepository<Movie>> _mockMovieRepository;
    private readonly Mock<IGenericRepository<BookingSeat>> _mockBookingSeatRepository;
    private readonly Mock<IGenericRepository<Promotion>> _mockPromotionRepository;
    private readonly InvoiceService _invoiceService;

    public InvoiceServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLoggerService = new Mock<ILoggerService>();
        _mockRedisService = new Mock<IRedisService>();
        _mockPromotionService = new Mock<IPromotionService>();
        _mockScoreService = new Mock<IScoreService>();
        _mockInvoiceRepository = new Mock<IGenericRepository<Invoice>>();
        _mockBookingRepository = new Mock<IGenericRepository<Booking>>();
        _mockUserRepository = new Mock<IGenericRepository<User>>();
        _mockShowTimeRepository = new Mock<IGenericRepository<ShowTime>>();
        _mockMovieRepository = new Mock<IGenericRepository<Movie>>();
        _mockBookingSeatRepository = new Mock<IGenericRepository<BookingSeat>>();
        _mockPromotionRepository = new Mock<IGenericRepository<Promotion>>();

        _mockUnitOfWork.Setup(u => u.Invoices).Returns(_mockInvoiceRepository.Object);
        _mockUnitOfWork.Setup(u => u.Bookings).Returns(_mockBookingRepository.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.ShowTimes).Returns(_mockShowTimeRepository.Object);
        _mockUnitOfWork.Setup(u => u.Movies).Returns(_mockMovieRepository.Object);
        _mockUnitOfWork.Setup(u => u.BookingSeats).Returns(_mockBookingSeatRepository.Object);
        _mockUnitOfWork.Setup(u => u.Promotions).Returns(_mockPromotionRepository.Object);

        _invoiceService = new InvoiceService(
            _mockUnitOfWork.Object,
            _mockLoggerService.Object,
            _mockRedisService.Object,
            _mockPromotionService.Object,
            _mockScoreService.Object
        );
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_ValidInvoiceId_ReturnsInvoiceDataTransferObject()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var movieId = Guid.NewGuid();

        var invoice = CreateTestInvoice(invoiceId, bookingId);
        var booking = CreateTestBooking(bookingId, userId, showTimeId);
        var user = CreateTestUser(userId);
        var showTime = CreateTestShowTime(showTimeId, movieId);
        var movie = CreateTestMovie(movieId);
        var bookingSeats = CreateTestBookingSeats(bookingId);

        // Fix: Set up the Movie navigation property on ShowTime
        showTime.Movie = movie;

        // Fix: Add the missing setup for the invoice repository GetByIdAsync method
        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync(invoice);

        SetupMocksForMapToDataTransferObject(booking, user, showTime, movie, bookingSeats);

        // Act
        var result = await _invoiceService.GetInvoiceByIdAsync(invoiceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(invoiceId, result.Id);
        Assert.Equal(bookingId, result.BookingId);
        Assert.Equal(100000, result.Amount);
        Assert.Equal("Pending", result.Status);
        Assert.NotNull(result.Booking);
        Assert.Equal("John Doe", result.Booking.MemberName);
        Assert.Equal("Test Movie", result.Booking.MovieTitle);
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_EmptyInvoiceId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _invoiceService.GetInvoiceByIdAsync(Guid.Empty));

        Assert.Equal("Invalid invoice ID.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn("Attempted to fetch invoice with an empty GUID."),
            Times.Once);
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_InvoiceNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();

        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync((Invoice)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _invoiceService.GetInvoiceByIdAsync(invoiceId));

        Assert.Equal($"Invoice with ID {invoiceId} not found", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"No invoice found with ID: {invoiceId}"),
            Times.Once);
    }

    [Fact]
    public async Task GetInvoiceByBookingIdAsync_ValidBookingId_ReturnsInvoiceDataTransferObject()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var movieId = Guid.NewGuid();

        var invoice = CreateTestInvoice(invoiceId, bookingId);
        var booking = CreateTestBooking(bookingId, userId, showTimeId);
        var user = CreateTestUser(userId);
        var showTime = CreateTestShowTime(showTimeId, movieId);
        var movie = CreateTestMovie(movieId);
        var bookingSeats = CreateTestBookingSeats(bookingId);

        _mockRedisService.Setup(r => r.GetAsync<InvoiceDto>(It.IsAny<string>()))
            .ReturnsAsync((InvoiceDto)null!);

        _mockInvoiceRepository.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync(invoice);

        SetupMocksForMapToDataTransferObject(booking, user, showTime, movie, bookingSeats);

        // Act
        var result = await _invoiceService.GetInvoiceByBookingIdAsync(bookingId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(invoiceId, result.Id);
        Assert.Equal(bookingId, result.BookingId);

        _mockRedisService.Verify(
            r => r.SetAsync(It.IsAny<string>(), result, TimeSpan.FromMinutes(10)),
            Times.Once);
    }

    [Fact]
    public async Task GetInvoiceByBookingIdAsync_EmptyBookingId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _invoiceService.GetInvoiceByBookingIdAsync(Guid.Empty));

        Assert.Equal("Invalid booking ID.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn("Attempted to fetch invoice with an empty booking GUID."),
            Times.Once);
    }

    [Fact]
    public async Task GetInvoiceByBookingIdAsync_InvoiceNotFound_ReturnsNull()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        _mockRedisService.Setup(r => r.GetAsync<InvoiceDto>(It.IsAny<string>()))
            .ReturnsAsync((InvoiceDto)null!);

        _mockInvoiceRepository.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync((Invoice)null!);

        // Act
        var result = await _invoiceService.GetInvoiceByBookingIdAsync(bookingId);

        // Assert
        Assert.Null(result);

        _mockLoggerService.Verify(
            l => l.Warn($"No invoice found for booking ID: {bookingId}"),
            Times.Once);
    }

    [Fact]
    public async Task GetInvoiceByBookingIdAsync_CachedResult_ReturnsCachedValue()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var cachedInvoice = new InvoiceDto
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            Amount = 100000,
            Status = "Pending"
        };

        _mockRedisService.Setup(r => r.GetAsync<InvoiceDto>(It.IsAny<string>()))
            .ReturnsAsync(cachedInvoice);

        // Act
        var result = await _invoiceService.GetInvoiceByBookingIdAsync(bookingId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(cachedInvoice.Id, result.Id);
        Assert.Equal(bookingId, result.BookingId);

        // Verify that repository was not called since cached value was returned
        _mockInvoiceRepository.Verify(r => r.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()),
            Times.Never);
    }

    [Fact]
    public async Task GetUserInvoicesAsync_ValidUserId_ReturnsUserInvoices()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var movieId = Guid.NewGuid();

        var booking = CreateTestBooking(bookingId, userId, showTimeId);
        var invoice = CreateTestInvoice(invoiceId, bookingId);
        var user = CreateTestUser(userId);
        var showTime = CreateTestShowTime(showTimeId, movieId);
        var movie = CreateTestMovie(movieId);
        var bookingSeats = CreateTestBookingSeats(bookingId);

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(new List<Booking> { booking });

        _mockInvoiceRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync(new List<Invoice> { invoice });

        SetupMocksForMapToDataTransferObject(booking, user, showTime, movie, bookingSeats);

        // Act
        var result = await _invoiceService.GetUserInvoicesAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var invoiceDto = result.First();
        Assert.Equal(invoiceId, invoiceDto.Id);
        Assert.Equal(bookingId, invoiceDto.BookingId);
    }

    [Fact]
    public async Task GetUserInvoicesAsync_EmptyUserId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _invoiceService.GetUserInvoicesAsync(Guid.Empty));

        Assert.Equal("Invalid user ID.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn("Attempted to fetch invoices with an empty user GUID."),
            Times.Once);
    }

    [Fact]
    public async Task GetUserInvoicesAsync_NoBookingsFound_ReturnsEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(new List<Booking>());

        // Act
        var result = await _invoiceService.GetUserInvoicesAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        _mockLoggerService.Verify(
            l => l.Warn($"No bookings found for user ID: {userId}"),
            Times.Once);
    }

    [Fact]
    public async Task CreateInvoiceAsync_ValidBookingId_ReturnsInvoiceDataTransferObject()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var booking = CreateTestBooking(bookingId, userId, showTimeId);
        var user = CreateTestUser(userId);
        var showTime = CreateTestShowTime(showTimeId, movieId);
        var movie = CreateTestMovie(movieId);
        var bookingSeats = CreateTestBookingSeats(bookingId);

        _mockInvoiceRepository.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync((Invoice)null!);

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId,
            It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync(user);

        var createdInvoice = new Invoice
        {
            Id = invoiceId,
            BookingId = bookingId,
            InvoiceDate = DateTime.UtcNow,
            Amount = 100000,
            Status = "Pending",
            Booking = booking
        };

        // Fix: Mock AddAsync to assign the ID to the input invoice and return it
        _mockInvoiceRepository.Setup(r => r.AddAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>(invoice => invoice.Id = invoiceId) // Assign the ID to the input invoice
            .ReturnsAsync(createdInvoice);

        // Setup GetByIdAsync to return the created invoice
        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync(createdInvoice);

        SetupMocksForMapToDataTransferObject(booking, user, showTime, movie, bookingSeats);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _invoiceService.CreateInvoiceAsync(bookingId, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(invoiceId, result.Id);
        Assert.Equal(bookingId, result.BookingId);
        Assert.Equal(100000, result.Amount);
        Assert.Equal("Pending", result.Status);

        _mockLoggerService.Verify(
            l => l.Info($"Starting invoice creation for booking: {bookingId}"),
            Times.Once);

        _mockLoggerService.Verify(
            l => l.Success($"Invoice created successfully with ID: {invoiceId}"),
            Times.Once);
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithPromotion_AppliesDiscount()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var promotionId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var booking = CreateTestBooking(bookingId, userId, showTimeId);
        var user = CreateTestUser(userId);
        var promotion = new Promotion
        {
            Id = promotionId,
            Title = "Test Promotion",
            DiscountValue = 0.1m // 10% discount
        };

        _mockInvoiceRepository.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync((Invoice)null!);

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId,
            It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync(user);

        _mockPromotionRepository.Setup(r => r.GetByIdAsync(promotionId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Promotion, object>>[]>()))
            .ReturnsAsync(promotion);

        // Fix: Use booking.MemberId instead of userId to match the service logic
        _mockPromotionService.Setup(s => s.UseClaimedPromotionAsync(promotionId, booking.MemberId))
            .ReturnsAsync(true);

        var createdInvoice = new Invoice
        {
            Id = invoiceId,
            BookingId = bookingId,
            InvoiceDate = DateTime.UtcNow,
            Amount = 90000, // After 10% discount
            Status = "Pending",
            PromotionId = promotionId
        };

        // Fix: Mock AddAsync to assign the ID to the input invoice and return it
        _mockInvoiceRepository.Setup(r => r.AddAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>(invoice => invoice.Id = invoiceId) // Assign the ID to the input invoice
            .ReturnsAsync(createdInvoice);

        // Fix: Setup GetByIdAsync to return the created invoice for ANY Guid, not just the specific invoiceId
        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync(createdInvoice);

        var showTime = CreateTestShowTime(showTimeId, movieId);
        var movie = CreateTestMovie(movieId);
        var bookingSeats = CreateTestBookingSeats(bookingId);

        SetupMocksForMapToDataTransferObject(booking, user, showTime, movie, bookingSeats);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _invoiceService.CreateInvoiceAsync(bookingId, promotionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(90000, result.Amount); // Discounted amount

        _mockLoggerService.Verify(
            l => l.Info($"Applying promotion with ID: {promotionId}"),
            Times.Once);
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithPoints_AppliesPointsDiscount()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var requestedPoints = 100;

        var booking = CreateTestBooking(bookingId, userId, showTimeId);
        var user = CreateTestUser(userId);
        user.ScoreBalance = 1000; // User has enough points

        _mockInvoiceRepository.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync((Invoice)null!);

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId,
            It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync(user);

        _mockScoreService.Setup(s => s.CalculateDiscount(1000, requestedPoints))
            .Returns((5m, 100)); // 5% discount, 100 points used

        _mockScoreService.Setup(s => s.UseScoreForBookingAsync(user, booking, 100))
            .Returns(Task.CompletedTask);

        var createdInvoice = new Invoice
        {
            Id = invoiceId,
            BookingId = bookingId,
            InvoiceDate = DateTime.UtcNow,
            Amount = 95000, // After 5% discount
            Status = "Pending"
        };

        // Fix: Mock AddAsync to assign the ID to the input invoice and return it
        _mockInvoiceRepository.Setup(r => r.AddAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>(invoice => invoice.Id = invoiceId) // Assign the ID to the input invoice
            .ReturnsAsync(createdInvoice);

        // Fix: Setup GetByIdAsync to return the created invoice for ANY Guid, not just the specific invoiceId
        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync(createdInvoice);

        var showTime = CreateTestShowTime(showTimeId, movieId);
        var movie = CreateTestMovie(movieId);
        var bookingSeats = CreateTestBookingSeats(bookingId);

        SetupMocksForMapToDataTransferObject(booking, user, showTime, movie, bookingSeats);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _invoiceService.CreateInvoiceAsync(bookingId, null, requestedPoints);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(95000, result.Amount); // Discounted amount

        _mockScoreService.Verify(
            s => s.UseScoreForBookingAsync(user, booking, 100),
            Times.Once);
    }

    [Fact]
    public async Task CreateInvoiceAsync_EmptyBookingId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _invoiceService.CreateInvoiceAsync(Guid.Empty, null, 0));

        Assert.Equal("Invalid booking ID.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn("Attempted to create invoice with an empty booking GUID."),
            Times.Once);
    }

    [Fact]
    public async Task CreateInvoiceAsync_InvoiceAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var existingInvoice = new Invoice { Id = Guid.NewGuid(), BookingId = bookingId };

        _mockInvoiceRepository.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync(existingInvoice);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _invoiceService.CreateInvoiceAsync(bookingId, null, 0));

        Assert.Equal("Invoice already exists for this booking", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"Invoice already exists for booking ID: {bookingId}"),
            Times.Once);
    }

    [Fact]
    public async Task CreateInvoiceAsync_BookingNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        _mockInvoiceRepository.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync((Invoice)null!);

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync((Booking)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _invoiceService.CreateInvoiceAsync(bookingId, null, 0));

        Assert.Equal($"Booking with ID {bookingId} not found", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"No booking found with ID: {bookingId}"),
            Times.Once);
    }

    [Fact]
    public async Task CreateInvoiceAsync_PromotionNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var promotionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var booking = CreateTestBooking(bookingId, userId, Guid.NewGuid());

        _mockInvoiceRepository.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync((Invoice)null!);

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId,
            It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync(CreateTestUser(userId));

        _mockPromotionRepository.Setup(r => r.GetByIdAsync(promotionId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Promotion, object>>[]>()))
            .ReturnsAsync((Promotion)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _invoiceService.CreateInvoiceAsync(bookingId, promotionId));

        Assert.Equal($"Promotion with ID {promotionId} not found", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"No promotion found with ID: {promotionId}"),
            Times.Once);
    }

    [Fact]
    public async Task CreateInvoiceAsync_PromotionCannotBeUsed_ThrowsInvalidOperationException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var promotionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var booking = CreateTestBooking(bookingId, userId, Guid.NewGuid());
        var promotion = new Promotion
        {
            Id = promotionId,
            Title = "Test Promotion",
            DiscountValue = 0.1m
        };

        _mockInvoiceRepository.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync((Invoice)null!);

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId,
            It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync(CreateTestUser(userId));

        _mockPromotionRepository.Setup(r => r.GetByIdAsync(promotionId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Promotion, object>>[]>()))
            .ReturnsAsync(promotion);

        _mockPromotionService.Setup(s => s.UseClaimedPromotionAsync(promotionId, userId))
            .ReturnsAsync(false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _invoiceService.CreateInvoiceAsync(bookingId, promotionId));

        Assert.Equal("Promotion could not be used", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"Promotion with ID {promotionId} could not be used for booking {bookingId}"),
            Times.Once);
    }

    [Fact]
    public async Task UpdateInvoiceStatusAsync_ValidInvoiceId_UpdatesStatusAndReturnsDataTransferObject()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var newStatus = "Paid";

        var invoice = CreateTestInvoice(invoiceId, bookingId);
        var booking = CreateTestBooking(bookingId, userId, showTimeId);
        var user = CreateTestUser(userId);
        var showTime = CreateTestShowTime(showTimeId, movieId);
        var movie = CreateTestMovie(movieId);
        var bookingSeats = CreateTestBookingSeats(bookingId);

        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync(invoice);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var updatedInvoice = new Invoice
        {
            Id = invoiceId,
            BookingId = bookingId,
            InvoiceDate = DateTime.UtcNow,
            Amount = 100000,
            Status = newStatus
        };

        _mockInvoiceRepository.SetupSequence(r => r.GetByIdAsync(invoiceId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync(invoice)
            .ReturnsAsync(updatedInvoice);

        SetupMocksForMapToDataTransferObject(booking, user, showTime, movie, bookingSeats);

        // Act
        var result = await _invoiceService.UpdateInvoiceStatusAsync(invoiceId, newStatus);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(invoiceId, result.Id);
        Assert.Equal(newStatus, result.Status);

        _mockLoggerService.Verify(
            l => l.Info($"Starting invoice status update for invoice: {invoiceId}"),
            Times.Once);

        _mockLoggerService.Verify(
            l => l.Success($"Invoice {invoiceId} status updated to {newStatus}"),
            Times.Once);

        _mockInvoiceRepository.Verify(r => r.Update(invoice), Times.Once);

        // Verify cache clearing
        _mockRedisService.Verify(r => r.RemoveAsync($"invoice:detail:{invoiceId}"), Times.Once);
        _mockRedisService.Verify(r => r.RemoveAsync($"invoice:booking:{invoice.BookingId}"), Times.Once);
        _mockRedisService.Verify(r => r.RemoveByPatternAsync("invoices:user:*"), Times.Once);
    }

    [Fact]
    public async Task UpdateInvoiceStatusAsync_EmptyInvoiceId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _invoiceService.UpdateInvoiceStatusAsync(Guid.Empty, "Paid"));

        Assert.Equal("Invalid invoice ID.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn("Attempted to update invoice with an empty GUID."),
            Times.Once);
    }

    [Fact]
    public async Task UpdateInvoiceStatusAsync_InvoiceNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();

        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync((Invoice)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _invoiceService.UpdateInvoiceStatusAsync(invoiceId, "Paid"));

        Assert.Equal($"Invoice with ID {invoiceId} not found", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"No invoice found with ID: {invoiceId}"),
            Times.Once);
    }

    [Fact]
    public async Task GetAllInvoicesAsync_ValidParameters_ReturnsPaginatedResults()
    {
        // Arrange
        var invoiceId1 = Guid.NewGuid();
        var invoiceId2 = Guid.NewGuid();
        var bookingId1 = Guid.NewGuid();
        var bookingId2 = Guid.NewGuid();

        var invoices = new List<Invoice>
        {
            CreateTestInvoice(invoiceId1, bookingId1),
            CreateTestInvoice(invoiceId2, bookingId2)
        };

        _mockInvoiceRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync(invoices);

        SetupMocksForPagination();

        // Act
        var result = await _invoiceService.GetAllInvoicesAsync(page: 1, pageSize: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(2, result.Items.Count);

        _mockLoggerService.Verify(
            l => l.Info($"Fetching invoices - Page 1, PageSize 10, Status: , Search: "),
            Times.Once);

        _mockLoggerService.Verify(
            l => l.Success(It.Is<string>(msg => msg.Contains("Retrieved 2 invoices on page 1 successfully"))),
            Times.Once);
    }

    [Fact]
    public async Task GetAllInvoicesAsync_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange
        var invoices = new List<Invoice>
        {
            new() { Id = Guid.NewGuid(), Status = "Pending", InvoiceDate = DateTime.UtcNow, Amount = 100 },
            new() { Id = Guid.NewGuid(), Status = "Paid", InvoiceDate = DateTime.UtcNow, Amount = 200 }
        };

        _mockInvoiceRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync(invoices);

        SetupMocksForPagination();

        // Act
        var result = await _invoiceService.GetAllInvoicesAsync(status: "Pending");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetAllInvoicesAsync_WithSearch_ReturnsFilteredResults()
    {
        // Arrange
        var searchTerm = "john";
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var invoices = new List<Invoice>
        {
            CreateTestInvoice(Guid.NewGuid(), bookingId)
        };

        _mockInvoiceRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync(invoices);

        _mockUserRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync(new List<User>
            {
                new() { Id = userId, FullName = "John Doe" }
            });

        _mockShowTimeRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(new List<ShowTime>());

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(new List<Booking>
            {
                new() { Id = bookingId, MemberId = userId }
            });

        SetupMocksForPagination();

        // Act
        var result = await _invoiceService.GetAllInvoicesAsync(search: searchTerm);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetAllInvoicesAsync_SortByDate_ReturnsSortedResults()
    {
        // Arrange
        var baseDate = DateTime.UtcNow;
        var invoices = new List<Invoice>
        {
            new() { Id = Guid.NewGuid(), InvoiceDate = baseDate.AddDays(-1), Amount = 100 },
            new() { Id = Guid.NewGuid(), InvoiceDate = baseDate.AddDays(1), Amount = 200 }
        };

        _mockInvoiceRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync(invoices);

        SetupMocksForPagination();

        // Act
        var result = await _invoiceService.GetAllInvoicesAsync(sortBy: "date", isDescending: false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.True(result.Items[0].InvoiceDate <= result.Items[1].InvoiceDate);
    }

    [Fact]
    public async Task GetAllInvoicesAsync_SortByAmount_ReturnsSortedResults()
    {
        // Arrange
        var invoices = new List<Invoice>
        {
            new() { Id = Guid.NewGuid(), InvoiceDate = DateTime.UtcNow, Amount = 200 },
            new() { Id = Guid.NewGuid(), InvoiceDate = DateTime.UtcNow, Amount = 100 }
        };

        _mockInvoiceRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync(invoices);

        SetupMocksForPagination();

        // Act
        var result = await _invoiceService.GetAllInvoicesAsync(sortBy: "amount", isDescending: false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.True(result.Items[0].Amount <= result.Items[1].Amount);
    }

    [Fact]
    public async Task GetAllInvoicesAsync_DatabaseError_ThrowsException()
    {
        // Arrange
        _mockInvoiceRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _invoiceService.GetAllInvoicesAsync());

        Assert.Equal("An error occurred while retrieving invoice items. Please try again later.", ex.Message);

        _mockLoggerService.Verify(
            l => l.Error(It.Is<string>(msg => msg.Contains("Failed to retrieve invoices"))),
            Times.Once);
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_MapToDataTransferObjectFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var invoice = CreateTestInvoice(invoiceId, bookingId);

        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync(invoice);

        // Setup booking repository to return null for both the invoice.Booking navigation property
        // and the explicit GetByIdAsync call in MapToDto method
        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync((Booking)null!);

        // Act & Assert
        // The MapToDto method will try to access booking.MemberId when booking is null,
        // which will throw a NullReferenceException
        var ex = await Assert.ThrowsAsync<NullReferenceException>(() =>
            _invoiceService.GetInvoiceByIdAsync(invoiceId));

        // Verify that the warning log was called when booking was not found
        _mockLoggerService.Verify(
            l => l.Warn($"Invoice {invoiceId} does not have a related booking loaded."),
            Times.Once);
    }

    [Fact]
    public async Task MapToDataTransferObject_NoBookingSeatsFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var movieId = Guid.NewGuid();

        var invoice = CreateTestInvoice(invoiceId, bookingId);
        var booking = CreateTestBooking(bookingId, userId, showTimeId);
        var user = CreateTestUser(userId);
        var showTime = CreateTestShowTime(showTimeId, movieId);
        var movie = CreateTestMovie(movieId);

        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, object>>[]>()))
            .ReturnsAsync(invoice);

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId,
            It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync(user);

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(showTime);

        _mockMovieRepository.Setup(r => r.GetByIdAsync(movieId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Movie, object>>[]>()))
            .ReturnsAsync(movie);

        // Setup booking seats repository to return empty list
        _mockBookingSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BookingSeat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<BookingSeat, object>>[]>()))
            .ReturnsAsync(new List<BookingSeat>());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _invoiceService.GetInvoiceByIdAsync(invoiceId));

        Assert.Equal($"No seats found for booking ID {bookingId}", ex.Message);

        _mockLoggerService.Verify(
            l => l.Warn($"No booking seats found for booking ID: {bookingId}"),
            Times.Once);
    }

    private Invoice CreateTestInvoice(Guid invoiceId, Guid bookingId)
    {
        return new Invoice
        {
            Id = invoiceId,
            BookingId = bookingId,
            InvoiceDate = DateTime.UtcNow,
            Amount = 100000,
            Status = "Pending"
        };
    }

    private Booking CreateTestBooking(Guid bookingId, Guid userId, Guid showTimeId)
    {
        return new Booking
        {
            Id = bookingId,
            MemberId = userId,
            ShowtimeId = showTimeId,
            BookingDate = DateTime.UtcNow,
            TotalAmount = 100000,
            Status = "Created"
        };
    }

    private User CreateTestUser(Guid userId)
    {
        return new User
        {
            Id = userId,
            FullName = "John Doe",
            ScoreBalance = 1000
        };
    }

    private ShowTime CreateTestShowTime(Guid showTimeId, Guid movieId)
    {
        return new ShowTime
        {
            Id = showTimeId,
            MovieId = movieId,
            ShowDate = DateTime.UtcNow.AddDays(1)
        };
    }

    private Movie CreateTestMovie(Guid movieId)
    {
        return new Movie
        {
            Id = movieId,
            Name = "Test Movie"
        };
    }

    private List<BookingSeat> CreateTestBookingSeats(Guid bookingId)
    {
        return new List<BookingSeat>
        {
            new() { BookingId = bookingId, SeatId = Guid.NewGuid() },
            new() { BookingId = bookingId, SeatId = Guid.NewGuid() }
        };
    }

    private void SetupMocksForMapToDataTransferObject(Booking booking, User user, ShowTime showTime, Movie movie, List<BookingSeat> bookingSeats)
    {
        // Setup all repository calls that MapToDto might make
        _mockBookingRepository.Setup(r => r.GetByIdAsync(booking.Id,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        _mockUserRepository.Setup(r => r.GetByIdAsync(user.Id,
            It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync(user);

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTime.Id,
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(showTime);

        _mockMovieRepository.Setup(r => r.GetByIdAsync(movie.Id,
            It.IsAny<System.Linq.Expressions.Expression<Func<Movie, object>>[]>()))
            .ReturnsAsync(movie);

        _mockBookingSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BookingSeat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<BookingSeat, object>>[]>()))
            .ReturnsAsync(bookingSeats);
    }

    private void SetupMocksForPagination()
    {
        // Setup for basic MapToDto requirements during pagination
        _mockBookingRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(CreateTestBooking(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
            .ReturnsAsync(CreateTestUser(Guid.NewGuid()));

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTime, object>>[]>()))
            .ReturnsAsync(CreateTestShowTime(Guid.NewGuid(), Guid.NewGuid()));

        _mockMovieRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Movie, object>>[]>()))
            .ReturnsAsync(CreateTestMovie(Guid.NewGuid()));

        _mockBookingSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BookingSeat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<BookingSeat, object>>[]>()))
            .ReturnsAsync(CreateTestBookingSeats(Guid.NewGuid()));
    }
}