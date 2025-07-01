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
            var redisService = scope.ServiceProvider.GetRequiredService<IRedisService>();

            // Get all keys that match the payment expiry pattern
            var keys = await redisService.GetKeysByPatternAsync("payment:expiry:*");

            if (keys == null || !keys.Any())
            {
                return;
            }

            foreach (var key in keys)
            {
                try
                {
                    // Get the expiry time
                    var expiryTime = await redisService.GetAsync<DateTime>(key);
                    if (expiryTime <= DateTime.UtcNow)
                    {
                        // Extract invoice ID from key - Fixed S6608 warning
                        var parts = key.Split(':');
                        var invoiceIdStr = parts[parts.Length - 1];

                        if (Guid.TryParse(invoiceIdStr, out Guid invoiceId))
                        {
                            // Check if invoice is still in "Processing" status
                            var invoice = await unitOfWork.Invoices.GetByIdAsync(invoiceId);
                            if (invoice != null && invoice.Status == "Processing")
                            {
                                loggerService.Warn($"Found expired payment for invoice {invoiceId}");
                                await paymentService.ProcessFailPaymentAsync(invoiceId);
                                _logger.LogInformation("Successfully processed expired payment for invoice {InvoiceId}", invoiceId);
                            }

                            // Remove the expiry key
                            await redisService.RemoveAsync(key);
                        }
                    }
                }
                catch (Exception ex)
                {
                    loggerService.Error($"Error processing expired payment key {key}: {ex.Message}");
                    _logger.LogError(ex, "Error processing payment key {Key}", key);
                }
            }
        }
    }
}
