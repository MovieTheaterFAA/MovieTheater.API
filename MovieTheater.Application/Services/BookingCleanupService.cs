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

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private static async Task CleanAbandonedBookings(IUnitOfWork unitOfWork, ILoggerService loggerService)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-5);

            // Find bookings that are pending and older than cutoff
            var abandonedBookings = await unitOfWork.Bookings.GetAllAsync(
                b => b.Status == "Created" && b.BookingDate < cutoffTime,
                b => b.Invoice, b => b.Showtime);

            if (abandonedBookings == null || !abandonedBookings.Any())
                return;

            loggerService.Info($"Processing {abandonedBookings.Count} abandoned bookings");

            foreach (var booking in abandonedBookings)
            {
                try
                {
                    // Get seats to release
                    var bookingSeats = await unitOfWork.BookingSeats.GetAllAsync(
                        bs => bs.BookingId == booking.Id);

                    if (bookingSeats == null || !bookingSeats.Any())
                    {
                        loggerService.Warn($"No seats found for booking {booking.Id}, skipping seat cleanup");
                    }
                    else
                    {
                        var seatIds = bookingSeats.Select(bs => bs.SeatId).ToList();

                        // Release the seats
                        var showTimeSeats = await unitOfWork.ShowTimeSeats.GetAllAsync(
                            sts => sts.ShowTimeId == booking.ShowtimeId &&
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

                    // Mark booking as cancelled
                    booking.Status = "Cancelled";
                    await unitOfWork.Bookings.Update(booking);

                    // If booking has an invoice, cancel it too
                    if (booking.Invoice != null)
                    {
                        booking.Invoice.Status = "Cancelled";
                        await unitOfWork.Invoices.Update(booking.Invoice);
                    }
                }
                catch (Exception ex)
                {
                    loggerService.Error($"Error processing abandoned booking {booking.Id}: {ex.Message}");
                }
            }

            await unitOfWork.SaveChangesAsync();
            loggerService.Success($"Processed {abandonedBookings.Count} abandoned bookings");
        }
    }
}
