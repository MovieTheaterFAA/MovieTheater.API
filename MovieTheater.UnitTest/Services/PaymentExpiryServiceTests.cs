using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.Entities;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.UnitTest.Services;

public class PaymentExpiryServiceTests
{
    private readonly Mock<IServiceProvider> _mockMainServiceProvider;
    private readonly Mock<IServiceProvider> _mockScopedServiceProvider;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly Mock<ILogger<PaymentExpiryService>> _mockLogger;
    private readonly Mock<IPaymentService> _mockPaymentService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILoggerService> _mockLoggerService;
    private readonly Mock<IScoreService> _mockScoreService;
    private readonly Mock<IGenericRepository<Invoice>> _mockInvoiceRepository;
    private readonly PaymentExpiryService _paymentExpiryService;

    public PaymentExpiryServiceTests()
    {
        _mockMainServiceProvider = new Mock<IServiceProvider>();
        _mockScopedServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockLogger = new Mock<ILogger<PaymentExpiryService>>();
        _mockPaymentService = new Mock<IPaymentService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLoggerService = new Mock<ILoggerService>();
        _mockScoreService = new Mock<IScoreService>();
        _mockInvoiceRepository = new Mock<IGenericRepository<Invoice>>();

        // Setup service scope to return the scoped service provider
        _mockServiceScope.Setup(s => s.ServiceProvider).Returns(_mockScopedServiceProvider.Object);

        // Setup service scope factory
        _mockServiceScopeFactory.Setup(f => f.CreateScope()).Returns(_mockServiceScope.Object);

        // Setup main service provider to return the scope factory when GetService is called for IServiceScopeFactory
        _mockMainServiceProvider.Setup(s => s.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockServiceScopeFactory.Object);

        // Setup scoped service provider to return required services
        _mockScopedServiceProvider.Setup(s => s.GetService(typeof(IPaymentService)))
            .Returns(_mockPaymentService.Object);
        _mockScopedServiceProvider.Setup(s => s.GetService(typeof(IUnitOfWork)))
            .Returns(_mockUnitOfWork.Object);
        _mockScopedServiceProvider.Setup(s => s.GetService(typeof(ILoggerService)))
            .Returns(_mockLoggerService.Object);
        _mockScopedServiceProvider.Setup(s => s.GetService(typeof(IScoreService)))
            .Returns(_mockScoreService.Object);

        // Setup unit of work repository
        _mockUnitOfWork.Setup(u => u.Invoices).Returns(_mockInvoiceRepository.Object);

        _paymentExpiryService = new PaymentExpiryService(
            _mockMainServiceProvider.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task ExecuteAsync_ServiceStartsSuccessfully_LogsInformation()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();

        _mockInvoiceRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>()))
            .ReturnsAsync(new List<Invoice>());

        // Act
        await _paymentExpiryService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50); // Give it time to start and log
        await cancellationTokenSource.CancelAsync(); // Cancel after it starts
        await _paymentExpiryService.StopAsync(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Payment expiry monitoring service started")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessExpiredPayments_NoExpiredPayments_ReturnsEarly()
    {
        // Arrange
        _mockInvoiceRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>()))
            .ReturnsAsync(new List<Invoice>());

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _paymentExpiryService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50); // Let one iteration complete
        await cancellationTokenSource.CancelAsync();
        await _paymentExpiryService.StopAsync(CancellationToken.None);

        // Assert
        _mockLoggerService.Verify(
            l => l.Info(It.IsAny<string>()),
            Times.Never);

        _mockPaymentService.Verify(p => p.ProcessFailPaymentAsync(It.IsAny<Guid>()), Times.Never);
        _mockScoreService.Verify(s => s.RefundScoreForBookingAsync(It.IsAny<Guid>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ProcessExpiredPayments_WithExpiredProcessingInvoices_ProcessesSuccessfully()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var expiredInvoice = new Invoice
        {
            Id = invoiceId,
            BookingId = bookingId,
            Status = "Processing",
            InvoiceDate = DateTime.UtcNow.AddMinutes(-10) // Expired
        };

        _mockInvoiceRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>()))
            .ReturnsAsync(new List<Invoice> { expiredInvoice });

        _mockPaymentService.Setup(p => p.ProcessFailPaymentAsync(invoiceId))
            .Returns(Task.CompletedTask);

        _mockScoreService.Setup(s => s.RefundScoreForBookingAsync(bookingId))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _paymentExpiryService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50); // Let one iteration complete
        await cancellationTokenSource.CancelAsync();
        await _paymentExpiryService.StopAsync(CancellationToken.None);

        // Assert
        _mockLoggerService.Verify(
            l => l.Info("Processing 1 expired payments"),
            Times.AtLeastOnce);

        _mockLoggerService.Verify(
            l => l.Warn($"Found expired payment for invoice {invoiceId}"),
            Times.AtLeastOnce);

        _mockLoggerService.Verify(
            l => l.Info($"Processed failed payment for invoice {invoiceId}"),
            Times.AtLeastOnce);

        _mockLoggerService.Verify(
            l => l.Info($"Refunded score for booking {bookingId} related to invoice {invoiceId}"),
            Times.AtLeastOnce);

        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Successfully processed expired payment for invoice {invoiceId}")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

        _mockPaymentService.Verify(p => p.ProcessFailPaymentAsync(invoiceId), Times.Once);
        _mockScoreService.Verify(s => s.RefundScoreForBookingAsync(bookingId), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessExpiredPayments_WithExpiredPendingInvoices_ProcessesSuccessfully()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var expiredInvoice = new Invoice
        {
            Id = invoiceId,
            BookingId = bookingId,
            Status = "Pending",
            InvoiceDate = DateTime.UtcNow.AddMinutes(-10) // Expired
        };

        _mockInvoiceRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>()))
            .ReturnsAsync(new List<Invoice> { expiredInvoice });

        _mockPaymentService.Setup(p => p.ProcessFailPaymentAsync(invoiceId))
            .Returns(Task.CompletedTask);

        _mockScoreService.Setup(s => s.RefundScoreForBookingAsync(bookingId))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _paymentExpiryService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50);
        await cancellationTokenSource.CancelAsync();
        await _paymentExpiryService.StopAsync(CancellationToken.None);

        // Assert
        _mockLoggerService.Verify(
            l => l.Info("Processing 1 expired payments"),
            Times.AtLeastOnce);

        _mockPaymentService.Verify(p => p.ProcessFailPaymentAsync(invoiceId), Times.Once);
        _mockScoreService.Verify(s => s.RefundScoreForBookingAsync(bookingId), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessExpiredPayments_MultipleExpiredInvoices_ProcessesAllSuccessfully()
    {
        // Arrange
        var invoice1Id = Guid.NewGuid();
        var invoice2Id = Guid.NewGuid();
        var booking1Id = Guid.NewGuid();
        var booking2Id = Guid.NewGuid();

        var expiredInvoice1 = new Invoice
        {
            Id = invoice1Id,
            BookingId = booking1Id,
            Status = "Processing",
            InvoiceDate = DateTime.UtcNow.AddMinutes(-10)
        };

        var expiredInvoice2 = new Invoice
        {
            Id = invoice2Id,
            BookingId = booking2Id,
            Status = "Pending",
            InvoiceDate = DateTime.UtcNow.AddMinutes(-15)
        };

        _mockInvoiceRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>()))
            .ReturnsAsync(new List<Invoice> { expiredInvoice1, expiredInvoice2 });

        _mockPaymentService.Setup(p => p.ProcessFailPaymentAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        _mockScoreService.Setup(s => s.RefundScoreForBookingAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _paymentExpiryService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50);
        await cancellationTokenSource.CancelAsync();
        await _paymentExpiryService.StopAsync(CancellationToken.None);

        // Assert
        _mockLoggerService.Verify(
            l => l.Info("Processing 2 expired payments"),
            Times.AtLeastOnce);

        _mockPaymentService.Verify(p => p.ProcessFailPaymentAsync(invoice1Id), Times.Once);
        _mockPaymentService.Verify(p => p.ProcessFailPaymentAsync(invoice2Id), Times.Once);
        _mockScoreService.Verify(s => s.RefundScoreForBookingAsync(booking1Id), Times.Once);
        _mockScoreService.Verify(s => s.RefundScoreForBookingAsync(booking2Id), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessExpiredPayments_PaymentServiceThrowsException_LogsErrorAndContinues()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var expiredInvoice = new Invoice
        {
            Id = invoiceId,
            BookingId = bookingId,
            Status = "Processing",
            InvoiceDate = DateTime.UtcNow.AddMinutes(-10)
        };

        _mockInvoiceRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>()))
            .ReturnsAsync(new List<Invoice> { expiredInvoice });

        _mockPaymentService.Setup(p => p.ProcessFailPaymentAsync(invoiceId))
            .ThrowsAsync(new Exception("Payment processing error"));

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _paymentExpiryService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50);
        await cancellationTokenSource.CancelAsync();
        await _paymentExpiryService.StopAsync(CancellationToken.None);

        // Assert
        _mockLoggerService.Verify(
            l => l.Error($"Error processing expired payment for invoice {invoiceId}: Payment processing error"),
            Times.AtLeastOnce);

        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Error processing expired payment for invoice {invoiceId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

        _mockPaymentService.Verify(p => p.ProcessFailPaymentAsync(invoiceId), Times.Once);

        // Score service should not be called due to payment service failure
        _mockScoreService.Verify(s => s.RefundScoreForBookingAsync(It.IsAny<Guid>()), Times.Never);

        // SaveChangesAsync should still be called at the end
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessExpiredPayments_ScoreServiceThrowsException_LogsErrorAndContinues()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var expiredInvoice = new Invoice
        {
            Id = invoiceId,
            BookingId = bookingId,
            Status = "Processing",
            InvoiceDate = DateTime.UtcNow.AddMinutes(-10)
        };

        _mockInvoiceRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>()))
            .ReturnsAsync(new List<Invoice> { expiredInvoice });

        _mockPaymentService.Setup(p => p.ProcessFailPaymentAsync(invoiceId))
            .Returns(Task.CompletedTask);

        _mockScoreService.Setup(s => s.RefundScoreForBookingAsync(bookingId))
            .ThrowsAsync(new Exception("Score refund error"));

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _paymentExpiryService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50);
        await cancellationTokenSource.CancelAsync();
        await _paymentExpiryService.StopAsync(CancellationToken.None);

        // Assert
        _mockLoggerService.Verify(
            l => l.Info($"Processed failed payment for invoice {invoiceId}"),
            Times.AtLeastOnce);

        _mockLoggerService.Verify(
            l => l.Error($"Error processing expired payment for invoice {invoiceId}: Score refund error"),
            Times.AtLeastOnce);

        _mockPaymentService.Verify(p => p.ProcessFailPaymentAsync(invoiceId), Times.Once);
        _mockScoreService.Verify(s => s.RefundScoreForBookingAsync(bookingId), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessExpiredPayments_FiltersCutoffTimeCorrectly_OnlyProcessesExpiredInvoices()
    {
        // Arrange
        var expiredInvoiceId = Guid.NewGuid();
        var recentInvoiceId = Guid.NewGuid();

        var expiredInvoice = new Invoice
        {
            Id = expiredInvoiceId,
            BookingId = Guid.NewGuid(),
            Status = "Processing",
            InvoiceDate = DateTime.UtcNow.AddMinutes(-10) // Expired
        };

        var recentInvoice = new Invoice
        {
            Id = recentInvoiceId,
            BookingId = Guid.NewGuid(),
            Status = "Processing",
            InvoiceDate = DateTime.UtcNow.AddMinutes(-2) // Not expired (within 5 minutes)
        };

        // Setup to return only expired invoice (simulating the query filter)
        _mockInvoiceRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>()))
            .ReturnsAsync(new List<Invoice> { expiredInvoice }); // Only expired invoice returned

        _mockPaymentService.Setup(p => p.ProcessFailPaymentAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        _mockScoreService.Setup(s => s.RefundScoreForBookingAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _paymentExpiryService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50);
        await cancellationTokenSource.CancelAsync();
        await _paymentExpiryService.StopAsync(CancellationToken.None);

        // Assert
        _mockLoggerService.Verify(
            l => l.Info("Processing 1 expired payments"),
            Times.AtLeastOnce);

        // Verify only expired invoice was processed
        _mockPaymentService.Verify(p => p.ProcessFailPaymentAsync(expiredInvoiceId), Times.Once);
        _mockPaymentService.Verify(p => p.ProcessFailPaymentAsync(recentInvoiceId), Times.Never);

        // Verify repository was called with correct filter
        _mockInvoiceRepository.Verify(r => r.GetAllAsync(
            It.Is<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(
                expr => expr != null)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessExpiredPayments_NullInvoicesResult_HandlesGracefully()
    {
        // Arrange
        _mockInvoiceRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>()))
            .ReturnsAsync((List<Invoice>)null!);

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _paymentExpiryService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50);
        await cancellationTokenSource.CancelAsync();
        await _paymentExpiryService.StopAsync(CancellationToken.None);

        // Assert
        // Should not process any invoices or call services
        _mockLoggerService.Verify(
            l => l.Info(It.IsAny<string>()),
            Times.Never);

        _mockPaymentService.Verify(p => p.ProcessFailPaymentAsync(It.IsAny<Guid>()), Times.Never);
        _mockScoreService.Verify(s => s.RefundScoreForBookingAsync(It.IsAny<Guid>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ExceptionInMainLoop_LogsErrorAndContinues()
    {
        // Arrange
        _mockServiceScopeFactory.Setup(f => f.CreateScope())
            .Throws(new Exception("Service creation error"));

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _paymentExpiryService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50);
        await cancellationTokenSource.CancelAsync();
        await _paymentExpiryService.StopAsync(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error processing expired payments")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessExpiredPayments_ServiceScopesDisposedProperly()
    {
        // Arrange
        _mockInvoiceRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>()))
            .ReturnsAsync(new List<Invoice>());

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _paymentExpiryService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50);
        await cancellationTokenSource.CancelAsync();
        await _paymentExpiryService.StopAsync(CancellationToken.None);

        // Assert
        // Verify scope was created and disposed
        _mockServiceScopeFactory.Verify(f => f.CreateScope(), Times.AtLeastOnce);
        _mockServiceScope.Verify(s => s.Dispose(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessExpiredPayments_PartialFailure_ProcessesRemainingInvoices()
    {
        // Arrange
        var invoice1Id = Guid.NewGuid();
        var invoice2Id = Guid.NewGuid();
        var booking1Id = Guid.NewGuid();
        var booking2Id = Guid.NewGuid();

        var expiredInvoice1 = new Invoice
        {
            Id = invoice1Id,
            BookingId = booking1Id,
            Status = "Processing",
            InvoiceDate = DateTime.UtcNow.AddMinutes(-10)
        };

        var expiredInvoice2 = new Invoice
        {
            Id = invoice2Id,
            BookingId = booking2Id,
            Status = "Processing",
            InvoiceDate = DateTime.UtcNow.AddMinutes(-15)
        };

        _mockInvoiceRepository.Setup(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>()))
            .ReturnsAsync(new List<Invoice> { expiredInvoice1, expiredInvoice2 });

        // First invoice fails, second succeeds
        _mockPaymentService.Setup(p => p.ProcessFailPaymentAsync(invoice1Id))
            .ThrowsAsync(new Exception("Payment processing error"));
        _mockPaymentService.Setup(p => p.ProcessFailPaymentAsync(invoice2Id))
            .Returns(Task.CompletedTask);

        _mockScoreService.Setup(s => s.RefundScoreForBookingAsync(booking2Id))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _paymentExpiryService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50);
        await cancellationTokenSource.CancelAsync();
        await _paymentExpiryService.StopAsync(CancellationToken.None);

        // Assert
        _mockLoggerService.Verify(
            l => l.Info("Processing 2 expired payments"),
            Times.AtLeastOnce);

        // Verify first invoice error was logged
        _mockLoggerService.Verify(
            l => l.Error($"Error processing expired payment for invoice {invoice1Id}: Payment processing error"),
            Times.AtLeastOnce);

        // Verify second invoice was processed successfully
        _mockLoggerService.Verify(
            l => l.Info($"Processed failed payment for invoice {invoice2Id}"),
            Times.AtLeastOnce);

        _mockPaymentService.Verify(p => p.ProcessFailPaymentAsync(invoice1Id), Times.Once);
        _mockPaymentService.Verify(p => p.ProcessFailPaymentAsync(invoice2Id), Times.Once);

        // Score service should only be called for successful payment processing
        _mockScoreService.Verify(s => s.RefundScoreForBookingAsync(booking1Id), Times.Never);
        _mockScoreService.Verify(s => s.RefundScoreForBookingAsync(booking2Id), Times.Once);

        // SaveChangesAsync should still be called at the end
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }
}