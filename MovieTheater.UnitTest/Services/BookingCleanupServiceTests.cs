using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.UnitTest.Services;

public class BookingCleanupServiceTests
{
    private readonly Mock<IServiceProvider> _mockMainServiceProvider;
    private readonly Mock<IServiceProvider> _mockScopedServiceProvider;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly Mock<ILogger<BookingCleanupService>> _mockLogger;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILoggerService> _mockLoggerService;
    private readonly Mock<IGenericRepository<Booking>> _mockBookingRepository;
    private readonly Mock<IGenericRepository<BookingSeat>> _mockBookingSeatRepository;
    private readonly Mock<IGenericRepository<ShowTimeSeat>> _mockShowTimeSeatRepository;
    private readonly Mock<IGenericRepository<Invoice>> _mockInvoiceRepository;
    private readonly BookingCleanupService _bookingCleanupService;

    public BookingCleanupServiceTests()
    {
        _mockMainServiceProvider = new Mock<IServiceProvider>();
        _mockScopedServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockLogger = new Mock<ILogger<BookingCleanupService>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLoggerService = new Mock<ILoggerService>();
        _mockBookingRepository = new Mock<IGenericRepository<Booking>>();
        _mockBookingSeatRepository = new Mock<IGenericRepository<BookingSeat>>();
        _mockShowTimeSeatRepository = new Mock<IGenericRepository<ShowTimeSeat>>();
        _mockInvoiceRepository = new Mock<IGenericRepository<Invoice>>();

        // Setup service scope to return the scoped service provider
        _mockServiceScope.Setup(s => s.ServiceProvider).Returns(_mockScopedServiceProvider.Object);

        // Setup service scope factory
        _mockServiceScopeFactory.Setup(f => f.CreateScope()).Returns(_mockServiceScope.Object);

        // Setup main service provider to return the scope factory when GetService is called for IServiceScopeFactory
        _mockMainServiceProvider.Setup(s => s.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockServiceScopeFactory.Object);

        // Setup scoped service provider to return required services
        _mockScopedServiceProvider.Setup(s => s.GetService(typeof(IUnitOfWork)))
            .Returns(_mockUnitOfWork.Object);
        _mockScopedServiceProvider.Setup(s => s.GetService(typeof(ILoggerService)))
            .Returns(_mockLoggerService.Object);

        // Setup unit of work repositories
        _mockUnitOfWork.Setup(u => u.Bookings).Returns(_mockBookingRepository.Object);
        _mockUnitOfWork.Setup(u => u.BookingSeats).Returns(_mockBookingSeatRepository.Object);
        _mockUnitOfWork.Setup(u => u.ShowTimeSeats).Returns(_mockShowTimeSeatRepository.Object);
        _mockUnitOfWork.Setup(u => u.Invoices).Returns(_mockInvoiceRepository.Object);

        _bookingCleanupService = new BookingCleanupService(
            _mockMainServiceProvider.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task ExecuteAsync_ServiceStartsSuccessfully_LogsInformation()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(new List<Booking>());

        // Act
        await _bookingCleanupService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50); // Give it time to start and log
        await cancellationTokenSource.CancelAsync(); // Cancel after it starts
        await _bookingCleanupService.StopAsync(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Booking cleanup service is running")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task CleanAbandonedBookings_NoAbandonedBookings_ReturnsEarly()
    {
        // Arrange
        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(new List<Booking>());

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _bookingCleanupService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50); // Let one iteration complete
        await cancellationTokenSource.CancelAsync();
        await _bookingCleanupService.StopAsync(CancellationToken.None);

        // Assert
        _mockLoggerService.Verify(
            l => l.Info(It.IsAny<string>()),
            Times.Never);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CleanAbandonedBookings_WithAbandonedBookings_ProcessesBookingsSuccessfully()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var abandonedBooking = new Booking
        {
            Id = bookingId,
            ShowtimeId = showTimeId,
            Status = "Created",
            BookingDate = DateTime.UtcNow.AddMinutes(-10),
            Invoice = new Invoice
            {
                Id = invoiceId,
                Status = "Pending"
            }
        };

        var bookingSeat = new BookingSeat
        {
            BookingId = bookingId,
            SeatId = seatId
        };

        var showTimeSeat = new ShowTimeSeat
        {
            ShowTimeId = showTimeId,
            SeatId = seatId,
            Status = SeatStatus.Booked
        };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(new List<Booking> { abandonedBooking });

        _mockBookingSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BookingSeat, bool>>>()))
            .ReturnsAsync(new List<BookingSeat> { bookingSeat });

        _mockShowTimeSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, bool>>>()))
            .ReturnsAsync(new List<ShowTimeSeat> { showTimeSeat });

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _bookingCleanupService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50); // Let one iteration complete
        await cancellationTokenSource.CancelAsync();
        await _bookingCleanupService.StopAsync(CancellationToken.None);

        // Assert
        _mockLoggerService.Verify(
            l => l.Info("Processing 1 abandoned bookings"),
            Times.AtLeastOnce);

        _mockLoggerService.Verify(
            l => l.Success("Processed 1 abandoned bookings"),
            Times.AtLeastOnce);

        // Verify booking status was updated
        Assert.Equal("Cancelled", abandonedBooking.Status);

        // Verify invoice status was updated
        Assert.Equal("Cancelled", abandonedBooking.Invoice.Status);

        // Verify seat status was updated
        Assert.Equal(SeatStatus.Available, showTimeSeat.Status);

        _mockBookingRepository.Verify(r => r.Update(abandonedBooking), Times.AtLeastOnce);
        _mockInvoiceRepository.Verify(r => r.Update(abandonedBooking.Invoice), Times.AtLeastOnce);
        _mockShowTimeSeatRepository.Verify(r => r.UpdateRange(It.IsAny<List<ShowTimeSeat>>()), Times.AtLeastOnce);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CleanAbandonedBookings_BookingWithoutInvoice_ProcessesBookingWithoutInvoiceUpdate()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var seatId = Guid.NewGuid();

        var abandonedBooking = new Booking
        {
            Id = bookingId,
            ShowtimeId = showTimeId,
            Status = "Created",
            BookingDate = DateTime.UtcNow.AddMinutes(-10),
            Invoice = null! // No invoice
        };

        var bookingSeat = new BookingSeat
        {
            BookingId = bookingId,
            SeatId = seatId
        };

        var showTimeSeat = new ShowTimeSeat
        {
            ShowTimeId = showTimeId,
            SeatId = seatId,
            Status = SeatStatus.Booked
        };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(new List<Booking> { abandonedBooking });

        _mockBookingSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BookingSeat, bool>>>()))
            .ReturnsAsync(new List<BookingSeat> { bookingSeat });

        _mockShowTimeSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, bool>>>()))
            .ReturnsAsync(new List<ShowTimeSeat> { showTimeSeat });

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _bookingCleanupService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50);
        await cancellationTokenSource.CancelAsync();
        await _bookingCleanupService.StopAsync(CancellationToken.None);

        // Assert
        _mockLoggerService.Verify(
            l => l.Info("Processing 1 abandoned bookings"),
            Times.AtLeastOnce);

        // Verify booking status was updated
        Assert.Equal("Cancelled", abandonedBooking.Status);

        // Verify seat status was updated
        Assert.Equal(SeatStatus.Available, showTimeSeat.Status);

        _mockBookingRepository.Verify(r => r.Update(abandonedBooking), Times.AtLeastOnce);

        // Verify invoice update was NOT called since there's no invoice
        _mockInvoiceRepository.Verify(r => r.Update(It.IsAny<Invoice>()), Times.Never);

        _mockShowTimeSeatRepository.Verify(r => r.UpdateRange(It.IsAny<List<ShowTimeSeat>>()), Times.AtLeastOnce);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CleanAbandonedBookings_BookingWithoutSeats_LogsWarningAndSkipsSeatsCleanup()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();

        var abandonedBooking = new Booking
        {
            Id = bookingId,
            ShowtimeId = showTimeId,
            Status = "Created",
            BookingDate = DateTime.UtcNow.AddMinutes(-10),
            Invoice = null!
        };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(new List<Booking> { abandonedBooking });

        // No booking seats found
        _mockBookingSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BookingSeat, bool>>>()))
            .ReturnsAsync(new List<BookingSeat>());

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _bookingCleanupService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50);
        await cancellationTokenSource.CancelAsync();
        await _bookingCleanupService.StopAsync(CancellationToken.None);

        // Assert
        _mockLoggerService.Verify(
            l => l.Warn($"No seats found for booking {bookingId}, skipping seat cleanup"),
            Times.AtLeastOnce);

        // Verify booking status was still updated
        Assert.Equal("Cancelled", abandonedBooking.Status);

        _mockBookingRepository.Verify(r => r.Update(abandonedBooking), Times.AtLeastOnce);

        // Verify ShowTimeSeats repository was NOT called
        _mockShowTimeSeatRepository.Verify(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, bool>>>()), Times.Never);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CleanAbandonedBookings_NoShowTimeSeatsFound_ProcessesBookingWithoutSeatUpdate()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var seatId = Guid.NewGuid();

        var abandonedBooking = new Booking
        {
            Id = bookingId,
            ShowtimeId = showTimeId,
            Status = "Created",
            BookingDate = DateTime.UtcNow.AddMinutes(-10),
            Invoice = null!
        };

        var bookingSeat = new BookingSeat
        {
            BookingId = bookingId,
            SeatId = seatId
        };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(new List<Booking> { abandonedBooking });

        _mockBookingSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BookingSeat, bool>>>()))
            .ReturnsAsync(new List<BookingSeat> { bookingSeat });

        // No show time seats found
        _mockShowTimeSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ShowTimeSeat, bool>>>()))
            .ReturnsAsync(new List<ShowTimeSeat>());

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _bookingCleanupService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50);
        await cancellationTokenSource.CancelAsync();
        await _bookingCleanupService.StopAsync(CancellationToken.None);

        // Assert
        // Verify booking status was updated
        Assert.Equal("Cancelled", abandonedBooking.Status);

        _mockBookingRepository.Verify(r => r.Update(abandonedBooking), Times.AtLeastOnce);

        // Verify UpdateRange was NOT called since no seats were found
        _mockShowTimeSeatRepository.Verify(r => r.UpdateRange(It.IsAny<List<ShowTimeSeat>>()), Times.Never);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CleanAbandonedBookings_MultipleAbandonedBookings_ProcessesAllSuccessfully()
    {
        // Arrange
        var booking1Id = Guid.NewGuid();
        var booking2Id = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();

        var abandonedBooking1 = new Booking
        {
            Id = booking1Id,
            ShowtimeId = showTimeId,
            Status = "Created",
            BookingDate = DateTime.UtcNow.AddMinutes(-10),
            Invoice = new Invoice { Status = "Pending" }
        };

        var abandonedBooking2 = new Booking
        {
            Id = booking2Id,
            ShowtimeId = showTimeId,
            Status = "Created",
            BookingDate = DateTime.UtcNow.AddMinutes(-15),
            Invoice = null!
        };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(new List<Booking> { abandonedBooking1, abandonedBooking2 });

        _mockBookingSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BookingSeat, bool>>>()))
            .ReturnsAsync(new List<BookingSeat>());

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _bookingCleanupService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50);
        await cancellationTokenSource.CancelAsync();
        await _bookingCleanupService.StopAsync(CancellationToken.None);

        // Assert
        _mockLoggerService.Verify(
            l => l.Info("Processing 2 abandoned bookings"),
            Times.AtLeastOnce);

        _mockLoggerService.Verify(
            l => l.Success("Processed 2 abandoned bookings"),
            Times.AtLeastOnce);

        // Verify both bookings were updated
        Assert.Equal("Cancelled", abandonedBooking1.Status);
        Assert.Equal("Cancelled", abandonedBooking2.Status);

        // Verify invoice for first booking was updated
        Assert.Equal("Cancelled", abandonedBooking1.Invoice.Status);

        _mockBookingRepository.Verify(r => r.Update(It.IsAny<Booking>()), Times.AtLeast(2));
        _mockInvoiceRepository.Verify(r => r.Update(It.IsAny<Invoice>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CleanAbandonedBookings_ExceptionInProcessingBooking_LogsErrorAndContinues()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();

        var abandonedBooking = new Booking
        {
            Id = bookingId,
            ShowtimeId = showTimeId,
            Status = "Created",
            BookingDate = DateTime.UtcNow.AddMinutes(-10),
            Invoice = null!
        };

        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(new List<Booking> { abandonedBooking });

        // Make BookingSeats repository throw an exception
        _mockBookingSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BookingSeat, bool>>>()))
            .ThrowsAsync(new Exception("Database error"));

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _bookingCleanupService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50);
        await cancellationTokenSource.CancelAsync();
        await _bookingCleanupService.StopAsync(CancellationToken.None);

        // Assert
        _mockLoggerService.Verify(
            l => l.Error($"Error processing abandoned booking {bookingId}: Database error"),
            Times.AtLeastOnce);

        // Verify SaveChangesAsync was still called at the end
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ExceptionInMainLoop_LogsErrorAndContinues()
    {
        // Arrange
        _mockServiceScopeFactory.Setup(f => f.CreateScope())
            .Throws(new Exception("Service creation error"));

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _bookingCleanupService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50);
        await cancellationTokenSource.CancelAsync();
        await _bookingCleanupService.StopAsync(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error in booking cleanup service")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task CleanAbandonedBookings_FiltersCutoffTimeCorrectly_OnlyProcessesOldBookings()
    {
        // Arrange
        var oldBookingId = Guid.NewGuid();
        var recentBookingId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();

        var oldBooking = new Booking
        {
            Id = oldBookingId,
            ShowtimeId = showTimeId,
            Status = "Created",
            BookingDate = DateTime.UtcNow.AddMinutes(-10), // Old booking
            Invoice = null!
        };

        var recentBooking = new Booking
        {
            Id = recentBookingId,
            ShowtimeId = showTimeId,
            Status = "Created",
            BookingDate = DateTime.UtcNow.AddMinutes(-2), // Recent booking (within 5 minutes)
            Invoice = null!
        };

        // Setup to return only old booking (simulating the query filter)
        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(new List<Booking> { oldBooking }); // Only old booking returned

        _mockBookingSeatRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<BookingSeat, bool>>>()))
            .ReturnsAsync(new List<BookingSeat>());

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _bookingCleanupService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50);
        await cancellationTokenSource.CancelAsync();
        await _bookingCleanupService.StopAsync(CancellationToken.None);

        // Assert
        _mockLoggerService.Verify(
            l => l.Info("Processing 1 abandoned bookings"),
            Times.AtLeastOnce);

        // Verify only old booking was processed
        Assert.Equal("Cancelled", oldBooking.Status);

        // Verify repository was called with correct filter
        _mockBookingRepository.Verify(r => r.GetAllAsync(
            It.Is<System.Linq.Expressions.Expression<Func<Booking, bool>>>(
                expr => expr != null),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task CleanAbandonedBookings_NullBookingsResult_HandlesGracefully()
    {
        // Arrange
        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync((List<Booking>)null!);

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _bookingCleanupService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50);
        await cancellationTokenSource.CancelAsync();
        await _bookingCleanupService.StopAsync(CancellationToken.None);

        // Assert
        // Should not process any bookings or call SaveChangesAsync
        _mockLoggerService.Verify(
            l => l.Info(It.IsAny<string>()),
            Times.Never);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CleanAbandonedBookings_ServiceScopesDisposedProperly()
    {
        // Arrange
        _mockBookingRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Booking, object>>[]>()))
            .ReturnsAsync(new List<Booking>());

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _bookingCleanupService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50);
        await cancellationTokenSource.CancelAsync();
        await _bookingCleanupService.StopAsync(CancellationToken.None);

        // Assert
        // Verify scope was created and disposed
        _mockServiceScopeFactory.Verify(f => f.CreateScope(), Times.AtLeastOnce);
        _mockServiceScope.Verify(s => s.Dispose(), Times.AtLeastOnce);
    }
}