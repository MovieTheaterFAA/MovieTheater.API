using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Application.Services
{
    public class BookingCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BookingCleanupService> _logger;

        public BookingCleanupService(
            IServiceProvider serviceProvider,
            ILogger<BookingCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Booking cleanup service is running");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var loggerService = scope.ServiceProvider.GetRequiredService<ILoggerService>();

                    // Clean abandoned bookings with pending payments
                    await CleanAbandonedBookings(unitOfWork, loggerService);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in booking cleanup service");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private static async Task CleanAbandonedBookings(IUnitOfWork unitOfWork, ILoggerService loggerService)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-5);

            var abandonedInvoices = await unitOfWork.Invoices.GetAllAsync(
                i => i.Status == "Pending" && i.InvoiceDate < cutoffTime,
                i => i.Booking);

            if (abandonedInvoices == null || !abandonedInvoices.Any())
                return;

            loggerService.Info($"Processing {abandonedInvoices.Count} abandoned bookings");

            foreach (var invoice in abandonedInvoices)
            {
                if (invoice.Booking == null)
                {
                    loggerService.Warn($"Invoice {invoice.Id} has no associated booking, skipping");
                    continue;
                }

                try
                {
                    // Get seats to release
                    var bookingSeats = await unitOfWork.BookingSeats.GetAllAsync(
                        bs => bs.BookingId == invoice.BookingId);

                    if (bookingSeats == null || !bookingSeats.Any())
                    {
                        loggerService.Warn($"No seats found for booking {invoice.BookingId}, skipping seat cleanup");
                    }
                    else
                    {
                        var seatIds = bookingSeats.Select(bs => bs.SeatId).ToList();

                        // Release the seats
                        var showTimeSeats = await unitOfWork.ShowTimeSeats.GetAllAsync(
                            sts => sts.ShowTimeId == invoice.Booking.ShowtimeId &&
                                  seatIds.Contains(sts.SeatId));

                        if (showTimeSeats != null && showTimeSeats.Any())
                        {
                            foreach (var seat in showTimeSeats)
                            {
                                seat.Status = SeatStatus.Available;
                            }

                            await unitOfWork.ShowTimeSeats.UpdateRange(showTimeSeats.ToList());
                        }
                    }

                    // Mark booking and invoice as cancelled
                    invoice.Status = "Cancelled";
                    invoice.Booking.Status = "Cancelled";

                    await unitOfWork.Invoices.Update(invoice);
                    await unitOfWork.Bookings.Update(invoice.Booking);
                }
                catch (Exception ex)
                {
                    loggerService.Error($"Error processing abandoned booking {invoice.BookingId}: {ex.Message}");
                }
            }

            await unitOfWork.SaveChangesAsync();
            loggerService.Success($"Processed {abandonedInvoices.Count} abandoned bookings");
        }
    }
}
