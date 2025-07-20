using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Application.Services
{
    public class PaymentExpiryService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaymentExpiryService> _logger;

        public PaymentExpiryService(
            IServiceProvider serviceProvider,
            ILogger<PaymentExpiryService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Payment expiry monitoring service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredPayments();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing expired payments: {Message}", ex.Message);
                }

                // Run every 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task ProcessExpiredPayments()
        {
            using var scope = _serviceProvider.CreateScope();
            var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var loggerService = scope.ServiceProvider.GetRequiredService<ILoggerService>();
            var scoreService = scope.ServiceProvider.GetRequiredService<IScoreService>();

            // Use the same cutoff logic as BookingCleanupService
            var cutoffTime = DateTime.UtcNow.AddMinutes(-5);

            // Find invoices in "Processing" status that are older than cutoff
            var expiredInvoices = await unitOfWork.Invoices.GetAllAsync(
                i => (i.Status == "Processing" || i.Status == "Pending") && i.InvoiceDate < cutoffTime);

            if (expiredInvoices == null || !expiredInvoices.Any())
                return;

            loggerService.Info($"Processing {expiredInvoices.Count} expired payments");

            foreach (var invoice in expiredInvoices)
            {
                try
                {
                    loggerService.Warn($"Found expired payment for invoice {invoice.Id}");

                    await paymentService.ProcessFailPaymentAsync(invoice.Id);
                    loggerService.Info($"Processed failed payment for invoice {invoice.Id}");

                    await scoreService.RefundScoreForBookingAsync(invoice.BookingId);
                    loggerService.Info($"Refunded score for booking {invoice.BookingId} related to invoice {invoice.Id}");

                    _logger.LogInformation("Successfully processed expired payment for invoice {InvoiceId}", invoice.Id);
                }
                catch (Exception ex)
                {
                    loggerService.Error($"Error processing expired payment for invoice {invoice.Id}: {ex.Message}");
                    _logger.LogError(ex, "Error processing expired payment for invoice {InvoiceId}", invoice.Id);
                }
            }

            await unitOfWork.SaveChangesAsync();
        }
    }
}
