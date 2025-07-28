using MockQueryable.Moq;
using Moq;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System.Linq.Expressions;

namespace MovieTheater.UnitTest.Services
{
    public class ScoreServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILoggerService> _mockLoggerService;
        private readonly Mock<IClaimsService> _mockClaimsService;
        private readonly Mock<IGenericRepository<ScoreHistory>> _mockScoreRepository;
        private readonly ScoreService _scoreService;

        public ScoreServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLoggerService = new Mock<ILoggerService>();
            _mockClaimsService = new Mock<IClaimsService>();
            _mockScoreRepository = new Mock<IGenericRepository<ScoreHistory>>();
            _mockUnitOfWork.Setup(u => u.ScoreHistories).Returns(_mockScoreRepository.Object);

            _scoreService = new ScoreService(_mockUnitOfWork.Object, _mockLoggerService.Object, _mockClaimsService.Object);
        }

        [Fact]
        public async Task AddScoreForBookingAsync_ValidUserAndBooking_AddsScoreHistory()
        {
            var user = new User { Id = Guid.NewGuid(), ScoreBalance = 0 };
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                MemberId = user.Id,
                Member = user,
                Tickets = new List<Ticket>
        {
            new Ticket
            {
                TicketSeats = new List<TicketSeat>
                {
                    new TicketSeat
                    {
                        Seat = new Seat { Type = SeatType.Normal }
                    }
                }
            }
        }
            };

            _mockScoreRepository.Setup(r => r.AddAsync(It.IsAny<ScoreHistory>()))
                .ReturnsAsync(new ScoreHistory { Id = Guid.NewGuid(), MemberId = user.Id, RelatedBookingId = booking.Id });
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.Users.Update(It.IsAny<User>()));

            await _scoreService.AddScoreForBookingAsync(user, booking);

            _mockScoreRepository.Verify(r => r.AddAsync(It.IsAny<ScoreHistory>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetScoreHistoryAsync_ReturnsScoreHistoryList()
        {
            var userId = Guid.NewGuid();
            var scoreHistories = new List<ScoreHistory>
            {
                new ScoreHistory { Id = Guid.NewGuid(), MemberId = userId, ScoreValue = 10 },
                new ScoreHistory { Id = Guid.NewGuid(), MemberId = userId, ScoreValue = 20 }
            };

            _mockClaimsService.Setup(c => c.GetCurrentUserId).Returns(userId);
            _mockScoreRepository.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ScoreHistory, bool>>>(), It.IsAny<System.Linq.Expressions.Expression<Func<ScoreHistory, object>>[]>()))
                .ReturnsAsync(scoreHistories);

            var result = await _scoreService.GetScoreHistoryAsync();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetCurrentScoreAsync_ReturnsScoreBalanceFromUser()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                ScoreBalance = 30,
                IsDeleted = false,
                Email = "test@example.com"
            };

            _mockClaimsService.Setup(c => c.GetCurrentUserId).Returns(userId);
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId)).ReturnsAsync(user);

            var result = await _scoreService.GetCurrentScoreAsync();

            Assert.Equal(30, result);
        }

        [Fact]
        public void CalculateDiscount_ValidPoints_ReturnsDiscount()
        {
            int availablePoints = 100;
            int requestedPoints = 50;

            var (discountPercent, usedPoints) = _scoreService.CalculateDiscount(availablePoints, requestedPoints);

            Assert.True(discountPercent > 0);
            Assert.Equal(requestedPoints, usedPoints);
        }

        [Fact]
        public async Task AddScoreForBookingAsync_UserIsNull_ThrowsArgumentNullException()
        {
            var booking = new Booking { Id = Guid.NewGuid(), Tickets = new List<Ticket>() };
            await Assert.ThrowsAsync<ArgumentNullException>(() => _scoreService.AddScoreForBookingAsync(null!, booking));
        }

        [Fact]
        public async Task AddScoreForBookingAsync_BookingIsNull_ThrowsArgumentNullException()
        {
            var user = new User { Id = Guid.NewGuid() };
            await Assert.ThrowsAsync<ArgumentNullException>(() => _scoreService.AddScoreForBookingAsync(user, null!));
        }

        [Fact]
        public async Task AddScoreForBookingAsync_BookingTicketsIsNull_LogsWarningAndDoesNotAddScore()
        {
            var user = new User { Id = Guid.NewGuid() };
            var booking = new Booking { Id = Guid.NewGuid(), MemberId = user.Id, Member = user, Tickets = null! };

            await _scoreService.AddScoreForBookingAsync(user, booking);

            _mockLoggerService.Verify(l => l.Warn(It.Is<string>(msg => msg.Contains("has no tickets"))), Times.Once);
            _mockScoreRepository.Verify(r => r.AddAsync(It.IsAny<ScoreHistory>()), Times.Never);
        }

        [Fact]
        public async Task AddScoreForBookingAsync_SeatIsNull_LogsWarningAndSkipsSeat()
        {
            var user = new User { Id = Guid.NewGuid() };
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                MemberId = user.Id,
                Member = user,
                Tickets = new List<Ticket>
                {
                    new Ticket
                    {
                        TicketSeats = new List<TicketSeat>
                        {
                            new TicketSeat { Seat = null!, SeatId = Guid.NewGuid(), Id = Guid.NewGuid() }
                        }
                    }
                }
            };
            var seatId = booking.Tickets.First().TicketSeats.First().SeatId;
            _mockUnitOfWork.Setup(u => u.Seats.GetByIdAsync(seatId)).ReturnsAsync((Seat)null!);

            await _scoreService.AddScoreForBookingAsync(user, booking);

            _mockLoggerService.Verify(l => l.Warn(It.Is<string>(msg => msg.Contains("Seat not found"))), Times.Once);
            _mockScoreRepository.Verify(r => r.AddAsync(It.IsAny<ScoreHistory>()), Times.Never);
        }

        [Theory]
        [InlineData(SeatType.VIP, 50)]
        [InlineData(SeatType.Couple, 100)]
        public async Task AddScoreForBookingAsync_SeatTypeVIPOrCouple_AddsCorrectScore(SeatType seatType, int expectedScore)
        {
            var user = new User { Id = Guid.NewGuid(), ScoreBalance = 0 };
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                MemberId = user.Id,
                Member = user,
                Tickets = new List<Ticket>
                {
                    new Ticket
                    {
                        TicketSeats = new List<TicketSeat>
                        {
                            new TicketSeat { Seat = new Seat { Type = seatType } }
                        }
                    }
                }
            };

            _mockScoreRepository.Setup(r => r.AddAsync(It.IsAny<ScoreHistory>()))
                .ReturnsAsync(new ScoreHistory { Id = Guid.NewGuid(), MemberId = user.Id, RelatedBookingId = booking.Id });
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.Users.Update(It.IsAny<User>()));

            await _scoreService.AddScoreForBookingAsync(user, booking);

            Assert.Equal(expectedScore, user.ScoreBalance);
            _mockScoreRepository.Verify(r => r.AddAsync(It.IsAny<ScoreHistory>()), Times.Once);
        }

        [Fact]
        public async Task AddScoreForBookingAsync_ExceptionThrown_LogsError()
        {
            var user = new User { Id = Guid.NewGuid(), ScoreBalance = 0 };
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                MemberId = user.Id,
                Member = user,
                Tickets = new List<Ticket>
                {
                    new Ticket
                    {
                        TicketSeats = new List<TicketSeat>
                        {
                            new TicketSeat { Seat = new Seat { Type = SeatType.Normal } }
                        }
                    }
                }
            };

            _mockScoreRepository.Setup(r => r.AddAsync(It.IsAny<ScoreHistory>()))
                .ThrowsAsync(new Exception("Test exception"));

            await Assert.ThrowsAsync<NullReferenceException>(() => _scoreService.AddScoreForBookingAsync(user, booking));
            _mockLoggerService.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Exception"))), Times.Once);
        }

        [Theory]
        [InlineData(0, 50)]
        [InlineData(100, 0)]
        [InlineData(0, 0)]
        public void CalculateDiscount_ZeroOrNegativePoints_ReturnsZeroDiscount(int availablePoints, int requestedPoints)
        {
            var (discountPercent, usedPoints) = _scoreService.CalculateDiscount(availablePoints, requestedPoints);
            Assert.Equal(0, discountPercent);
            Assert.Equal(0, usedPoints);
        }

        [Fact]
        public async Task UseScoreForBookingAsync_UserIsNull_ThrowsArgumentNullException()
        {
            var booking = new Booking { Id = Guid.NewGuid() };
            await Assert.ThrowsAsync<ArgumentNullException>(() => _scoreService.UseScoreForBookingAsync(null!, booking, 10));
        }

        [Fact]
        public async Task UseScoreForBookingAsync_UsedPointsZeroOrNegative_ThrowsArgumentException()
        {
            var user = new User { Id = Guid.NewGuid(), ScoreBalance = 10 };
            var booking = new Booking { Id = Guid.NewGuid(), MemberId = user.Id, Member = user };
            await Assert.ThrowsAsync<ArgumentException>(() => _scoreService.UseScoreForBookingAsync(user, booking, 0));
            await Assert.ThrowsAsync<ArgumentException>(() => _scoreService.UseScoreForBookingAsync(user, booking, -5));
        }

        [Fact]
        public async Task UseScoreForBookingAsync_UserHasInsufficientPoints_ThrowsInvalidOperationException()
        {
            var user = new User { Id = Guid.NewGuid(), ScoreBalance = 5 };
            var booking = new Booking { Id = Guid.NewGuid(), MemberId = user.Id, Member = user };
            await Assert.ThrowsAsync<ArgumentException>(() => _scoreService.UseScoreForBookingAsync(user, booking, 10));
        }

        [Fact]
        public async Task RefundScoreForBookingAsync_BookingNotFound_ThrowsKeyNotFoundException()
        {
            var bookingId = Guid.NewGuid();
            _mockUnitOfWork.Setup(u => u.Bookings.GetByIdAsync(bookingId)).ReturnsAsync((Booking)null!);

            await Assert.ThrowsAsync<ArgumentNullException>(() => _scoreService.RefundScoreForBookingAsync(bookingId));
        }

        [Fact]
        public async Task RefundScoreForBookingAsync_ScoreHistoryNotFound_ThrowsKeyNotFoundException()
        {
            var bookingId = Guid.NewGuid();
            var booking = new Booking { Id = bookingId };
            _mockUnitOfWork.Setup(u => u.Bookings.GetByIdAsync(bookingId)).ReturnsAsync(booking);
            _mockUnitOfWork.Setup(u => u.ScoreHistories.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ScoreHistory, bool>>>(), It.IsAny<System.Linq.Expressions.Expression<Func<ScoreHistory, object>>[]>()))
                .ReturnsAsync(new List<ScoreHistory>());

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _scoreService.RefundScoreForBookingAsync(bookingId));
        }

        [Fact]
        public async Task RefundScoreForBookingAsync_BookingNotFound_ThrowsArgumentNullException()
        {
            _mockUnitOfWork.Setup(u => u.Bookings.GetByIdAsync(It.IsAny<Guid>(), null!))
                           .ReturnsAsync((Booking)null!);

            await Assert.ThrowsAsync<ArgumentNullException>(() => _scoreService.RefundScoreForBookingAsync(Guid.NewGuid()));

            _mockLoggerService.Verify(l => l.Info(It.IsAny<string>()), Times.Once);
            _mockLoggerService.Verify(l => l.Error(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task RefundScoreForBookingAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            var booking = new Booking { Id = Guid.NewGuid(), MemberId = Guid.NewGuid() };
            var history = new ScoreHistory { RelatedBookingId = booking.Id, MemberId = booking.MemberId, ScoreValue = 10 };
            _mockUnitOfWork.Setup(u => u.Bookings.GetByIdAsync(booking.Id, null!)).ReturnsAsync(booking);
            _mockUnitOfWork.Setup(u => u.ScoreHistories.FirstOrDefaultAsync(It.IsAny<Expression<Func<ScoreHistory, bool>>>(), null!)).ReturnsAsync(history);
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(booking.MemberId, null!)).ReturnsAsync((User)null!);

            await Assert.ThrowsAsync<ArgumentNullException>(() => _scoreService.RefundScoreForBookingAsync(booking.Id));
            _mockLoggerService.Verify(l => l.Info(It.IsAny<string>()), Times.Once);
            _mockLoggerService.Verify(l => l.Error(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task UseScoreForBookingAsync_UserOrBookingNull_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _scoreService.UseScoreForBookingAsync(null!, new Booking(), 10));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _scoreService.UseScoreForBookingAsync(new User(), null!, 10));
            _mockLoggerService.Verify(l => l.Error(It.IsAny<string>()), Times.Exactly(4));
        }

        [Fact]
        public async Task UseScoreForBookingAsync_UsedPointsLessThanOrEqualZero_ThrowsArgumentException()
        {
            var user = new User { Id = Guid.NewGuid(), ScoreBalance = 100 };
            var booking = new Booking { Id = Guid.NewGuid() };
            await Assert.ThrowsAsync<ArgumentException>(() => _scoreService.UseScoreForBookingAsync(user, booking, 0));
            await Assert.ThrowsAsync<ArgumentException>(() => _scoreService.UseScoreForBookingAsync(user, booking, -5));
            _mockLoggerService.Verify(l => l.Error(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task UseScoreForBookingAsync_UsedPointsGreaterThanBalance_ThrowsArgumentException()
        {
            var user = new User { Id = Guid.NewGuid(), ScoreBalance = 10 };
            var booking = new Booking { Id = Guid.NewGuid() };
            await Assert.ThrowsAsync<ArgumentException>(() => _scoreService.UseScoreForBookingAsync(user, booking, 20));
            _mockLoggerService.Verify(l => l.Error(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UseScoreForBookingAsync_SuccessfulDeduction_UpdatesUserAndCreatesHistory()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                ScoreBalance = 100
            };
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                MemberId = user.Id
            };

            var mockUserRepo = new Mock<IGenericRepository<User>>();
            mockUserRepo.Setup(r => r.Update(user)).ReturnsAsync(true);
            _mockUnitOfWork.Setup(u => u.Users).Returns(mockUserRepo.Object);

            _mockUnitOfWork.Setup(u => u.ScoreHistories.AddAsync(It.IsAny<ScoreHistory>())).ReturnsAsync(new ScoreHistory());
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            await _scoreService.UseScoreForBookingAsync(user, booking, 50);

            // Assert
            Assert.Equal(50, user.ScoreBalance);
            mockUserRepo.Verify(r => r.Update(user), Times.Once);
            _mockUnitOfWork.Verify(u => u.ScoreHistories.AddAsync(It.IsAny<ScoreHistory>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            _mockLoggerService.Verify(l => l.Success(It.IsAny<string>()), Times.Once);
        }


        [Fact]
        public async Task GetCurrentScoreAsync_UserNullOrDeleted_ThrowsKeyNotFoundException()
        {
            _mockClaimsService.Setup(c => c.GetCurrentUserId).Returns(Guid.NewGuid());
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(It.IsAny<Guid>(), null!)).ReturnsAsync((User)null!);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _scoreService.GetCurrentScoreAsync());
            _mockLoggerService.Verify(l => l.Warn(It.Is<string>(s => s.Contains("User not found"))), Times.Once);
        }

        [Fact]
        public async Task GetCurrentScoreAsync_ValidUser_ReturnsScoreBalance()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                ScoreBalance = 123,
                IsDeleted = false,
                Email = "test@example.com"
            };

            _mockClaimsService.Setup(c => c.GetCurrentUserId).Returns(userId);
            _mockUnitOfWork
                .Setup(u => u.Users.GetByIdAsync(userId))
                .ReturnsAsync(user);

            var result = await _scoreService.GetCurrentScoreAsync();

            Assert.Equal(123, result);
            _mockLoggerService.Verify(l => l.Info(It.IsAny<string>()), Times.Once);
        }

    }
}