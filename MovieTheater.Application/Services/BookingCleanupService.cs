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

                    // Clean expired seat reservations
                    //await CleanExpiredReservations(unitOfWork, loggerService);

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

        //private async Task CleanExpiredReservations(IUnitOfWork unitOfWork, ILoggerService loggerService)
        //{
        //    var now = DateTime.UtcNow;
        //    var expiredReservations = await unitOfWork.ShowTimeSeats.GetAllAsync(
        //        s => s.Status == SeatStatus.Reserved && s.ReservationExpiry < now);

        //    if (!expiredReservations.Any())
        //        return;

        //    loggerService.Info($"Cleaning {expiredReservations.Count} expired seat reservations");

        //    foreach (var seat in expiredReservations)
        //    {
        //        seat.Status = SeatStatus.Available;
        //        seat.ReservationCode = null;
        //        seat.ReservationExpiry = null;
        //    }

        //    await unitOfWork.ShowTimeSeats.UpdateRange(expiredReservations.ToList());
        //    await unitOfWork.SaveChangesAsync();

        //    loggerService.Success($"Cleaned {expiredReservations.Count} expired seat reservations");
        //}

        private async Task CleanAbandonedBookings(IUnitOfWork unitOfWork, ILoggerService loggerService)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-30);

            var abandonedInvoices = await unitOfWork.Invoices.GetAllAsync(
                i => i.Status == "Pending" && i.InvoiceDate < cutoffTime,
                i => i.Booking);

            if (!abandonedInvoices.Any())
                return;

            loggerService.Info($"Processing {abandonedInvoices.Count} abandoned bookings");

            foreach (var invoice in abandonedInvoices)
            {
                // Get seats to release
                var bookingSeats = await unitOfWork.BookingSeats.GetAllAsync(
                    bs => bs.BookingId == invoice.BookingId);

                var seatIds = bookingSeats.Select(bs => bs.SeatId).ToList();

                // Release the seats
                var showTimeSeats = await unitOfWork.ShowTimeSeats.GetAllAsync(
                    sts => sts.ShowTimeId == invoice.Booking.ShowtimeId &&
                          seatIds.Contains(sts.SeatId));

                foreach (var seat in showTimeSeats)
                {
                    seat.Status = SeatStatus.Available;
                }

                await unitOfWork.ShowTimeSeats.UpdateRange(showTimeSeats.ToList());

                // Mark booking and invoice as cancelled
                invoice.Status = "Cancelled";
                invoice.Booking.Status = "Cancelled";

                await unitOfWork.Invoices.Update(invoice);
                await unitOfWork.Bookings.Update(invoice.Booking);
            }

            await unitOfWork.SaveChangesAsync();
            loggerService.Success($"Processed {abandonedInvoices.Count} abandoned bookings");
        }
    }
}
