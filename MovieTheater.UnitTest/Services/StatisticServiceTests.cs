using MockQueryable;
using Moq;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.DTOs.StatisticDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.UnitTest.Services
{
    public class StatisticServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILoggerService> _mockLoggerService;
        private readonly Mock<IGenericRepository<User>> _mockUserRepository;
        private readonly Mock<IGenericRepository<Ticket>> _mockTicketRepository;
        private readonly Mock<IGenericRepository<TicketSeat>> _mockTicketSeatRepository;
        private readonly Mock<IGenericRepository<TicketFoodAndDrink>> _mockTicketFoodAndDrinkRepository;
        private readonly Mock<IGenericRepository<Invoice>> _mockInvoiceRepository;
        private readonly Mock<IGenericRepository<Promotion>> _mockPromotionRepository;
        private readonly Mock<IGenericRepository<ScoreHistory>> _mockScoreHistoryRepository;
        private readonly StatisticService _statisticService;

        public StatisticServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLoggerService = new Mock<ILoggerService>();
            _mockUserRepository = new Mock<IGenericRepository<User>>();
            _mockTicketRepository = new Mock<IGenericRepository<Ticket>>();
            _mockTicketSeatRepository = new Mock<IGenericRepository<TicketSeat>>();
            _mockTicketFoodAndDrinkRepository = new Mock<IGenericRepository<TicketFoodAndDrink>>();
            _mockInvoiceRepository = new Mock<IGenericRepository<Invoice>>();
            _mockPromotionRepository = new Mock<IGenericRepository<Promotion>>();
            _mockScoreHistoryRepository = new Mock<IGenericRepository<ScoreHistory>>();

            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
            _mockUnitOfWork.Setup(u => u.Tickets).Returns(_mockTicketRepository.Object);
            _mockUnitOfWork.Setup(u => u.TicketSeats).Returns(_mockTicketSeatRepository.Object);
            _mockUnitOfWork.Setup(u => u.TicketFoodAndDrinks).Returns(_mockTicketFoodAndDrinkRepository.Object);
            _mockUnitOfWork.Setup(u => u.Invoices).Returns(_mockInvoiceRepository.Object);
            _mockUnitOfWork.Setup(u => u.Promotions).Returns(_mockPromotionRepository.Object);
            _mockUnitOfWork.Setup(u => u.ScoreHistories).Returns(_mockScoreHistoryRepository.Object);

            _statisticService = new StatisticService(
                _mockUnitOfWork.Object,
                _mockLoggerService.Object
            );
        }

        [Fact]
        public async Task GetRegisterPerMonthAsync_ReturnsMonthlyRegisterData()
        {
            // Arrange
            // Use current actual date for the test to work with the service's DateTime.UtcNow calls
            var currentDate = DateTime.UtcNow;
            var currentYear = currentDate.Year;
            var currentMonth = currentDate.Month;

            var users = new List<User>
            {
                new() { Id = Guid.NewGuid(), Role = RoleType.Member, IsDeleted = false, CreatedAt = new DateTime(currentYear, currentMonth, 1, 0, 0, 0, DateTimeKind.Utc) },
                new() { Id = Guid.NewGuid(), Role = RoleType.Member, IsDeleted = false, CreatedAt = new DateTime(currentYear, currentMonth - 1 > 0 ? currentMonth - 1 : 12, 15, 0, 0, 0, DateTimeKind.Utc) },
                new() { Id = Guid.NewGuid(), Role = RoleType.Member, IsDeleted = false, CreatedAt = new DateTime(currentYear, currentMonth - 2 > 0 ? currentMonth - 2 : currentMonth - 2 + 12, 10, 0, 0, 0, DateTimeKind.Utc) },
                new() { Id = Guid.NewGuid(), Role = RoleType.Admin, IsDeleted = false, CreatedAt = new DateTime(currentYear, currentMonth, 5, 0, 0, 0, DateTimeKind.Utc) }, // Should be excluded
                new() { Id = Guid.NewGuid(), Role = RoleType.Member, IsDeleted = true, CreatedAt = new DateTime(currentYear, currentMonth, 8, 0, 0, 0, DateTimeKind.Utc) } // Should be excluded
            };

            var queryable = users.AsQueryable().BuildMock();
            _mockUserRepository.Setup(r => r.GetQueryable()).Returns(queryable);

            // Act
            var result = await _statisticService.GetRegisterPerMonthAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(12, result.Count); // Should return 12 months of data

            // Verify current month has 1 member
            var currentMonthData = result.FirstOrDefault(r => r.Month == currentMonth && r.Year == currentYear);
            Assert.NotNull(currentMonthData);
            Assert.Equal(1, currentMonthData.TotalRegisters);

            // Verify previous month has 1 member (handle year boundary)
            var previousMonth = currentMonth - 1 > 0 ? currentMonth - 1 : 12;
            var previousYear = currentMonth - 1 > 0 ? currentYear : currentYear - 1;
            var previousMonthData = result.FirstOrDefault(r => r.Month == previousMonth && r.Year == previousYear);
            Assert.NotNull(previousMonthData);
            Assert.Equal(1, previousMonthData.TotalRegisters);

            // Verify logging
            _mockLoggerService.Verify(l => l.Info("Starting GetRegisterPerMonthAsync"), Times.Once);
        }

        [Fact]
        public async Task GetRegisterPerMonthAsync_ThrowsException_LogsErrorAndRethraws()
        {
            // Arrange
            var expectedErrorMessage = "Database connection failed";
            _mockUserRepository.Setup(r => r.GetQueryable()).Throws(new Exception(expectedErrorMessage));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _statisticService.GetRegisterPerMonthAsync());

            Assert.Equal(expectedErrorMessage, ex.Message);

            // Verify error logging
            _mockLoggerService.Verify(
                l => l.Error($"Error in GetRegisterPerMonthAsync: {expectedErrorMessage}"),
                Times.Once);
        }

        [Fact]
        public async Task GetMonthlyRevenueAsync_ReturnsMonthlyRevenueData()
        {
            // Arrange
            // Use current actual date for the test to work with the service's DateTime.UtcNow calls
            var currentDate = DateTime.UtcNow;
            var currentYear = currentDate.Year;
            var currentMonth = currentDate.Month;

            var tickets = new List<Ticket>
            {
                new() { Id = Guid.NewGuid(), IsDeleted = false, CreatedAt = new DateTime(currentYear, currentMonth, 1, 0, 0, 0, DateTimeKind.Utc), TotalPrice = 100m },
                new() { Id = Guid.NewGuid(), IsDeleted = false, CreatedAt = new DateTime(currentYear, currentMonth, 15, 0, 0, 0, DateTimeKind.Utc), TotalPrice = 200m },
                new() { Id = Guid.NewGuid(), IsDeleted = false, CreatedAt = new DateTime(currentYear, currentMonth - 1 > 0 ? currentMonth - 1 : 12, 10, 0, 0, 0, DateTimeKind.Utc), TotalPrice = 150m },
                new() { Id = Guid.NewGuid(), IsDeleted = true, CreatedAt = new DateTime(currentYear, currentMonth, 5, 0, 0, 0, DateTimeKind.Utc), TotalPrice = 50m } // Should be excluded
            };

            var queryable = tickets.AsQueryable().BuildMock();
            _mockTicketRepository.Setup(r => r.GetQueryable()).Returns(queryable);

            // Act
            var result = await _statisticService.GetMonthlyRevenueAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(12, result.Count);

            // Verify current month revenue
            var currentMonthData = result.FirstOrDefault(r => r.Month == currentMonth && r.Year == currentYear);
            Assert.NotNull(currentMonthData);
            Assert.Equal(300m, currentMonthData.TotalRevenue); // 100 + 200

            // Verify previous month revenue (handle year boundary)
            var previousMonth = currentMonth - 1 > 0 ? currentMonth - 1 : 12;
            var previousYear = currentMonth - 1 > 0 ? currentYear : currentYear - 1;
            var previousMonthData = result.FirstOrDefault(r => r.Month == previousMonth && r.Year == previousYear);
            Assert.NotNull(previousMonthData);
            Assert.Equal(150m, previousMonthData.TotalRevenue);

            // Verify logging
            _mockLoggerService.Verify(l => l.Info("Starting GetMonthlyRevenueAsync"), Times.Once);
        }

        [Fact]
        public async Task GetMonthlyRevenueAsync_ThrowsException_LogsErrorAndRethraws()
        {
            // Arrange
            var expectedErrorMessage = "Database query failed";
            _mockTicketRepository.Setup(r => r.GetQueryable()).Throws(new Exception(expectedErrorMessage));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _statisticService.GetMonthlyRevenueAsync());

            Assert.Equal(expectedErrorMessage, ex.Message);

            // Verify error logging
            _mockLoggerService.Verify(
                l => l.Error($"Error in GetMonthlyRevenueAsync: {expectedErrorMessage}"),
                Times.Once);
        }

        [Fact]
        public async Task GetMonthlyRevenueMovieAsync_ReturnsMovieRevenueData()
        {
            // Arrange
            var monthYear = new MonthYearDto(6, 2023);
            var movieId1 = Guid.NewGuid();
            var movieId2 = Guid.NewGuid();

            var movie1 = new Movie { Id = movieId1, Name = "Movie 1" };
            var movie2 = new Movie { Id = movieId2, Name = "Movie 2" };

            var showtime1 = new ShowTime { Id = Guid.NewGuid(), MovieId = movieId1, Movie = movie1 };
            var showtime2 = new ShowTime { Id = Guid.NewGuid(), MovieId = movieId2, Movie = movie2 };

            var tickets = new List<Ticket>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    IsDeleted = false,
                    CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    TicketType = TicketType.Offline,
                    Showtime = showtime1,
                    TicketSeats = new List<TicketSeat>()
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    IsDeleted = false,
                    CreatedAt = new DateTime(2023, 6, 15, 0, 0, 0, DateTimeKind.Utc),
                    TicketType = TicketType.Online,
                    Showtime = showtime2,
                    BookingId = Guid.NewGuid(),
                    TicketSeats = new List<TicketSeat>()
                }
            };

            var ticketSeats = new List<TicketSeat>
            {
                new() { Id = Guid.NewGuid(), TicketId = tickets[0].Id, PricePerSeat = 100m },
                new() { Id = Guid.NewGuid(), TicketId = tickets[1].Id, PricePerSeat = 150m }
            };

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                BookingId = tickets[1].BookingId!.Value,
                PromotionId = null
            };

            // Use BuildMock instead of BuildMockDbSet and remove the problematic Include setup
            var mockTicketQueryable = tickets.AsQueryable().BuildMock();
            _mockTicketRepository.Setup(r => r.GetQueryable()).Returns(mockTicketQueryable);

            var mockTicketSeatQueryable = ticketSeats.AsQueryable().BuildMock();
            _mockTicketSeatRepository.Setup(r => r.GetQueryable()).Returns(mockTicketSeatQueryable);

            var mockInvoiceQueryable = new List<Invoice> { invoice }.AsQueryable().BuildMock();
            _mockInvoiceRepository.Setup(r => r.GetQueryable()).Returns(mockInvoiceQueryable);

            var mockScoreHistoryQueryable = new List<ScoreHistory>().AsQueryable().BuildMock();
            _mockScoreHistoryRepository.Setup(r => r.GetQueryable()).Returns(mockScoreHistoryQueryable);

            // Act
            var result = await _statisticService.GetMonthlyRevenueMovieAsync(monthYear);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            var movie1Result = result.FirstOrDefault(r => r.MovieId == movieId1);
            Assert.NotNull(movie1Result);
            Assert.Equal("Movie 1", movie1Result.MovieName);
            Assert.Equal(100m, movie1Result.TotalRevenue);
            Assert.Equal(1, movie1Result.TotalTickets);

            // Verify logging
            _mockLoggerService.Verify(l => l.Info("Starting GetMonthlyRevenueMovieAsync"), Times.Once);
        }

        [Fact]
        public async Task GetMonthlyRevenueMovieAsync_OnlineTicketMissingBookingId_ThrowsException()
        {
            // Arrange
            var monthYear = new MonthYearDto(6, 2023);
            var movieId = Guid.NewGuid();
            var movie = new Movie { Id = movieId, Name = "Test Movie" };
            var showtime = new ShowTime { Id = Guid.NewGuid(), MovieId = movieId, Movie = movie };

            var ticket = new Ticket
            {
                Id = Guid.NewGuid(),
                IsDeleted = false,
                CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                TicketType = TicketType.Online,
                Showtime = showtime,
                BookingId = null, // Missing BookingId
                TicketSeats = new List<TicketSeat>()
            };

            var ticketSeat = new TicketSeat { Id = Guid.NewGuid(), TicketId = ticket.Id, PricePerSeat = 100m };

            var mockTicketQueryable = new List<Ticket> { ticket }.AsQueryable().BuildMock();
            _mockTicketRepository.Setup(r => r.GetQueryable()).Returns(mockTicketQueryable);

            var mockTicketSeatQueryable = new List<TicketSeat> { ticketSeat }.AsQueryable().BuildMock();
            _mockTicketSeatRepository.Setup(r => r.GetQueryable()).Returns(mockTicketSeatQueryable);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _statisticService.GetMonthlyRevenueMovieAsync(monthYear));

            Assert.Contains($"Ticket {ticket.Id} missing BookingId", ex.Message);

            // Verify warning logging
            _mockLoggerService.Verify(
                l => l.Warn($"[RevenueCalc] Missing BookingId on Ticket {ticket.Id}"),
                Times.Once);
        }

        [Fact]
        public async Task GetMonthlyRevenueMovieAsync_InvoiceNotFound_ThrowsException()
        {
            // Arrange
            var monthYear = new MonthYearDto(6, 2023);
            var movieId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var movie = new Movie { Id = movieId, Name = "Test Movie" };
            var showtime = new ShowTime { Id = Guid.NewGuid(), MovieId = movieId, Movie = movie };

            var ticket = new Ticket
            {
                Id = Guid.NewGuid(),
                IsDeleted = false,
                CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                TicketType = TicketType.Online,
                Showtime = showtime,
                BookingId = bookingId,
                TicketSeats = new List<TicketSeat>()
            };

            var ticketSeat = new TicketSeat { Id = Guid.NewGuid(), TicketId = ticket.Id, PricePerSeat = 100m };

            // Remove the problematic Include setup and use BuildMock instead of BuildMockDbSet
            var mockTicketQueryable = new List<Ticket> { ticket }.AsQueryable().BuildMock();
            _mockTicketRepository.Setup(r => r.GetQueryable()).Returns(mockTicketQueryable);

            var mockTicketSeatQueryable = new List<TicketSeat> { ticketSeat }.AsQueryable().BuildMock();
            _mockTicketSeatRepository.Setup(r => r.GetQueryable()).Returns(mockTicketSeatQueryable);

            var mockInvoiceQueryable = new List<Invoice>().AsQueryable().BuildMock(); // Empty invoice list
            _mockInvoiceRepository.Setup(r => r.GetQueryable()).Returns(mockInvoiceQueryable);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _statisticService.GetMonthlyRevenueMovieAsync(monthYear));

            Assert.Contains($"Invoice not found for Ticket {ticket.Id} with BookingId {bookingId}", ex.Message);

            // Verify warning logging
            _mockLoggerService.Verify(
                l => l.Warn($"[RevenueCalc] Invoice not found for Ticket {ticket.Id} with BookingId {bookingId}"),
                Times.Once);
        }

        [Fact]
        public async Task GetMonthlyRevenueMovieAsync_WithPromotionDiscount_CalculatesCorrectRevenue()
        {
            // Arrange
            var monthYear = new MonthYearDto(6, 2023);
            var movieId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var promotionId = Guid.NewGuid();

            var movie = new Movie { Id = movieId, Name = "Test Movie" };
            var showtime = new ShowTime { Id = Guid.NewGuid(), MovieId = movieId, Movie = movie };
            var promotion = new Promotion { Id = promotionId, DiscountValue = 0.2m }; // 20% discount

            var ticket = new Ticket
            {
                Id = Guid.NewGuid(),
                IsDeleted = false,
                CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                TicketType = TicketType.Online,
                Showtime = showtime,
                BookingId = bookingId,
                TicketSeats = new List<TicketSeat>()
            };

            var ticketSeat = new TicketSeat { Id = Guid.NewGuid(), TicketId = ticket.Id, PricePerSeat = 100m };

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                PromotionId = promotionId
            };

            // Use BuildMock instead of BuildMockDbSet and remove the problematic Include setup
            var mockTicketQueryable = new List<Ticket> { ticket }.AsQueryable().BuildMock();
            _mockTicketRepository.Setup(r => r.GetQueryable()).Returns(mockTicketQueryable);

            var mockTicketSeatQueryable = new List<TicketSeat> { ticketSeat }.AsQueryable().BuildMock();
            _mockTicketSeatRepository.Setup(r => r.GetQueryable()).Returns(mockTicketSeatQueryable);

            var mockInvoiceQueryable = new List<Invoice> { invoice }.AsQueryable().BuildMock();
            _mockInvoiceRepository.Setup(r => r.GetQueryable()).Returns(mockInvoiceQueryable);

            _mockPromotionRepository.Setup(r => r.GetByIdAsync(promotionId)).ReturnsAsync(promotion);

            var mockScoreHistoryQueryable = new List<ScoreHistory>().AsQueryable().BuildMock();
            _mockScoreHistoryRepository.Setup(r => r.GetQueryable()).Returns(mockScoreHistoryQueryable);

            // Act
            var result = await _statisticService.GetMonthlyRevenueMovieAsync(monthYear);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            var movieResult = result.First();
            Assert.Equal(movieId, movieResult.MovieId);
            Assert.Equal("Test Movie", movieResult.MovieName);
            Assert.Equal(80m, movieResult.TotalRevenue); // 100 - (100 * 0.2) = 80
            Assert.Equal(1, movieResult.TotalTickets);

            // Verify promotion logging - Use It.Is to match the pattern since decimal formatting varies by culture
            _mockLoggerService.Verify(
                l => l.Info(It.Is<string>(s =>
                    s.Contains($"Applied promotion with discount {promotion.DiscountValue}") &&
                    s.Contains($"to tickets {ticket.Id}") &&
                    s.Contains("New seat revenue: 80"))),
                Times.Once);
        }

        [Fact]
        public async Task GetMonthlyFoodAndDrinkRevenueAsync_ReturnsCorrectData()
        {
            // Arrange
            var monthYear = new MonthYearDto(6, 2023);
            var foodId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();

            var foodAndDrink = new FoodAndDrink { Id = foodId, Name = "Popcorn", Price = 50m };

            var ticketFoodAndDrink = new TicketFoodAndDrink
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                FoodAndDrinkId = foodId,
                FoodAndDrink = foodAndDrink,
                Quantity = 2
            };

            var ticket = new Ticket
            {
                Id = ticketId,
                IsDeleted = false,
                CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                TicketType = TicketType.Offline,
                TicketFoodAndDrinks = new List<TicketFoodAndDrink> { ticketFoodAndDrink }
            };

            // Set the reverse navigation property
            ticketFoodAndDrink.Ticket = ticket;

            // Create a mock queryable that simulates the Include behavior by having the navigation properties populated
            var tickets = new List<Ticket> { ticket };
            var mockTicketQueryable = tickets.AsQueryable().BuildMock();

            _mockTicketRepository.Setup(r => r.GetQueryable()).Returns(mockTicketQueryable);

            // Act
            var result = await _statisticService.GetMonthlyFoodAndDrinkRevenueAsync(monthYear);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            var foodResult = result.First();
            Assert.Equal(foodId, foodResult.FoodAndDrinkId);
            Assert.Equal("Popcorn", foodResult.FoodAndDrinkName);
            Assert.Equal(100m, foodResult.TotalRevenue); // 50 * 2 = 100
            Assert.Equal(2, foodResult.TotalSold);

            // Verify logging
            _mockLoggerService.Verify(l => l.Info("Starting GetMonthlyFoodAndDrinkRevenueAsync"), Times.Once);
        }

        [Fact]
        public async Task GetMonthlyTicketTypeStatisticsAsync_ReturnsCorrectStatistics()
        {
            // Arrange
            var monthYear = new MonthYearDto(6, 2023);
            var memberPhoneNumber = "1234567890";
            var guestPhoneNumber = "0987654321";

            var tickets = new List<Ticket>
            {
                new() { Id = Guid.NewGuid(), IsDeleted = false, CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc), TicketType = TicketType.Online, GuestPhoneNumber = memberPhoneNumber },
                new() { Id = Guid.NewGuid(), IsDeleted = false, CreatedAt = new DateTime(2023, 6, 5, 0, 0, 0, DateTimeKind.Utc), TicketType = TicketType.Online, GuestPhoneNumber = guestPhoneNumber },
                new() { Id = Guid.NewGuid(), IsDeleted = false, CreatedAt = new DateTime(2023, 6, 10, 0, 0, 0, DateTimeKind.Utc), TicketType = TicketType.Offline, GuestPhoneNumber = memberPhoneNumber },
                new() { Id = Guid.NewGuid(), IsDeleted = false, CreatedAt = new DateTime(2023, 6, 15, 0, 0, 0, DateTimeKind.Utc), TicketType = TicketType.Offline, GuestPhoneNumber = guestPhoneNumber }
            };

            var users = new List<User>
            {
                new() { Id = Guid.NewGuid(), Role = RoleType.Member, IsDeleted = false, PhoneNumber = memberPhoneNumber }
            };

            var mockTicketQueryable = tickets.AsQueryable().BuildMock();
            _mockTicketRepository.Setup(r => r.GetQueryable()).Returns(mockTicketQueryable);

            var mockUserQueryable = users.AsQueryable().BuildMock();
            _mockUserRepository.Setup(r => r.GetQueryable()).Returns(mockUserQueryable);

            // Act
            var result = await _statisticService.GetMonthlyTicketTypeStatisticsAsync(monthYear);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.OnlineTicketCount);
            Assert.Equal(2, result.OfflineTicketCount);
            Assert.Equal(2, result.GuestTicketCount); // Tickets with guestPhoneNumber not in member list
            Assert.Equal(4, result.TicketCount);

            // Verify logging
            _mockLoggerService.Verify(
                l => l.Info($"Starting GetMonthlyTicketTypeStatisticsAsync for {monthYear.Month}/{monthYear.Year}"),
                Times.Once);
        }

        [Fact]
        public async Task GetMonthlyTicketTypeStatisticsAsync_ThrowsException_LogsErrorAndRethraws()
        {
            // Arrange
            var monthYear = new MonthYearDto(6, 2023);
            var expectedErrorMessage = "Database connection lost";
            _mockTicketRepository.Setup(r => r.GetQueryable()).Throws(new Exception(expectedErrorMessage));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _statisticService.GetMonthlyTicketTypeStatisticsAsync(monthYear));

            Assert.Equal(expectedErrorMessage, ex.Message);

            // Verify error logging
            _mockLoggerService.Verify(
                l => l.Error($"Error in GetMonthlyTicketTypeStatisticsAsync: {expectedErrorMessage}"),
                Times.Once);
        }

        [Fact]
        public async Task GetMonthlyRevenueMovieAsync_ThrowsException_LogsErrorAndRethraws()
        {
            // Arrange
            var monthYear = new MonthYearDto(6, 2023);
            var expectedErrorMessage = "Database timeout";
            _mockTicketRepository.Setup(r => r.GetQueryable()).Throws(new Exception(expectedErrorMessage));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _statisticService.GetMonthlyRevenueMovieAsync(monthYear));

            Assert.Equal(expectedErrorMessage, ex.Message);

            // Verify error logging
            _mockLoggerService.Verify(
                l => l.Error($"[RevenueCalc] Exception: {expectedErrorMessage}"),
                Times.Once);
        }

        [Fact]
        public async Task GetMonthlyFoodAndDrinkRevenueAsync_ThrowsException_LogsErrorAndRethraws()
        {
            // Arrange
            var monthYear = new MonthYearDto(6, 2023);
            var expectedErrorMessage = "Network error";
            _mockTicketRepository.Setup(r => r.GetQueryable()).Throws(new Exception(expectedErrorMessage));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _statisticService.GetMonthlyFoodAndDrinkRevenueAsync(monthYear));

            Assert.Equal(expectedErrorMessage, ex.Message);

            // Verify error logging
            _mockLoggerService.Verify(
                l => l.Error($"[FoodRevenueCalc] Exception: {expectedErrorMessage}"),
                Times.Once);
        }

        [Fact]
        public async Task GetMonthlyRevenueMovieAsync_WithScoreDiscount_CalculatesCorrectRevenue()
        {
            // Arrange
            var monthYear = new MonthYearDto(6, 2023);
            var movieId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();

            var movie = new Movie { Id = movieId, Name = "Test Movie" };
            var showtime = new ShowTime { Id = Guid.NewGuid(), MovieId = movieId, Movie = movie };

            var ticket = new Ticket
            {
                Id = Guid.NewGuid(),
                IsDeleted = false,
                CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                TicketType = TicketType.Online,
                Showtime = showtime,
                BookingId = bookingId,
                TicketSeats = new List<TicketSeat>()
            };

            var ticketSeat = new TicketSeat { Id = Guid.NewGuid(), TicketId = ticket.Id, PricePerSeat = 100m };

            // Add the ticket seat to the ticket's collection to simulate the Include
            ticket.TicketSeats.Add(ticketSeat);

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                PromotionId = null
            };

            var scoreHistory = new ScoreHistory
            {
                Id = Guid.NewGuid(),
                RelatedBookingId = bookingId,
                ChangeType = ScoreChangeType.Use,
                ScoreValue = -50 // 50% discount
            };

            // Setup the ticket repository with proper navigation properties
            var mockTicketQueryable = new List<Ticket> { ticket }.AsQueryable().BuildMock();
            _mockTicketRepository.Setup(r => r.GetQueryable()).Returns(mockTicketQueryable);

            var mockTicketSeatQueryable = new List<TicketSeat> { ticketSeat }.AsQueryable().BuildMock();
            _mockTicketSeatRepository.Setup(r => r.GetQueryable()).Returns(mockTicketSeatQueryable);

            var mockInvoiceQueryable = new List<Invoice> { invoice }.AsQueryable().BuildMock();
            _mockInvoiceRepository.Setup(r => r.GetQueryable()).Returns(mockInvoiceQueryable);

            var mockScoreHistoryQueryable = new List<ScoreHistory> { scoreHistory }.AsQueryable().BuildMock();
            _mockScoreHistoryRepository.Setup(r => r.GetQueryable()).Returns(mockScoreHistoryQueryable);

            // Act
            var result = await _statisticService.GetMonthlyRevenueMovieAsync(monthYear);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            var movieResult = result.First();
            Assert.Equal(movieId, movieResult.MovieId);
            Assert.Equal("Test Movie", movieResult.MovieName);
            Assert.Equal(50m, movieResult.TotalRevenue); // 100 - (100 * 0.5) = 50
            Assert.Equal(1, movieResult.TotalTickets);

            // Verify score discount logging - Use It.Is to match the pattern since decimal formatting varies by culture
            _mockLoggerService.Verify(
                l => l.Info(It.Is<string>(s =>
                    s.Contains("Applied score deduction of 50") &&
                    s.Contains($"to tickets {ticket.Id}") &&
                    s.Contains("New seat revenue: 50"))),
                Times.Once);
        }

        [Fact]
        public async Task GetMonthlyFoodAndDrinkRevenueAsync_OnlineTicketMissingBookingId_ThrowsException()
        {
            // Arrange
            var monthYear = new MonthYearDto(6, 2023);
            var foodId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();

            var foodAndDrink = new FoodAndDrink { Id = foodId, Name = "Popcorn", Price = 50m };

            var ticketFoodAndDrink = new TicketFoodAndDrink
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                FoodAndDrinkId = foodId,
                FoodAndDrink = foodAndDrink,
                Quantity = 2
            };

            var ticket = new Ticket
            {
                Id = ticketId,
                IsDeleted = false,
                CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                TicketType = TicketType.Online,
                BookingId = null, // Missing BookingId
                TicketFoodAndDrinks = new List<TicketFoodAndDrink> { ticketFoodAndDrink }
            };

            ticketFoodAndDrink.Ticket = ticket;

            var tickets = new List<Ticket> { ticket };
            var mockTicketQueryable = tickets.AsQueryable().BuildMock();
            _mockTicketRepository.Setup(r => r.GetQueryable()).Returns(mockTicketQueryable);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _statisticService.GetMonthlyFoodAndDrinkRevenueAsync(monthYear));

            Assert.Contains($"Ticket {ticket.Id} missing BookingId", ex.Message);

            // Verify warning logging
            _mockLoggerService.Verify(
                l => l.Warn($"[FoodRevenueCalc] Missing BookingId on Ticket {ticket.Id}"),
                Times.Once);
        }

        [Fact]
        public async Task GetMonthlyFoodAndDrinkRevenueAsync_InvoiceNotFound_ThrowsException()
        {
            // Arrange
            var monthYear = new MonthYearDto(6, 2023);
            var foodId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();

            var foodAndDrink = new FoodAndDrink { Id = foodId, Name = "Popcorn", Price = 50m };

            var ticketFoodAndDrink = new TicketFoodAndDrink
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                FoodAndDrinkId = foodId,
                FoodAndDrink = foodAndDrink,
                Quantity = 2
            };

            var ticket = new Ticket
            {
                Id = ticketId,
                IsDeleted = false,
                CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                TicketType = TicketType.Online,
                BookingId = bookingId,
                TicketFoodAndDrinks = new List<TicketFoodAndDrink> { ticketFoodAndDrink }
            };

            ticketFoodAndDrink.Ticket = ticket;

            var tickets = new List<Ticket> { ticket };
            var mockTicketQueryable = tickets.AsQueryable().BuildMock();
            _mockTicketRepository.Setup(r => r.GetQueryable()).Returns(mockTicketQueryable);

            var mockInvoiceQueryable = new List<Invoice>().AsQueryable().BuildMock(); // Empty invoice list
            _mockInvoiceRepository.Setup(r => r.GetQueryable()).Returns(mockInvoiceQueryable);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _statisticService.GetMonthlyFoodAndDrinkRevenueAsync(monthYear));

            Assert.Contains($"Invoice not found for Ticket {ticket.Id} with BookingId {bookingId}", ex.Message);

            // Verify warning logging
            _mockLoggerService.Verify(
                l => l.Warn($"[FoodRevenueCalc] Invoice not found for Ticket {ticket.Id} with BookingId {bookingId}"),
                Times.Once);
        }

        [Fact]
        public async Task GetMonthlyFoodAndDrinkRevenueAsync_WithPromotionDiscount_CalculatesCorrectRevenue()
        {
            // Arrange
            var monthYear = new MonthYearDto(6, 2023);
            var foodId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var promotionId = Guid.NewGuid();

            var foodAndDrink = new FoodAndDrink { Id = foodId, Name = "Popcorn", Price = 50m };
            var promotion = new Promotion { Id = promotionId, DiscountValue = 0.2m }; // 20% discount

            var ticketFoodAndDrink = new TicketFoodAndDrink
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                FoodAndDrinkId = foodId,
                FoodAndDrink = foodAndDrink,
                Quantity = 2
            };

            var ticket = new Ticket
            {
                Id = ticketId,
                IsDeleted = false,
                CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                TicketType = TicketType.Online,
                BookingId = bookingId,
                TicketFoodAndDrinks = new List<TicketFoodAndDrink> { ticketFoodAndDrink }
            };

            ticketFoodAndDrink.Ticket = ticket;

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                PromotionId = promotionId
            };

            var tickets = new List<Ticket> { ticket };
            var mockTicketQueryable = tickets.AsQueryable().BuildMock();
            _mockTicketRepository.Setup(r => r.GetQueryable()).Returns(mockTicketQueryable);

            var mockInvoiceQueryable = new List<Invoice> { invoice }.AsQueryable().BuildMock();
            _mockInvoiceRepository.Setup(r => r.GetQueryable()).Returns(mockInvoiceQueryable);

            _mockPromotionRepository.Setup(r => r.GetByIdAsync(promotionId)).ReturnsAsync(promotion);

            var mockScoreHistoryQueryable = new List<ScoreHistory>().AsQueryable().BuildMock();
            _mockScoreHistoryRepository.Setup(r => r.GetQueryable()).Returns(mockScoreHistoryQueryable);

            // Act
            var result = await _statisticService.GetMonthlyFoodAndDrinkRevenueAsync(monthYear);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            var foodResult = result.First();
            Assert.Equal(foodId, foodResult.FoodAndDrinkId);
            Assert.Equal("Popcorn", foodResult.FoodAndDrinkName);
            Assert.Equal(80m, foodResult.TotalRevenue); // (50 * 2) - (100 * 0.2) = 100 - 20 = 80
            Assert.Equal(2, foodResult.TotalSold);

            // Verify promotion logging
            _mockLoggerService.Verify(
                l => l.Info(It.Is<string>(s =>
                    s.Contains($"Applied promotion with discount {promotion.DiscountValue}") &&
                    s.Contains("to food and drink Popcorn") &&
                    s.Contains("New seat revenue: 80"))),
                Times.Once);
        }

        [Fact]
        public async Task GetMonthlyFoodAndDrinkRevenueAsync_PromotionNotFound_ThrowsException()
        {
            // Arrange
            var monthYear = new MonthYearDto(6, 2023);
            var foodId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var promotionId = Guid.NewGuid();

            var foodAndDrink = new FoodAndDrink { Id = foodId, Name = "Popcorn", Price = 50m };

            var ticketFoodAndDrink = new TicketFoodAndDrink
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                FoodAndDrinkId = foodId,
                FoodAndDrink = foodAndDrink,
                Quantity = 2
            };

            var ticket = new Ticket
            {
                Id = ticketId,
                IsDeleted = false,
                CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                TicketType = TicketType.Online,
                BookingId = bookingId,
                TicketFoodAndDrinks = new List<TicketFoodAndDrink> { ticketFoodAndDrink }
            };

            ticketFoodAndDrink.Ticket = ticket;

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                PromotionId = promotionId
            };

            var tickets = new List<Ticket> { ticket };
            var mockTicketQueryable = tickets.AsQueryable().BuildMock();
            _mockTicketRepository.Setup(r => r.GetQueryable()).Returns(mockTicketQueryable);

            var mockInvoiceQueryable = new List<Invoice> { invoice }.AsQueryable().BuildMock();
            _mockInvoiceRepository.Setup(r => r.GetQueryable()).Returns(mockInvoiceQueryable);

            _mockPromotionRepository.Setup(r => r.GetByIdAsync(promotionId)).ReturnsAsync((Promotion)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _statisticService.GetMonthlyFoodAndDrinkRevenueAsync(monthYear));

            Assert.Contains($"Promotion {promotionId} not found for Invoice {invoice.Id}", ex.Message);
        }

        [Fact]
        public async Task GetMonthlyFoodAndDrinkRevenueAsync_WithScoreDiscount_CalculatesCorrectRevenue()
        {
            // Arrange
            var monthYear = new MonthYearDto(6, 2023);
            var foodId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();

            var foodAndDrink = new FoodAndDrink { Id = foodId, Name = "Popcorn", Price = 50m };

            var ticketFoodAndDrink = new TicketFoodAndDrink
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                FoodAndDrinkId = foodId,
                FoodAndDrink = foodAndDrink,
                Quantity = 2
            };

            var ticket = new Ticket
            {
                Id = ticketId,
                IsDeleted = false,
                CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                TicketType = TicketType.Online,
                BookingId = bookingId,
                TicketFoodAndDrinks = new List<TicketFoodAndDrink> { ticketFoodAndDrink }
            };

            ticketFoodAndDrink.Ticket = ticket;

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                PromotionId = null
            };

            var scoreHistory = new ScoreHistory
            {
                Id = Guid.NewGuid(),
                RelatedBookingId = bookingId,
                ChangeType = ScoreChangeType.Use,
                ScoreValue = -30 // 30% discount
            };

            var tickets = new List<Ticket> { ticket };
            var mockTicketQueryable = tickets.AsQueryable().BuildMock();
            _mockTicketRepository.Setup(r => r.GetQueryable()).Returns(mockTicketQueryable);

            var mockInvoiceQueryable = new List<Invoice> { invoice }.AsQueryable().BuildMock();
            _mockInvoiceRepository.Setup(r => r.GetQueryable()).Returns(mockInvoiceQueryable);

            var mockScoreHistoryQueryable = new List<ScoreHistory> { scoreHistory }.AsQueryable().BuildMock();
            _mockScoreHistoryRepository.Setup(r => r.GetQueryable()).Returns(mockScoreHistoryQueryable);

            // Act
            var result = await _statisticService.GetMonthlyFoodAndDrinkRevenueAsync(monthYear);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            var foodResult = result.First();
            Assert.Equal(foodId, foodResult.FoodAndDrinkId);
            Assert.Equal("Popcorn", foodResult.FoodAndDrinkName);
            Assert.Equal(70m, foodResult.TotalRevenue); // (50 * 2) - (100 * 0.3) = 100 - 30 = 70
            Assert.Equal(2, foodResult.TotalSold);

            // Verify score discount logging
            _mockLoggerService.Verify(
                l => l.Info(It.Is<string>(s =>
                    s.Contains("Applied score deduction of 30") &&
                    s.Contains("to food and drink Popcorn") &&
                    s.Contains("New seat revenue: 70"))),
                Times.Once);
        }

        [Fact]
        public async Task GetMonthlyFoodAndDrinkRevenueAsync_WithBothPromotionAndScoreDiscounts_CalculatesCorrectRevenue()
        {
            // Arrange
            var monthYear = new MonthYearDto(6, 2023);
            var foodId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var promotionId = Guid.NewGuid();

            var foodAndDrink = new FoodAndDrink { Id = foodId, Name = "Popcorn", Price = 50m };
            var promotion = new Promotion { Id = promotionId, DiscountValue = 0.2m }; // 20% discount

            var ticketFoodAndDrink = new TicketFoodAndDrink
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                FoodAndDrinkId = foodId,
                FoodAndDrink = foodAndDrink,
                Quantity = 2
            };

            var ticket = new Ticket
            {
                Id = ticketId,
                IsDeleted = false,
                CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                TicketType = TicketType.Online,
                BookingId = bookingId,
                TicketFoodAndDrinks = new List<TicketFoodAndDrink> { ticketFoodAndDrink }
            };

            ticketFoodAndDrink.Ticket = ticket;

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                PromotionId = promotionId
            };

            var scoreHistory = new ScoreHistory
            {
                Id = Guid.NewGuid(),
                RelatedBookingId = bookingId,
                ChangeType = ScoreChangeType.Use,
                ScoreValue = -25 // 25% discount
            };

            var tickets = new List<Ticket> { ticket };
            var mockTicketQueryable = tickets.AsQueryable().BuildMock();
            _mockTicketRepository.Setup(r => r.GetQueryable()).Returns(mockTicketQueryable);

            var mockInvoiceQueryable = new List<Invoice> { invoice }.AsQueryable().BuildMock();
            _mockInvoiceRepository.Setup(r => r.GetQueryable()).Returns(mockInvoiceQueryable);

            _mockPromotionRepository.Setup(r => r.GetByIdAsync(promotionId)).ReturnsAsync(promotion);

            var mockScoreHistoryQueryable = new List<ScoreHistory> { scoreHistory }.AsQueryable().BuildMock();
            _mockScoreHistoryRepository.Setup(r => r.GetQueryable()).Returns(mockScoreHistoryQueryable);

            // Act
            var result = await _statisticService.GetMonthlyFoodAndDrinkRevenueAsync(monthYear);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            var foodResult = result.First();
            Assert.Equal(foodId, foodResult.FoodAndDrinkId);
            Assert.Equal("Popcorn", foodResult.FoodAndDrinkName);
            // Original: 100, Promotion discount: 20, Score discount: 25, Final: 100 - 20 - 25 = 55
            Assert.Equal(55m, foodResult.TotalRevenue);
            Assert.Equal(2, foodResult.TotalSold);

            // Verify both promotion and score discount logging
            _mockLoggerService.Verify(
                l => l.Info(It.Is<string>(s =>
                    s.Contains($"Applied promotion with discount {promotion.DiscountValue}") &&
                    s.Contains("to food and drink Popcorn"))),
                Times.Once);

            _mockLoggerService.Verify(
                l => l.Info(It.Is<string>(s =>
                    s.Contains("Applied score deduction of 25") &&
                    s.Contains("to food and drink Popcorn"))),
                Times.Once);
        }

        [Fact]
        public async Task GetMonthlyFoodAndDrinkRevenueAsync_ExcessiveDiscountsResultInZeroRevenue_CalculatesCorrectly()
        {
            // Arrange
            var monthYear = new MonthYearDto(6, 2023);
            var foodId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var promotionId = Guid.NewGuid();

            var foodAndDrink = new FoodAndDrink { Id = foodId, Name = "Popcorn", Price = 50m };
            var promotion = new Promotion { Id = promotionId, DiscountValue = 0.6m }; // 60% discount

            var ticketFoodAndDrink = new TicketFoodAndDrink
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                FoodAndDrinkId = foodId,
                FoodAndDrink = foodAndDrink,
                Quantity = 2
            };

            var ticket = new Ticket
            {
                Id = ticketId,
                IsDeleted = false,
                CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                TicketType = TicketType.Online,
                BookingId = bookingId,
                TicketFoodAndDrinks = new List<TicketFoodAndDrink> { ticketFoodAndDrink }
            };

            ticketFoodAndDrink.Ticket = ticket;

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                PromotionId = promotionId
            };

            var scoreHistory = new ScoreHistory
            {
                Id = Guid.NewGuid(),
                RelatedBookingId = bookingId,
                ChangeType = ScoreChangeType.Use,
                ScoreValue = -50 // 50% discount
            };

            var tickets = new List<Ticket> { ticket };
            var mockTicketQueryable = tickets.AsQueryable().BuildMock();
            _mockTicketRepository.Setup(r => r.GetQueryable()).Returns(mockTicketQueryable);

            var mockInvoiceQueryable = new List<Invoice> { invoice }.AsQueryable().BuildMock();
            _mockInvoiceRepository.Setup(r => r.GetQueryable()).Returns(mockInvoiceQueryable);

            _mockPromotionRepository.Setup(r => r.GetByIdAsync(promotionId)).ReturnsAsync(promotion);

            var mockScoreHistoryQueryable = new List<ScoreHistory> { scoreHistory }.AsQueryable().BuildMock();
            _mockScoreHistoryRepository.Setup(r => r.GetQueryable()).Returns(mockScoreHistoryQueryable);

            // Act
            var result = await _statisticService.GetMonthlyFoodAndDrinkRevenueAsync(monthYear);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            var foodResult = result.First();
            Assert.Equal(foodId, foodResult.FoodAndDrinkId);
            Assert.Equal("Popcorn", foodResult.FoodAndDrinkName);
            // Original: 100, Promotion discount: 60, Score discount: 50, Total discount: 110, but Math.Max(0, seatRevenue) ensures it's 0
            Assert.Equal(0m, foodResult.TotalRevenue);
            Assert.Equal(2, foodResult.TotalSold);
        }

        [Fact]
        public async Task GetMonthlyFoodAndDrinkRevenueAsync_ScoreDiscountCappedAt100Percent_CalculatesCorrectly()
        {
            // Arrange
            var monthYear = new MonthYearDto(6, 2023);
            var foodId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();

            var foodAndDrink = new FoodAndDrink { Id = foodId, Name = "Popcorn", Price = 50m };

            var ticketFoodAndDrink = new TicketFoodAndDrink
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                FoodAndDrinkId = foodId,
                FoodAndDrink = foodAndDrink,
                Quantity = 2
            };

            var ticket = new Ticket
            {
                Id = ticketId,
                IsDeleted = false,
                CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                TicketType = TicketType.Online,
                BookingId = bookingId,
                TicketFoodAndDrinks = new List<TicketFoodAndDrink> { ticketFoodAndDrink }
            };

            ticketFoodAndDrink.Ticket = ticket;

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                PromotionId = null
            };

            var scoreHistory = new ScoreHistory
            {
                Id = Guid.NewGuid(),
                RelatedBookingId = bookingId,
                ChangeType = ScoreChangeType.Use,
                ScoreValue = -150 // 150% discount, but should be capped at 100%
            };

            var tickets = new List<Ticket> { ticket };
            var mockTicketQueryable = tickets.AsQueryable().BuildMock();
            _mockTicketRepository.Setup(r => r.GetQueryable()).Returns(mockTicketQueryable);

            var mockInvoiceQueryable = new List<Invoice> { invoice }.AsQueryable().BuildMock();
            _mockInvoiceRepository.Setup(r => r.GetQueryable()).Returns(mockInvoiceQueryable);

            var mockScoreHistoryQueryable = new List<ScoreHistory> { scoreHistory }.AsQueryable().BuildMock();
            _mockScoreHistoryRepository.Setup(r => r.GetQueryable()).Returns(mockScoreHistoryQueryable);

            // Act
            var result = await _statisticService.GetMonthlyFoodAndDrinkRevenueAsync(monthYear);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            var foodResult = result.First();
            Assert.Equal(foodId, foodResult.FoodAndDrinkId);
            Assert.Equal("Popcorn", foodResult.FoodAndDrinkName);
            // Original: 100, Score discount capped at 100%, Final: 100 - 100 = 0
            Assert.Equal(0m, foodResult.TotalRevenue);
            Assert.Equal(2, foodResult.TotalSold);

            // Verify score discount logging shows the original value (150), not the capped value
            // This is because the implementation logs Math.Abs(scoreHistory.ScoreValue) rather than the capped amount
            _mockLoggerService.Verify(
                l => l.Info(It.Is<string>(s =>
                    s.Contains("Applied score deduction of 150") && // Shows original value, not capped
                    s.Contains("to food and drink Popcorn") &&
                    s.Contains("New seat revenue: 0"))), // But the revenue is correctly capped
                Times.Once);
        }

        [Fact]
        public async Task GetMonthlyFoodAndDrinkRevenueAsync_OnlineTicketWithoutPromotionOrScore_CalculatesCorrectRevenue()
        {
            // Arrange
            var monthYear = new MonthYearDto(6, 2023);
            var foodId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();

            var foodAndDrink = new FoodAndDrink { Id = foodId, Name = "Popcorn", Price = 50m };

            var ticketFoodAndDrink = new TicketFoodAndDrink
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                FoodAndDrinkId = foodId,
                FoodAndDrink = foodAndDrink,
                Quantity = 3
            };

            var ticket = new Ticket
            {
                Id = ticketId,
                IsDeleted = false,
                CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                TicketType = TicketType.Online,
                BookingId = bookingId,
                TicketFoodAndDrinks = new List<TicketFoodAndDrink> { ticketFoodAndDrink }
            };

            ticketFoodAndDrink.Ticket = ticket;

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                PromotionId = null
            };

            var tickets = new List<Ticket> { ticket };
            var mockTicketQueryable = tickets.AsQueryable().BuildMock();
            _mockTicketRepository.Setup(r => r.GetQueryable()).Returns(mockTicketQueryable);

            var mockInvoiceQueryable = new List<Invoice> { invoice }.AsQueryable().BuildMock();
            _mockInvoiceRepository.Setup(r => r.GetQueryable()).Returns(mockInvoiceQueryable);

            var mockScoreHistoryQueryable = new List<ScoreHistory>().AsQueryable().BuildMock(); // No score history
            _mockScoreHistoryRepository.Setup(r => r.GetQueryable()).Returns(mockScoreHistoryQueryable);

            // Act
            var result = await _statisticService.GetMonthlyFoodAndDrinkRevenueAsync(monthYear);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            var foodResult = result.First();
            Assert.Equal(foodId, foodResult.FoodAndDrinkId);
            Assert.Equal("Popcorn", foodResult.FoodAndDrinkName);
            Assert.Equal(150m, foodResult.TotalRevenue); // 50 * 3 = 150 (no discounts applied)
            Assert.Equal(3, foodResult.TotalSold);

            // Verify initial revenue calculation logging
            _mockLoggerService.Verify(
                l => l.Info(It.Is<string>(s =>
                    s.Contains($"Calculating revenue for Ticket {ticket.Id}") &&
                    s.Contains("with Food and Drink Popcorn") &&
                    s.Contains("Initial seat revenue: 150"))),
                Times.Once);

            // Verify final revenue calculation logging
            _mockLoggerService.Verify(
                l => l.Info(It.Is<string>(s =>
                    s.Contains($"Ticket {ticket.Id} revenue calculated: 150") &&
                    s.Contains("Total Revenue: 150"))),
                Times.Once);
        }
    }
}