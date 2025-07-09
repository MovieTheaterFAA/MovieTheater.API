//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Moq;
//using MovieTheater.API.Controllers;
//using MovieTheater.Application.Interfaces;
//using MovieTheater.Application.Interfaces.Commons;
//using MovieTheater.Application.Utils;
//using MovieTheater.Domain;
//using MovieTheater.Domain.Entities;
//using MovieTheater.Domain.Enums;
//using Microsoft.EntityFrameworkCore.InMemory;

//namespace MovieTheater.UnitTest.Controllers
//{
//    public class SystemControllerTests : IDisposable
//    {
//        private readonly MovieTheaterDbContext _context;
//        private readonly Mock<ILoggerService> _mockLogger;
//        private readonly Mock<IAuditLogService> _mockAuditLogService;
//        private readonly SystemController _controller;
//        private readonly DbContextOptions<MovieTheaterDbContext> _contextOptions;

//        public SystemControllerTests()
//        {
//            // Setup in-memory database
//            _contextOptions = new DbContextOptionsBuilder<MovieTheaterDbContext>()
//                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
//                .Options;

//            _context = new MovieTheaterDbContext(_contextOptions);
//            _mockLogger = new Mock<ILoggerService>();
//            _mockAuditLogService = new Mock<IAuditLogService>();

//            _controller = new SystemController(_context, _mockLogger.Object, _mockAuditLogService.Object);
//        }


//        [Fact]
//        public async Task SeedData_Success_SeedsSeatsForAllRoomTypes()
//        {
//            // Act
//            var result = await _controller.SeedData();

//            // Assert
//            var okResult = Assert.IsType<ObjectResult>(result);
//            Assert.NotNull(okResult.Value);

//            var seats = await _context.Seats.Include(s => s.CinemaRoom).ToListAsync();
//            var cinemaRooms = await _context.CinemaRooms.ToListAsync();

//            // Verify each room has seats
//            foreach (var room in cinemaRooms)
//            {
//                var roomSeats = seats.Where(s => s.CinemaRoomId == room.Id).ToList();
//                Assert.True(roomSeats.Count > 0);

//                // Verify seat types based on room type
//                if (room.Type == RoomType.TwoD)
//                {
//                    Assert.Equal(130, roomSeats.Count); // 10 rows × 13 columns
//                    Assert.Contains(roomSeats, s => s.Type == SeatType.Normal);
//                    Assert.Contains(roomSeats, s => s.Type == SeatType.VIP);
//                    Assert.Contains(roomSeats, s => s.Type == SeatType.Couple);
//                }
//                else if (room.Type == RoomType.FourD)
//                {
//                    Assert.Equal(60, roomSeats.Count); // 6 rows × 10 columns
//                    Assert.All(roomSeats, s => Assert.Equal(SeatType.VIP, s.Type));
//                }
//            }
//        }

//        [Fact]
//        public async Task SeedData_Success_VerifiesPasswordHashing()
//        {
//            // Act
//            var result = await _controller.SeedData();

//            // Assert
//            var okResult = Assert.IsType<ObjectResult>(result);
//            Assert.NotNull(okResult.Value);

//            var users = await _context.Users.ToListAsync();

//            // Verify all users have hashed passwords (not plaintext "1@")
//            foreach (var user in users)
//            {
//                Assert.NotEqual("1@", user.Password);
//                Assert.True(user.Password.Length > 10); // Hashed passwords should be longer
//            }
//        }

//        [Fact]
//        public async Task SeedData_Success_VerifiesAllUsersAreActiveAndVerified()
//        {
//            // Act
//            var result = await _controller.SeedData();

//            // Assert
//            var okResult = Assert.IsType<ObjectResult>(result);
//            Assert.NotNull(okResult.Value);

//            var users = await _context.Users.ToListAsync();

//            // Verify all users are active and email verified
//            foreach (var user in users)
//            {
//                Assert.Equal(UserStatus.Active, user.UserStatus);
//                Assert.True(user.IsEmailVerified);
//                Assert.Equal(0, user.ScoreBalance);
//            }
//        }

//        [Fact]
//        public async Task SeedData_Success_VerifiesMovieRatingsAndDates()
//        {
//            // Act
//            var result = await _controller.SeedData();

//            // Assert
//            var okResult = Assert.IsType<ObjectResult>(result);
//            Assert.NotNull(okResult.Value);

//            var movies = await _context.Movies.ToListAsync();

//            // Verify movie data integrity
//            foreach (var movie in movies)
//            {
//                Assert.True(movie.FromDate < movie.ToDate);
//                Assert.True(movie.Rating >= 0 && movie.Rating <= 10);
//                Assert.NotNull(movie.Name);
//                Assert.NotNull(movie.Director);
//                Assert.NotNull(movie.Description);
//                Assert.NotNull(movie.Actors);
//                Assert.NotNull(movie.Genres);
//                Assert.True(movie.Actors.Count > 0);
//                Assert.True(movie.Genres.Count > 0);
//            }
//        }

//        [Fact]
//        public async Task SeedData_Success_VerifiesFoodAndDrinkPrices()
//        {
//            // Act
//            var result = await _controller.SeedData();

//            // Assert
//            var okResult = Assert.IsType<ObjectResult>(result);
//            Assert.NotNull(okResult.Value);

//            var foodAndDrinks = await _context.FoodAndDrinks.ToListAsync();

//            // Verify all items have positive prices and are available
//            foreach (var item in foodAndDrinks)
//            {
//                Assert.True(item.Price > 0);
//                Assert.True(item.IsAvailable);
//                Assert.NotNull(item.Name);
//            }
//        }

//        [Fact]
//        public async Task SeedData_Success_VerifiesEventTimeRanges()
//        {
//            // Act
//            var result = await _controller.SeedData();

//            // Assert
//            var okResult = Assert.IsType<ObjectResult>(result);
//            Assert.NotNull(okResult.Value);

//            var events = await _context.Events.Include(e => e.Promotions).ToListAsync();

//            // Verify event time ranges and promotions
//            foreach (var evt in events)
//            {
//                Assert.True(evt.StartTime < evt.EndTime);
//                Assert.NotNull(evt.Name);
//                Assert.NotNull(evt.Detail);
//                Assert.True(evt.Promotions.Count > 0);

//                // Verify each promotion has valid discount values
//                foreach (var promotion in evt.Promotions)
//                {
//                    Assert.True(promotion.DiscountValue > 0 && promotion.DiscountValue <= 1);
//                    Assert.NotNull(promotion.Title);
//                    Assert.NotNull(promotion.Detail);
//                }
//            }
//        }
//        public void Dispose()
//        {
//            _context?.Dispose();
//        }
//    }
//}