using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.TicketDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using QRCoder;
using System.Text.Json;

namespace MovieTheater.Application.Services;

public class TicketService : ITicketService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggerService _loggerService;
    public TicketService(
        IUnitOfWork unitOfWork,
        ILoggerService loggerService)
    {
        _unitOfWork = unitOfWork;
        _loggerService = loggerService;
    }

    public async Task<TicketResponseDto> GenerateTicketFromBookingAsync(Guid bookingId)
    {
        if (bookingId == Guid.Empty)
        {
            _loggerService.Warn("Attempted to generate ticket with an empty booking GUID.");
            throw new ArgumentException("Invalid booking ID.");
        }

        try
        {
            _loggerService.Info($"Starting ticket generation for booking ID: {bookingId}");

            // Get the booking with its details
            var booking = await _unitOfWork.Bookings.GetByIdAsync(
                bookingId,
                b => b.BookingSeats,
                b => b.BookingFoods,
                b => b.Member,
                b => b.Showtime);

            if (booking == null || booking.IsDeleted)
            {
                _loggerService.Warn($"No booking found with ID: {bookingId} or booking is already deleted");
                throw new KeyNotFoundException($"Booking with ID {bookingId} not found.");
            }

            // Check if ticket already exists for this booking
            var existingTicket = await _unitOfWork.Tickets.FirstOrDefaultAsync(t => t.BookingId == bookingId && !t.IsDeleted);
            if (existingTicket != null)
            {
                _loggerService.Info($"Ticket already exists for booking ID: {bookingId}");
                // Return existing ticket details
                return await GetTicketDetailsAsync(existingTicket.Id);
            }

            // Create the ticket
            var ticket = new Ticket
            {
                BookingId = bookingId,
                IssuedAt = DateTime.UtcNow,
                GuestPhoneNumber = booking.Member.PhoneNumber,
                TotalPrice = booking.TotalAmount,
                TicketType = TicketType.Online,
                TicketSeats = new List<TicketSeat>(),
                TicketFoodAndDrinks = new List<TicketFoodAndDrink>()
            };

            // Add seats to the ticket
            if (booking.BookingSeats == null && !booking.BookingSeats.Any())
            {
                _loggerService.Warn($"No seats found for booking ID: {bookingId}");
                throw new InvalidOperationException("No seats associated with this booking.");
            }
            var seats = await _unitOfWork.Seats.GetAllAsync(s =>
                booking.BookingSeats.Select(bs => bs.SeatId).Contains(s.Id) && !s.IsDeleted);

            foreach (var seat in seats)
            {
                ticket.TicketSeats.Add(new TicketSeat
                {
                    SeatId = seat.Id,
                });
            }

            // Add food items to the ticket
            if (booking.BookingFoods != null && booking.BookingFoods.Any())
            {
                foreach (var bookingFood in booking.BookingFoods)
                {
                    ticket.TicketFoodAndDrinks.Add(new TicketFoodAndDrink
                    {
                        FoodAndDrinkId = bookingFood.FoodAndDrinkId,
                        Quantity = bookingFood.Quantity
                    });
                }
            }

            // Save the ticket
            await _unitOfWork.Tickets.AddAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.Success($"Ticket generated successfully for booking ID: {bookingId}");

            // Return ticket details
            return await GetTicketDetailsAsync(ticket.Id);
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error generating ticket for booking {bookingId}: {ex.Message}");
            throw;
        }
    }

    public async Task<TicketResponseDto> GetTicketByIdAsync(Guid ticketId)
    {
        if (ticketId == Guid.Empty)
        {
            _loggerService.Warn("Attempted to fetch ticket with an empty GUID.");
            throw new ArgumentException("Invalid ticket ID.");
        }

        try
        {
            return await GetTicketDetailsAsync(ticketId);
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error fetching ticket {ticketId}: {ex.Message}");
            throw;
        }
    }

    public async Task<IEnumerable<TicketResponseDto>> GetUserTicketsAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            _loggerService.Warn("Attempted to fetch tickets with an empty user GUID.");
            throw new ArgumentException("Invalid user ID.");
        }

        try
        {
            _loggerService.Info($"Fetching tickets for user ID: {userId}");

            // Get all bookings for the user
            var bookings = await _unitOfWork.Bookings.GetAllAsync(
                b => b.MemberId == userId && !b.IsDeleted);

            if (!bookings.Any())
            {
                _loggerService.Info($"No bookings found for user ID: {userId}");
                return new List<TicketResponseDto>();
            }

            // Get all tickets for those bookings
            var bookingIds = bookings.Select(b => b.Id).ToList();
            var tickets = await _unitOfWork.Tickets.GetAllAsync(
                t => bookingIds.Contains(t.BookingId.Value) && !t.IsDeleted);

            if (!tickets.Any())
            {
                _loggerService.Info($"No tickets found for user ID: {userId}");
                return new List<TicketResponseDto>();
            }

            // Get ticket details for each ticket
            var result = new List<TicketResponseDto>();
            foreach (var ticket in tickets)
            {
                result.Add(await GetTicketDetailsAsync(ticket.Id));
            }

            _loggerService.Success($"Successfully retrieved {result.Count} tickets for user ID: {userId}");
            return result;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error fetching tickets for user {userId}: {ex.Message}");
            throw;
        }
    }

    public async Task<string> GenerateTicketQRCodeAsync(Guid ticketId)
    {
        try
        {
            _loggerService.Info($"Generating QR code for ticket ID: {ticketId}");

            var ticketDetails = await GetTicketDetailsAsync(ticketId);
            if (ticketDetails == null)
                throw new KeyNotFoundException($"Ticket with ID {ticketId} not found.");

            var qrPayload = new
            {
                TicketId = ticketDetails.Id,
                Hash = GenerateTicketValidationHash(ticketDetails),
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            };

            string qrContent = JsonSerializer.Serialize(qrPayload);

            using var qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);

            var qrCode = new Base64QRCode(qrCodeData);
            string qrBase64 = qrCode.GetGraphic(7, "Black", "White", true, Base64QRCode.ImageType.Png);

            return $"data:image/png;base64,{qrBase64}";
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error generating QR code for ticket {ticketId}: {ex.Message}");
            throw;
        }
    }

    public async Task<TicketVerificationResultDto> VerifyTicketQRCodeAsync(QrCodePayload qrCodeData)
    {
        try
        {
            _loggerService.Info($"Verifying ticket QR code");

            if (qrCodeData == null)
            {
                _loggerService.Warn("Invalid QR code format");
                return new TicketVerificationResultDto
                {
                    IsValid = false,
                    Message = "Invalid QR code format"
                };
            }

            // Check if the ticket exists
            var ticketDetails = await GetTicketDetailsAsync(qrCodeData.TicketId);

            // Verify expiration
            if (DateTime.UtcNow > qrCodeData.ExpiresAt)
            {
                _loggerService.Warn($"QR code for ticket {qrCodeData.TicketId} has expired");
                return new TicketVerificationResultDto
                {
                    IsValid = false,
                    Message = "QR code has expired",
                    Ticket = ticketDetails
                };
            }

            // Validate hash
            string expectedHash = GenerateTicketValidationHash(ticketDetails);
            if (qrCodeData.Hash != expectedHash)
            {
                _loggerService.Warn($"Invalid hash for ticket {qrCodeData.TicketId}");
                return new TicketVerificationResultDto
                {
                    IsValid = false,
                    Message = "Invalid ticket hash - QR code may have been tampered with",
                    Ticket = ticketDetails
                };
            }

            _loggerService.Success($"Ticket {qrCodeData.TicketId} verified successfully");
            return new TicketVerificationResultDto
            {
                IsValid = true,
                Message = "Ticket verified successfully",
                Ticket = ticketDetails
            };
        }
        catch (KeyNotFoundException ex)
        {
            _loggerService.Warn($"Ticket not found during verification: {ex.Message}");
            return new TicketVerificationResultDto
            {
                IsValid = false,
                Message = "Ticket not found"
            };
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error verifying ticket QR code: {ex.Message}");
            throw;
        }
    }

    private string GenerateTicketValidationHash(TicketResponseDto ticket)
    {
        // Create a simple validation hash combining ticket ID and issue time
        // In a real application, you might want to use a more secure method
        string dataToHash = $"{ticket.Id}";
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(dataToHash);
            byte[] hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    private async Task<TicketResponseDto> GetTicketDetailsAsync(Guid ticketId)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(
            ticketId,
            t => t.Booking,
            t => t.TicketSeats,
            t => t.TicketFoodAndDrinks);

        if (ticket == null)
        {
            _loggerService.Warn($"No ticket found with ID: {ticketId}");
            throw new KeyNotFoundException($"Ticket with ID {ticketId} not found.");
        }

        var ticketSeats = new List<TicketSeatDto>();

        var seatIds = ticket.TicketSeats.Select(ts => ts.SeatId).ToList();
        var seats = await _unitOfWork.Seats.GetAllAsync(s => seatIds.Contains(s.Id) && !s.IsDeleted);

        ticketSeats = ticket.TicketSeats.Select(ts =>
        {
            var seat = seats.FirstOrDefault(s => s.Id == ts.SeatId);
            return new TicketSeatDto
            {
                SeatId = ts.SeatId,
                Row = seat.Row,
                Number = seat.Number,
            };
        }).ToList();

        var ticketFoods = new List<TicketFoodDto>();
        if (ticket.TicketFoodAndDrinks != null && ticket.TicketFoodAndDrinks.Any())
        {
            var foodIds = ticket.TicketFoodAndDrinks.Select(tf => tf.FoodAndDrinkId).ToList();
            var foods = await _unitOfWork.FoodAndDrinks.GetAllAsync(f => foodIds.Contains(f.Id) && !f.IsDeleted);

            ticketFoods = ticket.TicketFoodAndDrinks.Select(tf =>
            {
                var food = foods.FirstOrDefault(f => f.Id == tf.FoodAndDrinkId);
                return new TicketFoodDto
                {
                    FoodId = tf.FoodAndDrinkId,
                    Name = food?.Name ?? "Unknown",
                    Quantity = tf.Quantity,
                    Price = food?.Price ?? 0
                };
            }).ToList();
        }

        var showtime = await _unitOfWork.ShowTimes.GetByIdAsync(ticket.Booking.ShowtimeId, s => s.Movie, s => s.CinemaRoom);

        return new TicketResponseDto
        {
            Id = ticket.Id,
            BookingId = ticket.BookingId,
            IssuedAt = ticket.IssuedAt,
            GuestPhoneNumber = ticket.GuestPhoneNumber,
            TotalPrice = ticket.TotalPrice,
            TicketType = ticket.TicketType.ToString(),
            MovieName = showtime?.Movie?.Name,
            ShowTime = showtime?.ShowDate.ToString("yyyy-MM-dd HH:mm"),
            CinemaRoom = showtime?.CinemaRoom?.Name,
            Seats = ticketSeats,
            FoodItems = ticketFoods
        };
    }
}

