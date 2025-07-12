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
        ILoggerService loggerService
        )
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

            _loggerService.Info($"Booking retrieved: {booking?.Id}, Member: {booking?.Member?.PhoneNumber}, Showtime: {booking?.Showtime?.Id}");

            if (booking == null || booking.IsDeleted)
            {
                _loggerService.Warn($"No booking found with ID: {bookingId} or booking is already deleted");
                throw new KeyNotFoundException($"Booking with ID {bookingId} not found.");
            }

            // Check if ticket already exists for this booking
            var existingTicket = await _unitOfWork.Tickets.FirstOrDefaultAsync(t => t.BookingId == bookingId && !t.IsDeleted);
            if (existingTicket != null)
            {
                _loggerService.Warn($"Ticket already exists for booking ID: {bookingId}");
                throw new InvalidOperationException("Ticket already exists for this booking.");
            }

            // Create the ticket
            var ticket = new Ticket
            {
                BookingId = bookingId,
                IssuedAt = DateTime.UtcNow,
                GuestPhoneNumber = booking.Member.PhoneNumber,
                TotalPrice = booking.TotalAmount,
                ShowTimeId = booking.ShowtimeId,
                TicketType = TicketType.Online,
                TicketSeats = new List<TicketSeat>(),
                TicketFoodAndDrinks = new List<TicketFoodAndDrink>()
            };
            if (ticket == null)
            {
                _loggerService.Warn($"Failed to create ticket object for booking ID: {bookingId}");
                throw new InvalidOperationException("Failed to create ticket object.");
            }

            // Add seats to the ticket
            if (booking.BookingSeats == null)
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
                    PricePerSeat = GetSeatPrice(seat)
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

    public async Task<TicketResponseDto> CreateOfflineTicketAsync(CreateOfflineTicketRequest request)
    {
        try
        {
            _loggerService.Info($"Creating offline ticket for guest phone number: {request.GuestPhoneNumber}, Showtime ID: {request.ShowtimeId}");
            if (string.IsNullOrWhiteSpace(request.GuestPhoneNumber))
                throw new ArgumentException("Guest phone number is required.");
            if (!System.Text.RegularExpressions.Regex.IsMatch(request.GuestPhoneNumber, @"^\+?\d{9,15}$"))
                throw new ArgumentException("Guest phone number is not in a valid format.");

            // Optionally check if user exists by phone number
            User? user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.GuestPhoneNumber && !u.IsDeleted);

            // Validate showtime
            var showtime = await _unitOfWork.ShowTimes.GetByIdAsync(request.ShowtimeId, s => s.Movie, s => s.CinemaRoom);
            if (showtime == null || showtime.IsDeleted)
                throw new KeyNotFoundException("Showtime not found.");

            _loggerService.Info($"Showtime found: {showtime.Id}, Movie: {showtime.Movie?.Name}, Cinema Room: {showtime.CinemaRoom?.Name}");

            // Validate seats
            var seats = await _unitOfWork.Seats.GetAllAsync(s => request.SeatIds.Contains(s.Id) && !s.IsDeleted);
            if (seats.Count() != request.SeatIds.Count)
                throw new InvalidOperationException("Some seats are invalid.");

            var existSeat = await _unitOfWork.ShowTimeSeats.GetAllAsync(
                sts => sts.ShowTimeId == request.ShowtimeId && request.SeatIds.Contains(sts.SeatId));

            // Check if any selected seat is already booked
            if (existSeat.Any(s => s.Status == SeatStatus.Booked || s.Status == SeatStatus.Sold))
            {
                _loggerService.Warn($"Attempted to book unavailable seats for showtime: {request.ShowtimeId}");
                throw new InvalidOperationException("One or more selected seats are not available");
            }

            var ticket = new Ticket
            {
                BookingId = null,
                IssuedAt = DateTime.UtcNow,
                GuestPhoneNumber = request.GuestPhoneNumber,
                ShowTimeId = showtime.Id,
                TicketType = TicketType.Offline,
                TicketSeats = new List<TicketSeat>(),
                TicketFoodAndDrinks = new List<TicketFoodAndDrink>()
            };

            foreach (var seat in seats)
            {
                ticket.TicketSeats.Add(new TicketSeat
                {
                    SeatId = seat.Id,
                    PricePerSeat = GetSeatPrice(seat)
                });
            }

            if (request.FoodItems != null)
            {
                foreach (var item in request.FoodItems)
                {
                    ticket.TicketFoodAndDrinks.Add(new TicketFoodAndDrink
                    {
                        FoodAndDrinkId = item.FoodAndDrinkId,
                        Quantity = item.Quantity
                    });
                }
            }
            // Calculate total price
            ticket.TotalPrice = CaculateTotalPrice(
                ticket.TicketSeats.Select(ts => new TicketSeatDto
                {
                    SeatId = ts.SeatId,
                    PricePerSeat = ts.PricePerSeat
                }),
                ticket.TicketFoodAndDrinks.Select(tf => new TicketFoodDto
                {
                    FoodId = tf.FoodAndDrinkId,
                    Quantity = tf.Quantity,
                    Price = _unitOfWork.FoodAndDrinks.GetByIdAsync(tf.FoodAndDrinkId).Result!.Price
                }));

            foreach (var seat in seats)
            {
                var ShowTimeSeat = new ShowTimeSeat
                {
                    ShowTimeId = request.ShowtimeId,
                    SeatId = seat.Id,
                    Status = SeatStatus.Sold
                };
                await _unitOfWork.ShowTimeSeats.AddAsync(ShowTimeSeat);
            }
            await _unitOfWork.Tickets.AddAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            return await GetTicketDetailsAsync(ticket.Id);
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error creating offline ticket: {ex.Message}");
            throw new InvalidOperationException("Failed to create offline ticket", ex);
        }
    }

    public async Task<Pagination<TicketResponseDto>> GetAllTicketsAsync(
        int page = 1,
        int pageSize = 10,
        TicketType? ticketType = null,
        string? sortBy = null,
        bool isDescending = false,
        string? search = null)
    {
        try
        {
            _loggerService.Info($"Fetching tickets - Page {page}, PageSize {pageSize}, TicketType: {ticketType}, Search: {search}");

            var tickets = await _unitOfWork.Tickets.GetAllAsync(t => !t.IsDeleted);
            var query = tickets.AsQueryable();

            // Filter by ticket type if provided
            if (ticketType.HasValue)
            {
                query = query.Where(t => t.TicketType == ticketType.Value);
            }

            // Search by movie name, guest phone, or cinema room
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                _loggerService.Info($"Searching tickets with term: {lowerSearch}");

                // Find showtimeIds by movie name
                var showtimeIds = (await _unitOfWork.ShowTimes.GetAllAsync(st =>
                    !st.IsDeleted && st.Movie != null && !st.Movie.IsDeleted &&
                    st.Movie.Name != null && st.Movie.Name.ToLower().Contains(lowerSearch)))
                    .Select(st => st.Id)
                    .ToList();

                query = query.Where(t =>
                    (!string.IsNullOrEmpty(t.GuestPhoneNumber) && t.GuestPhoneNumber.Contains(lowerSearch)) ||
                    (t.Showtime != null && showtimeIds.Contains(t.Showtime.Id))
                );
                _loggerService.Info($"Filtered tickets by search term '{lowerSearch}', total count: {query.Count()}");
            }

            var totalItems = query.Count();

            // Sorting
            query = sortBy?.ToLower() switch
            {
                "date" => isDescending ? query.OrderByDescending(t => t.IssuedAt) : query.OrderBy(t => t.IssuedAt),
                "price" => isDescending ? query.OrderByDescending(t => t.TotalPrice) : query.OrderBy(t => t.TotalPrice),
                _ => isDescending ? query.OrderByDescending(t => t.IssuedAt) : query.OrderBy(t => t.IssuedAt)
            };

            // Pagination
            var pagedItems = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new List<TicketResponseDto>();
            foreach (var ticket in pagedItems)
            {
                // Use the existing method to get full ticket details
                result.Add(await GetTicketDetailsAsync(ticket.Id));
            }

            var response = new Pagination<TicketResponseDto>(result, totalItems, page, pageSize);
            _loggerService.Success($"Retrieved {result.Count} tickets on page {page} successfully.");

            return response;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Failed to retrieve tickets. Exception: {ex.Message}");
            throw new Exception("An error occurred while retrieving ticket items. Please try again later.");
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

            var member = await _unitOfWork.Users.GetByIdAsync(userId);
            if (member == null || member.IsDeleted)
            {
                _loggerService.Warn($"No user found with ID: {userId}");
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            var phoneNumber = member.PhoneNumber;
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                _loggerService.Warn($"User with ID {userId} has no phone number associated.");
                throw new InvalidOperationException("User phone number is required to fetch tickets.");
            }

            _loggerService.Info($"User phone number: {phoneNumber}");
            // Get all tickets for the user
            var tickets = await _unitOfWork.Tickets.GetAllAsync(
                t => (t.GuestPhoneNumber == phoneNumber) && !t.IsDeleted,
                t => t.TicketSeats,
                t => t.TicketFoodAndDrinks,
                t => t.Showtime);

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

            // Validate input
            if (ticketId == Guid.Empty)
            {
                _loggerService.Warn("Attempted to generate QR code with empty ticket ID");
                throw new ArgumentException("Invalid ticket ID", nameof(ticketId));
            }

            var ticketDetails = await GetTicketDetailsAsync(ticketId);
            if (ticketDetails == null)
                throw new KeyNotFoundException($"Ticket with ID {ticketId} not found.");

            var qrPayload = new QrCodePayload
            {
                TicketId = ticketDetails.Id,
                Hash = GenerateTicketValidationHash(ticketDetails),
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            string qrContent = JsonSerializer.Serialize(qrPayload, jsonOptions);

            // Validate QR content size
            if (qrContent.Length > 2953)
            {
                _loggerService.Warn($"QR content too large: {qrContent.Length} characters");
                throw new InvalidOperationException("QR code content exceeds maximum size limit");
            }

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new Base64QRCode(qrCodeData);

            string qrBase64 = qrCode.GetGraphic(
                7,
                "Black",
                "White",
                true,
                Base64QRCode.ImageType.Png);

            _loggerService.Success($"QR code generated successfully for ticket ID: {ticketId}");
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
            _loggerService.Info($"Verifying ticket QR code for ticket ID: {qrCodeData?.TicketId}");

            if (qrCodeData == null || qrCodeData.TicketId == Guid.Empty)
            {
                _loggerService.Warn("Invalid QR code format or missing ticket ID");
                throw new ArgumentException("Invalid QR code data");
            }

            if (string.IsNullOrWhiteSpace(qrCodeData.Hash))
            {
                _loggerService.Warn($"Missing hash in QR code for ticket {qrCodeData.TicketId}");
                throw new ArgumentException("QR code hash is missing");
            }

            // Check expiration first (faster check)
            if (DateTime.UtcNow > qrCodeData.ExpiresAt)
            {
                _loggerService.Warn($"QR code for ticket {qrCodeData.TicketId} has expired at {qrCodeData.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC");
                throw new InvalidOperationException("QR code has expired");
            }

            // Check if the ticket exists
            var ticketDetails = await GetTicketDetailsAsync(qrCodeData.TicketId);
            if (ticketDetails == null)
            {
                _loggerService.Warn($"Ticket with ID {qrCodeData.TicketId} not found");
                throw new KeyNotFoundException($"Ticket with ID {qrCodeData.TicketId} not found.");
            }

            // Validate hash using the improved method
            string expectedHash = GenerateTicketValidationHash(ticketDetails);
            if (qrCodeData.Hash != expectedHash)
            {
                _loggerService.Warn($"Hash validation failed for ticket {qrCodeData.TicketId}");
                throw new InvalidOperationException("QR code hash does not match the ticket data");
            }

            _loggerService.Success($"Ticket {qrCodeData.TicketId} verified successfully");
            return new TicketVerificationResultDto
            {
                IsValid = true,
                Message = "Ticket verified successfully",
                Ticket = ticketDetails
            };
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error verifying ticket QR code: {ex.Message}");
            throw new InvalidOperationException("QR code verification failed due to system error", ex);
        }
    }

    private string GenerateTicketValidationHash(TicketResponseDto ticket)
    {
        try
        {
            // Include comprehensive ticket data
            var dataToHash = $"{ticket.Id}|{ticket.IssuedAt:yyyy-MM-dd-HH-mm-ss}|{ticket.TotalPrice}";

            // Use the configured HMAC secret key from settings
            using var hmac = new System.Security.Cryptography.HMACSHA256(
                System.Text.Encoding.UTF8.GetBytes("MovieTheater_QRCode_SecretKey_2024_VeryLongAndRandomString_ForHMACValidation_ShouldBe256BitsMinimum_NeverShareThis"));

            if (hmac.Key == null || hmac.Key.Length < 32)
            {
                _loggerService.Warn("HMAC key is not properly configured or too short");
                throw new InvalidOperationException("HMAC key is not properly configured");
            }

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(dataToHash);
            byte[] hash = hmac.ComputeHash(bytes);

            return Convert.ToBase64String(hash);
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error generating validation hash: {ex.Message}");
            throw new InvalidOperationException("Failed to generate validation hash", ex);
        }
    }

    private async Task<TicketResponseDto> GetTicketDetailsAsync(Guid ticketId)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(
            ticketId,
            t => t.Booking,
            t => t.TicketSeats,
            t => t.TicketFoodAndDrinks,
            t => t.Showtime);

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
            if (seat == null)
            {
                _loggerService.Warn($"Seat with ID {ts.SeatId} not found for ticket {ticketId}");
                throw new KeyNotFoundException($"Seat with ID {ts.SeatId} not found for ticket {ticketId}.");
            }
            return new TicketSeatDto
            {
                SeatId = ts.SeatId,
                Row = seat.Row,
                Number = seat.Number,
                PricePerSeat = ts.PricePerSeat != 0 ? ts.PricePerSeat : GetSeatPrice(seat),
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

        var showtime = await _unitOfWork.ShowTimes.GetByIdAsync(ticket.Showtime.Id, s => s.Movie, s => s.CinemaRoom);
        if (showtime == null)
        {
            _loggerService.Warn($"No showtime found for ticket with ID: {ticketId}");
            throw new KeyNotFoundException($"Showtime for ticket with ID {ticketId} not found.");
        }

        return new TicketResponseDto
        {
            Id = ticket.Id,
            BookingId = ticket.BookingId,
            IssuedAt = ticket.IssuedAt,
            GuestPhoneNumber = ticket.GuestPhoneNumber,
            TotalPrice = ticket.TotalPrice,
            TicketType = ticket.TicketType.ToString(),
            MovieName = showtime.Movie.Name,
            ShowTime = showtime.ShowDate.ToString("yyyy-MM-dd HH:mm"),
            CinemaRoom = showtime.CinemaRoom.Name,
            Seats = ticketSeats,
            FoodItems = ticketFoods
        };
    }

    private decimal CaculateTotalPrice(IEnumerable<TicketSeatDto> seats, IEnumerable<TicketFoodDto> foodItems)
    {
        decimal total = 0;
        if (seats != null)
        {
            total += seats.Sum(s => s.PricePerSeat);
        }
        if (foodItems != null)
        {
            total += foodItems.Sum(f => f.Price * f.Quantity);
        }
        return total;
    }
    private decimal GetSeatPrice(Seat seat)
    {
        // Logic to determine seat price based on seat type
        if (seat == null)
            return 0;

        return seat.Type switch
        {
            SeatType.Normal => 80000,  // Example prices
            SeatType.VIP => 120000,
            SeatType.Couple => 200000,
            _ => 80000
        };
    }
}

