using Microsoft.Extensions.Configuration;
using Moq;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.DTOs.TicketDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace MovieTheater.UnitTest.Services;

public class StripePaymentServiceTests
{
    private readonly Mock<ILoggerService> _mockLoggerService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IRedisService> _mockRedisService;
    private readonly Mock<IStripeClient> _mockStripeClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ITicketService> _mockTicketService;
    private readonly Mock<IGenericRepository<Domain.Entities.Invoice>> _mockInvoiceRepository;
    private readonly Mock<IGenericRepository<Booking>> _mockBookingRepository;
    private readonly Mock<IGenericRepository<Payment>> _mockPaymentRepository;
    private readonly Mock<IGenericRepository<BookingSeat>> _mockBookingSeatRepository;
    private readonly Mock<IGenericRepository<ShowTimeSeat>> _mockShowTimeSeatRepository;
    private readonly StripePaymentService _stripePaymentService;

    public StripePaymentServiceTests()
    {
        _mockLoggerService = new Mock<ILoggerService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockRedisService = new Mock<IRedisService>();
        _mockStripeClient = new Mock<IStripeClient>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockTicketService = new Mock<ITicketService>();
        _mockInvoiceRepository = new Mock<IGenericRepository<Domain.Entities.Invoice>>();
        _mockBookingRepository = new Mock<IGenericRepository<Booking>>();
        _mockPaymentRepository = new Mock<IGenericRepository<Payment>>();
        _mockBookingSeatRepository = new Mock<IGenericRepository<BookingSeat>>();
        _mockShowTimeSeatRepository = new Mock<IGenericRepository<ShowTimeSeat>>();

        _mockUnitOfWork.Setup(u => u.Invoices).Returns(_mockInvoiceRepository.Object);
        _mockUnitOfWork.Setup(u => u.Bookings).Returns(_mockBookingRepository.Object);
        _mockUnitOfWork.Setup(u => u.Payments).Returns(_mockPaymentRepository.Object);
        _mockUnitOfWork.Setup(u => u.BookingSeats).Returns(_mockBookingSeatRepository.Object);
        _mockUnitOfWork.Setup(u => u.ShowTimeSeats).Returns(_mockShowTimeSeatRepository.Object);

        _stripePaymentService = new StripePaymentService(
            _mockLoggerService.Object,
            _mockUnitOfWork.Object,
            _mockRedisService.Object,
            _mockStripeClient.Object,
            _mockConfiguration.Object,
            _mockTicketService.Object
        );
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_ValidInvoiceId_ReturnsCheckoutUrl()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var showtimeId = Guid.NewGuid();
        var movieId = Guid.NewGuid();

        var invoice = CreateTestInvoice(invoiceId, bookingId, 100000);
        var booking = CreateTestBooking(bookingId, showtimeId);
        var showtime = CreateTestShowtime(showtimeId, movieId);
        var movie = CreateTestMovie(movieId, "Test Movie");

        booking.Showtime = showtime;
        showtime.Movie = movie;

        var expectedSession = new Session
        {
            Id = "sess_test123",
            Url = "https://checkout.stripe.com/pay/test123"
        };

        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Invoice, object>>[]>()))
            .ReturnsAsync(invoice);

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        // Fix: Mock the SessionService.CreateAsync method directly through IStripeClient
        _mockStripeClient.Setup(c => c.RequestAsync<Session>(
            It.IsAny<HttpMethod>(),
            It.IsAny<string>(),
            It.IsAny<BaseOptions>(),
            It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSession);

        // Act
        var result = await _stripePaymentService.CreateCheckoutSessionAsync(invoiceId);

        // Assert
        Assert.Equal(expectedSession.Url, result);
        _mockLoggerService.Verify(l => l.Info($"Creating Stripe checkout session for invoice: {invoiceId}"), Times.Once);
        _mockLoggerService.Verify(l => l.Success($"Stripe checkout session created successfully: {expectedSession.Id}"), Times.Once);
        _mockRedisService.Verify(r => r.SetAsync($"stripe:session:{expectedSession.Id}", invoiceId.ToString(), TimeSpan.FromHours(1)), Times.Once);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_InvoiceNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();

        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Invoice, object>>[]>()))
            .ReturnsAsync((Domain.Entities.Invoice)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _stripePaymentService.CreateCheckoutSessionAsync(invoiceId));

        Assert.Equal($"Invoice with ID {invoiceId} not found", ex.Message);
        _mockLoggerService.Verify(l => l.Warn($"Invoice {invoiceId} not found"), Times.Once);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_BookingNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var invoice = CreateTestInvoice(invoiceId, bookingId, 100000);

        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Invoice, object>>[]>()))
            .ReturnsAsync(invoice);

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync((Booking)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _stripePaymentService.CreateCheckoutSessionAsync(invoiceId));

        Assert.Equal($"Booking for invoice with ID {invoiceId} not found", ex.Message);
        _mockLoggerService.Verify(l => l.Warn($"Booking for invoice {invoiceId} not found"), Times.Once);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_ShowtimeNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var invoice = CreateTestInvoice(invoiceId, bookingId, 100000);
        var booking = CreateTestBooking(bookingId, Guid.NewGuid());

        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Invoice, object>>[]>()))
            .ReturnsAsync(invoice);

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _stripePaymentService.CreateCheckoutSessionAsync(invoiceId));

        Assert.Equal($"Showtime for booking {booking.Id} not found", ex.Message);
        _mockLoggerService.Verify(l => l.Warn($"Showtime for booking {booking.Id} not found"), Times.Once);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_MovieNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var showtimeId = Guid.NewGuid();
        var invoice = CreateTestInvoice(invoiceId, bookingId, 100000);
        var booking = CreateTestBooking(bookingId, showtimeId);
        var showtime = CreateTestShowtime(showtimeId, Guid.NewGuid());

        booking.Showtime = showtime;

        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Invoice, object>>[]>()))
            .ReturnsAsync(invoice);

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _stripePaymentService.CreateCheckoutSessionAsync(invoiceId));

        Assert.Equal($"Movie for showtime {booking.ShowtimeId} not found", ex.Message);
        _mockLoggerService.Verify(l => l.Warn($"Movie for showtime {booking.ShowtimeId} not found"), Times.Once);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_NegativeAmount_ThrowsArgumentException()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var showtimeId = Guid.NewGuid();
        var movieId = Guid.NewGuid();

        var invoice = CreateTestInvoice(invoiceId, bookingId, -100);
        var booking = CreateTestBooking(bookingId, showtimeId);
        var showtime = CreateTestShowtime(showtimeId, movieId);
        var movie = CreateTestMovie(movieId, "Test Movie");

        booking.Showtime = showtime;
        showtime.Movie = movie;

        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Invoice, object>>[]>()))
            .ReturnsAsync(invoice);

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _stripePaymentService.CreateCheckoutSessionAsync(invoiceId));

        Assert.Equal("Payment amount must be greater than zero", ex.Message);
    }

    [Fact]
    public async Task VerifyPaymentAsync_ValidPaidSession_ReturnsTrue()
    {
        // Arrange
        var sessionId = "sess_test123";
        var invoiceId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var session = new Session
        {
            Id = sessionId,
            PaymentStatus = "paid",
            Metadata = new Dictionary<string, string>
        {
            { "invoiceId", invoiceId.ToString() }
        }
        };

        var invoice = CreateTestInvoice(invoiceId, bookingId, 100000);
        var booking = CreateTestBooking(bookingId, Guid.NewGuid());
        invoice.Booking = booking;

        // Fix: Mock the SessionService.GetAsync method directly through IStripeClient
        _mockStripeClient.Setup(c => c.RequestAsync<Session>(
            HttpMethod.Get,
            $"/v1/checkout/sessions/{sessionId}",
            It.IsAny<BaseOptions>(),
            It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Invoice, object>>[]>()))
            .ReturnsAsync(invoice);

        _mockPaymentRepository.Setup(r => r.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Payment, object>>[]>()))
            .ReturnsAsync((Payment)null!);

        // Mock additional repository operations for ProcessSuccessfulPaymentAsync
        _mockBookingSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BookingSeat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<BookingSeat, object>>[]>()))
            .ReturnsAsync(new List<BookingSeat>
            {
            new() { BookingId = bookingId, SeatId = Guid.NewGuid() }
            });

        _mockShowTimeSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, object>>[]>()))
            .ReturnsAsync(new List<ShowTimeSeat>
            {
            new() { ShowTimeId = booking.ShowtimeId, SeatId = Guid.NewGuid(), Status = SeatStatus.Available }
            });

        // Mock Redis and other services
        _mockRedisService.Setup(r => r.RemoveAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTicketService.Setup(t => t.GenerateTicketFromBookingAsync(booking.Id))
            .ReturnsAsync(new TicketResponseDto { MovieName = "Test Movie", MoviePosterUrl = "test-url" });

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _stripePaymentService.VerifyPaymentAsync(sessionId);

        // Assert
        Assert.True(result);
        _mockLoggerService.Verify(l => l.Info($"Verifying Stripe payment for session: {sessionId}"), Times.Once);
        _mockLoggerService.Verify(l => l.Success($"Payment verified successfully for session: {sessionId}"), Times.Once);
        _mockTicketService.Verify(t => t.GenerateTicketFromBookingAsync(booking.Id), Times.Once);
    }

    [Fact]
    public async Task VerifyPaymentAsync_UnpaidSession_ReturnsFalse()
    {
        // Arrange
        var sessionId = "sess_test123";

        var session = new Session
        {
            Id = sessionId,
            PaymentStatus = "unpaid"
        };

        // Fix: Mock the IStripeClient.RequestAsync method to return the unpaid session
        _mockStripeClient.Setup(c => c.RequestAsync<Session>(
            HttpMethod.Get,
            $"/v1/checkout/sessions/{sessionId}",
            It.IsAny<BaseOptions>(),
            It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _stripePaymentService.VerifyPaymentAsync(sessionId);

        // Assert
        Assert.False(result);
        _mockLoggerService.Verify(l => l.Warn($"Payment verification failed for session: {sessionId}, status: unpaid"), Times.Once);
    }

    [Fact]
    public async Task VerifyPaymentAsync_NullOrEmptySessionId_ReturnsFalse()
    {
        // Act & Assert
        var result1 = await _stripePaymentService.VerifyPaymentAsync(null!);
        var result2 = await _stripePaymentService.VerifyPaymentAsync(string.Empty);

        Assert.False(result1);
        Assert.False(result2);
        _mockLoggerService.Verify(l => l.Warn("Attempted to verify payment with null or empty session ID"), Times.Exactly(2));
    }

    [Fact]
    public async Task VerifyPaymentAsync_StripeException_ReturnsFalse()
    {
        // Arrange
        var sessionId = "sess_test123";
        var stripeError = new StripeError { Message = "Session not found" };
        var stripeException = new StripeException { StripeError = stripeError };

        // Fix: Mock the IStripeClient.RequestAsync method to throw the StripeException
        _mockStripeClient.Setup(c => c.RequestAsync<Session>(
            HttpMethod.Get,
            $"/v1/checkout/sessions/{sessionId}",
            It.IsAny<BaseOptions>(),
            It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(stripeException);

        // Act
        var result = await _stripePaymentService.VerifyPaymentAsync(sessionId);

        // Assert
        Assert.False(result);
        _mockLoggerService.Verify(l => l.Error($"Stripe API error during verification: {stripeError.Message}"), Times.Once);
    }

    [Fact]
    public async Task InitiatePaymentAsync_ValidInvoiceId_ReturnsCheckoutUrl()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var showtimeId = Guid.NewGuid();
        var movieId = Guid.NewGuid();

        var invoice = CreateTestInvoice(invoiceId, bookingId, 100000);
        var booking = CreateTestBooking(bookingId, showtimeId);
        var showtime = CreateTestShowtime(showtimeId, movieId);
        var movie = CreateTestMovie(movieId, "Test Movie");

        invoice.Booking = booking;
        booking.Showtime = showtime;
        showtime.Movie = movie;

        var expectedSession = new Session
        {
            Id = "sess_test123",
            Url = "https://checkout.stripe.com/pay/test123"
        };

        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Invoice, object>>[]>()))
            .ReturnsAsync(invoice);

        _mockBookingRepository.Setup(r => r.GetByIdAsync(bookingId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(booking);

        // Fix: Mock the IStripeClient.RequestAsync method to return the expected session
        _mockStripeClient.Setup(c => c.RequestAsync<Session>(
            HttpMethod.Post,
            "/v1/checkout/sessions",
            It.IsAny<BaseOptions>(),
            It.IsAny<RequestOptions>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSession);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _stripePaymentService.InitiatePaymentAsync(invoiceId);

        // Assert
        Assert.Equal(expectedSession.Url, result);
        _mockLoggerService.Verify(l => l.Info($"Initiating payment for invoice {invoiceId}"), Times.Once);
        _mockLoggerService.Verify(l => l.Success($"Payment initiated for invoice {invoiceId}"), Times.Once);
        _mockInvoiceRepository.Verify(r => r.Update(It.Is<Domain.Entities.Invoice>(i => i.Status == "Processing")), Times.Once);
    }

    [Fact]
    public async Task InitiatePaymentAsync_InvoiceNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();

        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Invoice, object>>[]>()))
            .ReturnsAsync((Domain.Entities.Invoice)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _stripePaymentService.InitiatePaymentAsync(invoiceId));

        Assert.Equal($"Invoice with ID {invoiceId} not found", ex.Message);
        _mockLoggerService.Verify(l => l.Warn($"Invoice {invoiceId} not found"), Times.Once);
    }

    [Fact]
    public async Task InitiatePaymentAsync_InvoiceAlreadyPaid_ThrowsInvalidOperationException()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoice = CreateTestInvoice(invoiceId, Guid.NewGuid(), 100000);
        invoice.Status = "Paid";

        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Invoice, object>>[]>()))
            .ReturnsAsync(invoice);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _stripePaymentService.InitiatePaymentAsync(invoiceId));

        Assert.Equal($"Invoice {invoiceId} is already paid", ex.Message);
        _mockLoggerService.Verify(l => l.Warn($"Invoice {invoiceId} is already paid"), Times.Once);
    }

    [Fact]
    public async Task ProcessFailPaymentAsync_ValidInvoiceId_UpdatesStatusesToFailed()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var showtimeId = Guid.NewGuid();
        var seatId1 = Guid.NewGuid();
        var seatId2 = Guid.NewGuid();

        var invoice = CreateTestInvoice(invoiceId, bookingId, 100000);
        var booking = CreateTestBooking(bookingId, showtimeId);
        invoice.Booking = booking;

        var bookingSeats = new List<BookingSeat>
        {
            new() { BookingId = bookingId, SeatId = seatId1 },
            new() { BookingId = bookingId, SeatId = seatId2 }
        };

        var showTimeSeats = new List<ShowTimeSeat>
        {
            new() { ShowTimeId = showtimeId, SeatId = seatId1, Status = SeatStatus.Available },
            new() { ShowTimeId = showtimeId, SeatId = seatId2, Status = SeatStatus.Available }
        };

        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Invoice, object>>[]>()))
            .ReturnsAsync(invoice);

        _mockBookingSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BookingSeat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<BookingSeat, object>>[]>()))
            .ReturnsAsync(bookingSeats);

        _mockShowTimeSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, object>>[]>()))
            .ReturnsAsync(showTimeSeats);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _stripePaymentService.ProcessFailPaymentAsync(invoiceId);

        // Assert
        _mockLoggerService.Verify(l => l.Info($"Processing failed payment for invoice {invoiceId}"), Times.Once);
        _mockLoggerService.Verify(l => l.Warn($"Payment failed for invoice {invoiceId}"), Times.Once);
        _mockInvoiceRepository.Verify(r => r.Update(It.Is<Domain.Entities.Invoice>(i => i.Status == "Failed")), Times.Once);
        _mockBookingRepository.Verify(r => r.Update(It.Is<Booking>(b => b.Status == "PaymentFailed")), Times.Once);
        _mockShowTimeSeatRepository.Verify(r => r.Update(It.Is<ShowTimeSeat>(s => s.Status == SeatStatus.Available)), Times.Exactly(2));
        _mockRedisService.Verify(r => r.RemoveAsync($"payment:expiry:{invoiceId}"), Times.Once);
    }

    [Fact]
    public async Task ProcessFailPaymentAsync_InvoiceNotFound_ReturnsWithoutError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();

        _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId,
            It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Invoice, object>>[]>()))
            .ReturnsAsync((Domain.Entities.Invoice)null!);

        // Act
        await _stripePaymentService.ProcessFailPaymentAsync(invoiceId);

        // Assert
        _mockLoggerService.Verify(l => l.Info($"Processing failed payment for invoice {invoiceId}"), Times.Once);
        _mockLoggerService.Verify(l => l.Warn($"Invoice {invoiceId} not found during payment failure processing"), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    private Domain.Entities.Invoice CreateTestInvoice(Guid invoiceId, Guid bookingId, decimal amount)
    {
        return new Domain.Entities.Invoice
        {
            Id = invoiceId,
            BookingId = bookingId,
            Amount = amount,
            Status = "Pending",
            InvoiceDate = DateTime.UtcNow
        };
    }

    private Booking CreateTestBooking(Guid bookingId, Guid showtimeId)
    {
        return new Booking
        {
            Id = bookingId,
            ShowtimeId = showtimeId,
            Status = "Created",
            BookingDate = DateTime.UtcNow,
            MemberId = Guid.NewGuid(),
            TotalAmount = 100000
        };
    }

    private ShowTime CreateTestShowtime(Guid showtimeId, Guid movieId)
    {
        return new ShowTime
        {
            Id = showtimeId,
            MovieId = movieId,
            ShowDate = DateTime.UtcNow.AddDays(1),
            CinemaRoomId = Guid.NewGuid()
        };
    }

    private Movie CreateTestMovie(Guid movieId, string name)
    {
        return new Movie
        {
            Id = movieId,
            Name = name,
            Description = "Test movie description",
            Rating = 4.5f,
            Status = MovieStatus.NowShowing,
            Director = "Test Director",
            Genres = new List<string> { "Action", "Drama" },
            RunningTime = 120,
            FromDate = DateTime.UtcNow.AddMonths(-1)
        };
    }

}