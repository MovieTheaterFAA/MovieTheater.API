using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.BookingDTOs;
using MovieTheater.Domain.DTOs.InvoiceDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Application.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _loggerService;
        private readonly IRedisService _redisService;

        public InvoiceService(IUnitOfWork unitOfWork, ILoggerService loggerService, IRedisService redisService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
            _redisService = redisService;
        }

        public async Task<InvoiceDto> GetInvoiceByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                _loggerService.Warn("Attempted to fetch invoice with an empty GUID.");
                throw new ArgumentException("Invalid invoice ID.");
            }

            try
            {
                var invoice = await _unitOfWork.Invoices.GetByIdAsync(id);

                if (invoice == null)
                {
                    _loggerService.Warn($"No invoice found with ID: {id}");
                    throw new KeyNotFoundException($"Invoice with ID {id} not found");
                }

                var result = await MapToDto(invoice);
                return result;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"An unexpected error occurred while fetching invoice details for ID {id}: {ex.Message}");
                throw;
            }
        }

        public async Task<InvoiceDto> GetInvoiceByBookingIdAsync(Guid bookingId)
        {
            if (bookingId == Guid.Empty)
            {
                _loggerService.Warn("Attempted to fetch invoice with an empty booking GUID.");
                throw new ArgumentException("Invalid booking ID.");
            }

            try
            {
                string cacheKey = $"invoice:booking:{bookingId}";
                var cached = await _redisService.GetAsync<InvoiceDto>(cacheKey);
                if (cached != null) return cached;

                var invoice = await _unitOfWork.Invoices.FirstOrDefaultAsync(i => i.BookingId == bookingId);

                if (invoice == null)
                {
                    _loggerService.Warn($"No invoice found for booking ID: {bookingId}");
                    return null;
                }

                var result = await MapToDto(invoice);
                await _redisService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
                return result;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"An unexpected error occurred while fetching invoice for booking ID {bookingId}: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<InvoiceDto>> GetUserInvoicesAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                _loggerService.Warn("Attempted to fetch invoices with an empty user GUID.");
                throw new ArgumentException("Invalid user ID.");
            }

            try
            {
                string cacheKey = $"invoices:user:{userId}";
                var cached = await _redisService.GetAsync<IEnumerable<InvoiceDto>>(cacheKey);
                if (cached != null) return cached;

                // Get all bookings for the user
                var bookings = await _unitOfWork.Bookings.GetAllAsync(b => b.MemberId == userId);
                var bookingIds = bookings.Select(b => b.Id).ToList();

                // Get all invoices for those bookings
                var invoices = await _unitOfWork.Invoices.GetAllAsync(
                    i => bookingIds.Contains(i.BookingId));

                var tasks = invoices.Select(MapToDto);
                var result = await Task.WhenAll(tasks);

                await _redisService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
                return result;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"An unexpected error occurred while fetching invoices for user ID {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task<InvoiceDto> CreateInvoiceAsync(Guid bookingId)
        {
            if (bookingId == Guid.Empty)
            {
                _loggerService.Warn("Attempted to create invoice with an empty booking GUID.");
                throw new ArgumentException("Invalid booking ID.");
            }

            try
            {
                _loggerService.Info($"Starting invoice creation for booking: {bookingId}");

                // Check if invoice already exists for this booking
                var existingInvoice = await _unitOfWork.Invoices.FirstOrDefaultAsync(i => i.BookingId == bookingId);
                if (existingInvoice != null)
                {
                    _loggerService.Warn($"Invoice already exists for booking ID: {bookingId}");
                    throw new InvalidOperationException("Invoice already exists for this booking");
                }

                // Get the booking
                var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);

                if (booking == null)
                {
                    _loggerService.Warn($"No booking found with ID: {bookingId}");
                    throw new KeyNotFoundException($"Booking with ID {bookingId} not found");
                }

                // Create invoice
                var invoice = new Invoice
                {
                    BookingId = bookingId,
                    InvoiceDate = DateTime.UtcNow,
                    Amount = booking.TotalAmount,
                    Status = "Pending" // Initial status
                };

                await _unitOfWork.Invoices.AddAsync(invoice);
                await _unitOfWork.SaveChangesAsync();

                _loggerService.Success($"Invoice created successfully with ID: {invoice.Id}");

                // Get the newly created invoice with all the relationships
                invoice = await _unitOfWork.Invoices.GetByIdAsync(invoice.Id);

                var result = await MapToDto(invoice);
                return result;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error creating invoice for booking {bookingId}: {ex.Message}");
                throw;
            }
        }

        public async Task<InvoiceDto> UpdateInvoiceStatusAsync(Guid id, string status)
        {
            if (id == Guid.Empty)
            {
                _loggerService.Warn("Attempted to update invoice with an empty GUID.");
                throw new ArgumentException("Invalid invoice ID.");
            }

            try
            {
                _loggerService.Info($"Starting invoice status update for invoice: {id}");

                var invoice = await _unitOfWork.Invoices.GetByIdAsync(id);
                if (invoice == null)
                {
                    _loggerService.Warn($"No invoice found with ID: {id}");
                    throw new KeyNotFoundException($"Invoice with ID {id} not found");
                }

                // Update status
                invoice.Status = status;
                await _unitOfWork.Invoices.Update(invoice);
                await _unitOfWork.SaveChangesAsync();

                // Clear caches
                await _redisService.RemoveAsync($"invoice:detail:{id}");
                await _redisService.RemoveAsync($"invoice:booking:{invoice.BookingId}");
                await _redisService.RemoveByPatternAsync("invoices:user:*");

                _loggerService.Success($"Invoice {id} status updated to {status}");

                // Get the updated invoice with all relationships
                invoice = await _unitOfWork.Invoices.GetByIdAsync(id);

                var result = await MapToDto(invoice);
                return result;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error updating invoice {id} status: {ex.Message}");
                throw;
            }
        }


        private async Task<InvoiceDto> MapToDto(Invoice invoice)
        {
            var booking = invoice.Booking;

            // Get member name
            var member = await _unitOfWork.Users.GetByIdAsync(booking.MemberId);
            string memberName = member?.FullName ?? "Unknown";

            // Get movie title and show date
            var showtime = await _unitOfWork.ShowTimes.GetByIdAsync(booking.ShowtimeId, st => st.Movie);
            string movieTitle = showtime?.Movie?.Name ?? "Unknown";
            DateTime showDate = showtime?.ShowDate ?? DateTime.MinValue;

            // Get seat count
            int seatCount;
            var bookingSeats = await _unitOfWork.BookingSeats.GetAllAsync(bs => bs.BookingId == booking.Id);
            if (bookingSeats == null || !bookingSeats.Any())
            {
                _loggerService.Warn($"No booking seats found for booking ID: {booking.Id}");
                throw new KeyNotFoundException($"No seats found for booking ID {booking.Id}");
            }
            else
            {
                seatCount = bookingSeats.Count();
            }

            return new InvoiceDto
            {
                Id = invoice.Id,
                BookingId = invoice.BookingId,
                InvoiceDate = invoice.InvoiceDate,
                Amount = invoice.Amount,
                Status = invoice.Status,
                Booking = new BookingSummaryDto
                {
                    Id = booking.Id,
                    MemberName = memberName,
                    MovieTitle = movieTitle,
                    ShowDate = showDate,
                    SeatCount = seatCount,
                    TotalAmount = booking.TotalAmount
                }
            };
        }

        private string GeneratePaymentReference()
        {
            return $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 8)}";
        }
    }
}
