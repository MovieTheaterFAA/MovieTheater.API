using MockQueryable;
using MockQueryable.Moq;
using Moq;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.DTOs.SeatDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System.Collections.Concurrent;
using System.Reflection;

namespace MovieTheater.UnitTest.Services;

public class SeatServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILoggerService> _mockLoggerService;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<IGenericRepository<Seat>> _mockSeatRepository;
    private readonly Mock<IGenericRepository<CinemaRoom>> _mockCinemaRoomRepository;
    private readonly Mock<IGenericRepository<ShowTime>> _mockShowTimeRepository;
    private readonly Mock<IGenericRepository<ShowTimeSeat>> _mockShowTimeSeatRepository;
    private readonly Mock<IGenericRepository<BookingSeat>> _mockBookingSeatRepository;
    private readonly Mock<IGenericRepository<Booking>> _mockBookingRepository;
    private readonly SeatService _seatService;

    public SeatServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLoggerService = new Mock<ILoggerService>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockSeatRepository = new Mock<IGenericRepository<Seat>>();
        _mockCinemaRoomRepository = new Mock<IGenericRepository<CinemaRoom>>();
        _mockShowTimeRepository = new Mock<IGenericRepository<ShowTime>>();
        _mockShowTimeSeatRepository = new Mock<IGenericRepository<ShowTimeSeat>>();
        _mockBookingSeatRepository = new Mock<IGenericRepository<BookingSeat>>();
        _mockBookingRepository = new Mock<IGenericRepository<Booking>>();

        _mockUnitOfWork.Setup(u => u.Seats).Returns(_mockSeatRepository.Object);
        _mockUnitOfWork.Setup(u => u.CinemaRooms).Returns(_mockCinemaRoomRepository.Object);
        _mockUnitOfWork.Setup(u => u.ShowTimes).Returns(_mockShowTimeRepository.Object);
        _mockUnitOfWork.Setup(u => u.ShowTimeSeats).Returns(_mockShowTimeSeatRepository.Object);
        _mockUnitOfWork.Setup(u => u.BookingSeats).Returns(_mockBookingSeatRepository.Object);
        _mockUnitOfWork.Setup(u => u.Bookings).Returns(_mockBookingRepository.Object);

        _seatService = new SeatService(
            _mockLoggerService.Object,
            _mockUnitOfWork.Object,
            _mockAuditLogService.Object
        );
    }

    [Fact]
    public async Task GetSeatsByCinemaRoomAsync_ReturnsSeats()
    {
        var roomId = Guid.NewGuid();
        var seats = new List<Seat>
        {
            new() { Id = Guid.NewGuid(), Row = "A", Number = 1, Type = SeatType.Normal, CinemaRoomId = roomId, IsDeleted = false }
        };
        var queryable = seats.AsQueryable().BuildMock();
        _mockSeatRepository.Setup(r => r.GetQueryable()).Returns(queryable);

        var result = await _seatService.GetSeatsByCinemaRoomAsync(roomId);

        Assert.Single(result);
        Assert.Equal("A", result[0].Row);
    }

    [Fact]
    public async Task GetSeatsByCinemaRoomAsync_ThrowsException()
    {
        _mockSeatRepository.Setup(r => r.GetQueryable()).Throws(new Exception("DB error"));
        await Assert.ThrowsAsync<Exception>(() => _seatService.GetSeatsByCinemaRoomAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task BatchCreateSeatsAsync_CreatesSeats()
    {
        var roomId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var dto = new BatchCreateSeatDto
        {
            Seats = new List<CreateSeatDto>
            {
                new() { Row = "A", Number = 1, Type = SeatType.Normal }
            }
        };
        _mockCinemaRoomRepository.Setup(r => r.GetByIdAsync(roomId)).ReturnsAsync(new CinemaRoom { Id = roomId, IsDeleted = false });
        _mockSeatRepository.Setup(r => r.AddRangeAsync(It.IsAny<List<Seat>>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(0);
        _mockAuditLogService.Setup(a => a.LogAsync(adminId, AuditActionType.Create, "Seat", roomId, null!, It.IsAny<object>(), It.IsAny<string>(), "Batch created seats")).Returns(Task.CompletedTask);

        var result = await _seatService.BatchCreateSeatsAsync(roomId, dto, adminId);

        Assert.Single(result);
        Assert.Equal("A", result[0].Row);
    }

    [Fact]
    public async Task BatchCreateSeatsAsync_RoomNotFound_Throws()
    {
        var roomId = Guid.NewGuid();
        var dto = new BatchCreateSeatDto { Seats = new List<CreateSeatDto>() };
        _mockCinemaRoomRepository.Setup(r => r.GetByIdAsync(roomId)).ReturnsAsync((CinemaRoom)null!);

        await Assert.ThrowsAsync<Exception>(() => _seatService.BatchCreateSeatsAsync(roomId, dto, Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateSeatAsync_UpdatesSeat()
    {
        var seatId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var seat = new Seat { Id = seatId, Row = "A", Number = 1, Type = SeatType.Normal, IsDeleted = false };
        var dto = new UpdateSeatDto { Row = "B", Number = 2, Type = SeatType.VIP };
        _mockSeatRepository.Setup(r => r.GetByIdAsync(seatId)).ReturnsAsync(seat);
        _mockSeatRepository.Setup(r => r.Update(seat)).ReturnsAsync(true);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(0);
        _mockAuditLogService.Setup(a => a.LogAsync(adminId, AuditActionType.Update, "Seat", seatId, It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>(), "Updated seat")).Returns(Task.CompletedTask);

        var result = await _seatService.UpdateSeatAsync(seatId, dto, adminId);

        Assert.NotNull(result);
        Assert.Equal("B", result.Row);
    }

    [Fact]
    public async Task UpdateSeatAsync_SeatNotFound_ReturnsNull()
    {
        _mockSeatRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Seat)null!);
        var result = await _seatService.UpdateSeatAsync(Guid.NewGuid(), new UpdateSeatDto(), Guid.NewGuid());
        Assert.Null(result);
    }
    [Fact]
    public async Task UpdateSeatAsync_ThrowsExceptionWhenDatabaseErrorOccurs()
    {
        // Arrange
        var seatId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var expectedErrorMessage = "Database connection failed";
        var seat = new Seat
        {
            Id = seatId,
            Row = "A",
            Number = 1,
            Type = SeatType.Normal,
            IsDeleted = false
        };
        var dto = new UpdateSeatDto
        {
            Row = "B",
            Number = 2,
            Type = SeatType.VIP
        };

        // Mock seat retrieval to succeed
        _mockSeatRepository.Setup(r => r.GetByIdAsync(seatId))
            .ReturnsAsync(seat);

        // Mock the Update method to throw an exception
        _mockSeatRepository.Setup(r => r.Update(It.IsAny<Seat>()))
            .Throws(new InvalidOperationException(expectedErrorMessage));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _seatService.UpdateSeatAsync(seatId, dto, adminId));

        // Verify the exception message
        Assert.Equal("An error occurred while updating the seat.", ex.Message);

        // Verify that error logging was called with the correct message format
        _mockLoggerService.Verify(
            l => l.Error(It.Is<string>(msg =>
                msg.Contains("[SeatManagementService] Error updating seat:") &&
                msg.Contains(expectedErrorMessage))),
            Times.Once);

        // Verify that the seat properties were updated before the exception occurred
        Assert.Equal("B", seat.Row);
        Assert.Equal(2, seat.Number);
        Assert.Equal(SeatType.VIP, seat.Type);
        Assert.Equal(adminId, seat.UpdatedBy);

        // Verify that SaveChangesAsync was not called due to the exception
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);

        // Verify that audit logging was not called due to the exception
        _mockAuditLogService.Verify(
            a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<AuditActionType>(), It.IsAny<string>(),
                It.IsAny<Guid>(), It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
    [Fact]
    public async Task SoftDeleteSeatAsync_DeletesSeat()
    {
        var seatId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var seat = new Seat { Id = seatId, IsDeleted = false };
        _mockSeatRepository.Setup(r => r.GetByIdAsync(seatId)).ReturnsAsync(seat);
        _mockSeatRepository.Setup(r => r.Update(seat)).ReturnsAsync(true);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(0);
        _mockAuditLogService.Setup(a => a.LogAsync(adminId, AuditActionType.Delete, "Seat", seatId, It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>(), "Soft deleted seat")).Returns(Task.CompletedTask);

        var result = await _seatService.SoftDeleteSeatAsync(seatId, adminId);

        Assert.True(result);
    }

    [Fact]
    public async Task SoftDeleteSeatAsync_SeatNotFound_ReturnsFalse()
    {
        _mockSeatRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Seat)null!);
        var result = await _seatService.SoftDeleteSeatAsync(Guid.NewGuid(), Guid.NewGuid());
        Assert.False(result);
    }
    [Fact]
    public async Task SoftDeleteSeatAsync_ThrowsExceptionWhenDatabaseErrorOccurs()
    {
        // Arrange
        var seatId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var expectedErrorMessage = "Database connection failed";
        var seat = new Seat
        {
            Id = seatId,
            Row = "A",
            Number = 1,
            Type = SeatType.Normal,
            IsDeleted = false
        };

        // Mock seat retrieval to succeed
        _mockSeatRepository.Setup(r => r.GetByIdAsync(seatId))
            .ReturnsAsync(seat);

        // Mock the Update method to throw an exception
        _mockSeatRepository.Setup(r => r.Update(It.IsAny<Seat>()))
            .Throws(new InvalidOperationException(expectedErrorMessage));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _seatService.SoftDeleteSeatAsync(seatId, adminId));

        // Verify the exception message
        Assert.Equal("An error occurred while deleting the seat.", ex.Message);

        // Verify that error logging was called with the correct message format
        _mockLoggerService.Verify(
            l => l.Error(It.Is<string>(msg =>
                msg.Contains("[SeatManagementService] Error soft deleting seat:") &&
                msg.Contains(expectedErrorMessage))),
            Times.Once);

        // Verify that the seat IsDeleted flag was set before the exception occurred
        Assert.True(seat.IsDeleted);

        // Verify that SaveChangesAsync was not called due to the exception
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);

        // Verify that audit logging was not called due to the exception
        _mockAuditLogService.Verify(
            a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<AuditActionType>(), It.IsAny<string>(),
                It.IsAny<Guid>(), It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
    [Fact]
    public async Task GetShowTimeSeatStatusAsync_ThrowsIfShowTimeNotFound()
    {
        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ShowTime)null!);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _seatService.GetShowTimeSeatStatusAsync(Guid.NewGuid()));
    }
    [Fact]
    public async Task GetShowTimeSeatStatusAsync_ReturnsHoldingStatusWhenSeatIsInHoldingSeats()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = cinemaRoomId };
        var seat = new Seat
        {
            Id = seatId,
            CinemaRoomId = cinemaRoomId,
            Row = "A",
            Number = 1,
            Type = SeatType.Normal
        };

        // No ShowTimeSeat record exists, so the seat should fall back to checking _holdingSeats
        var emptyShowTimeSeats = new List<ShowTimeSeat>();

        // Use reflection to access the static _holdingSeats field
        var holdingSeatsField = typeof(SeatService).GetField("_holdingSeats", BindingFlags.NonPublic | BindingFlags.Static);
        var holdingSeats = (ConcurrentDictionary<(Guid, Guid), (Guid userId, DateTime expireAt)>)holdingSeatsField!.GetValue(null)!;

        // Clear any existing holds and add our test hold
        holdingSeats.Clear();
        holdingSeats.TryAdd((seatId, showTimeId), (userId, DateTime.UtcNow.AddMinutes(10))); // Non-expired hold

        // Mock the repositories
        var mockShowTimeSeatDbSet = emptyShowTimeSeats.AsQueryable().BuildMockDbSet();
        var mockSeatDbSet = new List<Seat> { seat }.AsQueryable().BuildMockDbSet();

        _mockShowTimeRepository
            .Setup(r => r.GetByIdAsync(showTimeId))
            .ReturnsAsync(showTime);

        _mockShowTimeSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockShowTimeSeatDbSet.Object);

        _mockSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockSeatDbSet.Object);

        // Act
        var result = await _seatService.GetShowTimeSeatStatusAsync(showTimeId);

        // Assert
        Assert.Single(result);
        var seatResult = result.First();
        Assert.Equal(seatId, seatResult.SeatId);
        Assert.Equal("A", seatResult.Row);
        Assert.Equal(1, seatResult.Number);
        Assert.Equal(SeatType.Normal, seatResult.Type);
        Assert.Equal(SeatStatus.Holding, seatResult.Status); // This tests the specific condition

        // Verify success logging was called with the specific broadcast message
        _mockLoggerService.Verify(
            l => l.Success(It.Is<string>(msg =>
                msg.Contains($"Broadcasted seat status for showtime {showTimeId}") &&
                msg.Contains("with 1 seats."))),
            Times.Once);

        // Clean up
        holdingSeats.Clear();
    }

    [Fact]
    public async Task GetShowTimeSeatStatusAsync_ReturnsAvailableStatusWhenSeatHoldIsExpired()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = cinemaRoomId };
        var seat = new Seat
        {
            Id = seatId,
            CinemaRoomId = cinemaRoomId,
            Row = "B",
            Number = 2,
            Type = SeatType.VIP
        };

        // No ShowTimeSeat record exists, so the seat should fall back to checking _holdingSeats
        var emptyShowTimeSeats = new List<ShowTimeSeat>();

        // Use reflection to access the static _holdingSeats field
        var holdingSeatsField = typeof(SeatService).GetField("_holdingSeats", BindingFlags.NonPublic | BindingFlags.Static);
        var holdingSeats = (ConcurrentDictionary<(Guid, Guid), (Guid userId, DateTime expireAt)>)holdingSeatsField!.GetValue(null)!;

        // Clear any existing holds and add an expired hold
        holdingSeats.Clear();
        holdingSeats.TryAdd((seatId, showTimeId), (userId, DateTime.UtcNow.AddMinutes(-5))); // Expired hold

        // Mock the repositories
        var mockShowTimeSeatDbSet = emptyShowTimeSeats.AsQueryable().BuildMockDbSet();
        var mockSeatDbSet = new List<Seat> { seat }.AsQueryable().BuildMockDbSet();

        _mockShowTimeRepository
            .Setup(r => r.GetByIdAsync(showTimeId))
            .ReturnsAsync(showTime);

        _mockShowTimeSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockShowTimeSeatDbSet.Object);

        _mockSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockSeatDbSet.Object);

        // Act
        var result = await _seatService.GetShowTimeSeatStatusAsync(showTimeId);

        // Assert
        Assert.Single(result);
        var seatResult = result.First();
        Assert.Equal(seatId, seatResult.SeatId);
        Assert.Equal("B", seatResult.Row);
        Assert.Equal(2, seatResult.Number);
        Assert.Equal(SeatType.VIP, seatResult.Type);
        Assert.Equal(SeatStatus.Available, seatResult.Status); // Expired hold should result in Available status

        // Clean up
        holdingSeats.Clear();
    }

    [Fact]
    public async Task GetShowTimeSeatStatusAsync_PrioritizesShowTimeSeatStatusOverHoldingSeats()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = cinemaRoomId };
        var seat = new Seat
        {
            Id = seatId,
            CinemaRoomId = cinemaRoomId,
            Row = "C",
            Number = 3,
            Type = SeatType.Normal
        };

        // ShowTimeSeat record exists with Sold status
        var showTimeSeats = new List<ShowTimeSeat>
        {
            new() { SeatId = seatId, ShowTimeId = showTimeId, Status = SeatStatus.Sold }
        };

        // Use reflection to access the static _holdingSeats field
        var holdingSeatsField = typeof(SeatService).GetField("_holdingSeats", BindingFlags.NonPublic | BindingFlags.Static);
        var holdingSeats = (ConcurrentDictionary<(Guid, Guid), (Guid userId, DateTime expireAt)>)holdingSeatsField!.GetValue(null)!;

        // Clear any existing holds and add a valid hold (should be ignored since ShowTimeSeat exists)
        holdingSeats.Clear();
        holdingSeats.TryAdd((seatId, showTimeId), (userId, DateTime.UtcNow.AddMinutes(10)));

        // Mock the repositories
        var mockShowTimeSeatDbSet = showTimeSeats.AsQueryable().BuildMockDbSet();
        var mockSeatDbSet = new List<Seat> { seat }.AsQueryable().BuildMockDbSet();

        _mockShowTimeRepository
            .Setup(r => r.GetByIdAsync(showTimeId))
            .ReturnsAsync(showTime);

        _mockShowTimeSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockShowTimeSeatDbSet.Object);

        _mockSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockSeatDbSet.Object);

        // Act
        var result = await _seatService.GetShowTimeSeatStatusAsync(showTimeId);

        // Assert
        Assert.Single(result);
        var seatResult = result.First();
        Assert.Equal(seatId, seatResult.SeatId);
        Assert.Equal("C", seatResult.Row);
        Assert.Equal(3, seatResult.Number);
        Assert.Equal(SeatType.Normal, seatResult.Type);
        Assert.Equal(SeatStatus.Sold, seatResult.Status); // Should use ShowTimeSeat status, not holding status

        // Clean up
        holdingSeats.Clear();
    }

    [Fact]
    public async Task GetShowTimeSeatStatusAsync_MixedSeatStatuses()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var seatIds = Enumerable.Range(1, 4).Select(_ => Guid.NewGuid()).ToList();
        var userId = Guid.NewGuid();

        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = cinemaRoomId };

        // Mix of ShowTimeSeat records and seats without records
        var showTimeSeats = new List<ShowTimeSeat>
        {
            new() { SeatId = seatIds[0], ShowTimeId = showTimeId, Status = SeatStatus.Booked },
            new() { SeatId = seatIds[1], ShowTimeId = showTimeId, Status = SeatStatus.Sold }
            // seatIds[2] and seatIds[3] will fall back to _holdingSeats logic
        };

        var seats = new List<Seat>
        {
            new() { Id = seatIds[0], CinemaRoomId = cinemaRoomId, Row = "A", Number = 1, Type = SeatType.Normal },
            new() { Id = seatIds[1], CinemaRoomId = cinemaRoomId, Row = "A", Number = 2, Type = SeatType.VIP },
            new() { Id = seatIds[2], CinemaRoomId = cinemaRoomId, Row = "A", Number = 3, Type = SeatType.Normal },
            new() { Id = seatIds[3], CinemaRoomId = cinemaRoomId, Row = "A", Number = 4, Type = SeatType.Normal }
        };

        // Use reflection to access the static _holdingSeats field
        var holdingSeatsField = typeof(SeatService).GetField("_holdingSeats", BindingFlags.NonPublic | BindingFlags.Static);
        var holdingSeats = (ConcurrentDictionary<(Guid, Guid), (Guid userId, DateTime expireAt)>)holdingSeatsField!.GetValue(null)!;

        holdingSeats.Clear();
        // Add one valid hold and one expired hold
        holdingSeats.TryAdd((seatIds[2], showTimeId), (userId, DateTime.UtcNow.AddMinutes(10))); // Valid hold
        holdingSeats.TryAdd((seatIds[3], showTimeId), (userId, DateTime.UtcNow.AddMinutes(-5))); // Expired hold

        var mockShowTimeSeatDbSet = showTimeSeats.AsQueryable().BuildMockDbSet();
        var mockSeatDbSet = seats.AsQueryable().BuildMockDbSet();

        _mockShowTimeRepository
            .Setup(r => r.GetByIdAsync(showTimeId))
            .ReturnsAsync(showTime);

        _mockShowTimeSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockShowTimeSeatDbSet.Object);

        _mockSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockSeatDbSet.Object);

        // Act
        var result = await _seatService.GetShowTimeSeatStatusAsync(showTimeId);

        // Assert
        Assert.Equal(4, result.Count);

        // Verify each seat status
        var bookedSeat = result.First(s => s.SeatId == seatIds[0]);
        Assert.Equal(SeatStatus.Booked, bookedSeat.Status);

        var soldSeat = result.First(s => s.SeatId == seatIds[1]);
        Assert.Equal(SeatStatus.Sold, soldSeat.Status);

        var holdingSeat = result.First(s => s.SeatId == seatIds[2]);
        Assert.Equal(SeatStatus.Holding, holdingSeat.Status); // Valid hold

        var availableSeat = result.First(s => s.SeatId == seatIds[3]);
        Assert.Equal(SeatStatus.Available, availableSeat.Status); // Expired hold

        // Verify success logging was called
        _mockLoggerService.Verify(
            l => l.Success(It.Is<string>(msg =>
                msg.Contains($"Broadcasted seat status for showtime {showTimeId}") &&
                msg.Contains("with 4 seats."))),
            Times.Once);

        // Clean up
        holdingSeats.Clear();
    }

    [Fact]
    public async Task GetShowTimeSeatStatusAsync_ShowTimeNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId)).ReturnsAsync((ShowTime)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _seatService.GetShowTimeSeatStatusAsync(showTimeId));

        // Verify the inner exception is KeyNotFoundException
        Assert.IsType<KeyNotFoundException>(ex.InnerException);
        Assert.Equal("Showtime not found.", ex.InnerException.Message);

        // Verify warning logging was called
        _mockLoggerService.Verify(
            l => l.Warn($"Showtime not found for showTimeId: {showTimeId}"),
            Times.Once);
    }

    [Fact]
    public async Task GetShowTimeSeatStatusAsync_DatabaseError_ThrowsInvalidOperationException()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = Guid.NewGuid() };
        var expectedErrorMessage = "Database connection failed";

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId)).ReturnsAsync(showTime);
        _mockSeatRepository.Setup(r => r.GetQueryable()).Throws(new Exception(expectedErrorMessage));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _seatService.GetShowTimeSeatStatusAsync(showTimeId));

        // Verify the exception message and inner exception
        Assert.Equal("An error occurred while broadcasting seat status.", ex.Message);
        Assert.Equal(expectedErrorMessage, ex.InnerException?.Message);

        // Verify error logging was called
        _mockLoggerService.Verify(
            l => l.Error(It.Is<string>(msg =>
                msg.Contains($"Error broadcasting seat status for showtime {showTimeId}:") &&
                msg.Contains(expectedErrorMessage))),
            Times.Once);
    }

    [Fact]
    public async Task GetShowTimeSeatStatusAsync_EmptyCinemaRoom_ReturnsEmptyList()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = cinemaRoomId };

        // Empty seats and showTimeSeats
        var emptySeats = new List<Seat>();
        var emptyShowTimeSeats = new List<ShowTimeSeat>();

        var mockSeatDbSet = emptySeats.AsQueryable().BuildMockDbSet();
        var mockShowTimeSeatDbSet = emptyShowTimeSeats.AsQueryable().BuildMockDbSet();

        _mockShowTimeRepository
            .Setup(r => r.GetByIdAsync(showTimeId))
            .ReturnsAsync(showTime);

        _mockSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockSeatDbSet.Object);

        _mockShowTimeSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockShowTimeSeatDbSet.Object);

        // Act
        var result = await _seatService.GetShowTimeSeatStatusAsync(showTimeId);

        // Assert
        Assert.Empty(result);

        // Verify success logging was called with 0 seats
        _mockLoggerService.Verify(
            l => l.Success($"Broadcasted seat status for showtime {showTimeId} with 0 seats."),
            Times.Once);
    }
    [Fact]
    public async Task GetSeatsByShowTimeAsync_ThrowsIfShowTimeNotFound()
    {
        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ShowTime)null!);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _seatService.GetSeatsByShowTimeAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetSeatsByShowTimeAsync_ReturnsHoldingStatusWhenSeatIsInHoldingSeats()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = cinemaRoomId };
        var seat = new Seat
        {
            Id = seatId,
            CinemaRoomId = cinemaRoomId,
            Row = "A",
            Number = 1,
            Type = SeatType.Normal
        };

        // No ShowTimeSeat record exists, so the seat should fall back to checking _holdingSeats
        var emptyShowTimeSeats = new List<ShowTimeSeat>();

        // Use reflection to access the static _holdingSeats field
        var holdingSeatsField = typeof(SeatService).GetField("_holdingSeats", BindingFlags.NonPublic | BindingFlags.Static);
        var holdingSeats = (ConcurrentDictionary<(Guid, Guid), (Guid userId, DateTime expireAt)>)holdingSeatsField!.GetValue(null)!;

        // Clear any existing holds and add our test hold
        holdingSeats.Clear();
        holdingSeats.TryAdd((seatId, showTimeId), (userId, DateTime.UtcNow.AddMinutes(10))); // Non-expired hold

        // Mock the repositories
        var mockShowTimeSeatDbSet = emptyShowTimeSeats.AsQueryable().BuildMockDbSet();
        var mockSeatDbSet = new List<Seat> { seat }.AsQueryable().BuildMockDbSet();

        _mockShowTimeRepository
            .Setup(r => r.GetByIdAsync(showTimeId))
            .ReturnsAsync(showTime);

        _mockShowTimeSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockShowTimeSeatDbSet.Object);

        _mockSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockSeatDbSet.Object);

        // Act
        var result = await _seatService.GetSeatsByShowTimeAsync(showTimeId);

        // Assert
        Assert.Single(result);
        var seatResult = result.First();
        Assert.Equal(seatId, seatResult.SeatId);
        Assert.Equal("A", seatResult.Row);
        Assert.Equal(1, seatResult.Number);
        Assert.Equal(SeatType.Normal, seatResult.Type);
        Assert.Equal(SeatStatus.Holding, seatResult.Status); // This tests the specific condition

        // Verify success logging was called
        _mockLoggerService.Verify(
            l => l.Success(It.Is<string>(msg =>
                msg.Contains($"Successfully retrieved seats for showtime {showTimeId}") &&
                msg.Contains("with 1 seats."))),
            Times.Once);

        // Clean up
        holdingSeats.Clear();
    }

    [Fact]
    public async Task GetSeatsByShowTimeAsync_ReturnsAvailableStatusWhenSeatHoldIsExpired()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = cinemaRoomId };
        var seat = new Seat
        {
            Id = seatId,
            CinemaRoomId = cinemaRoomId,
            Row = "B",
            Number = 2,
            Type = SeatType.VIP
        };

        // No ShowTimeSeat record exists, so the seat should fall back to checking _holdingSeats
        var emptyShowTimeSeats = new List<ShowTimeSeat>();

        // Use reflection to access the static _holdingSeats field
        var holdingSeatsField = typeof(SeatService).GetField("_holdingSeats", BindingFlags.NonPublic | BindingFlags.Static);
        var holdingSeats = (ConcurrentDictionary<(Guid, Guid), (Guid userId, DateTime expireAt)>)holdingSeatsField!.GetValue(null)!;

        // Clear any existing holds and add an expired hold
        holdingSeats.Clear();
        holdingSeats.TryAdd((seatId, showTimeId), (userId, DateTime.UtcNow.AddMinutes(-5))); // Expired hold

        // Mock the repositories
        var mockShowTimeSeatDbSet = emptyShowTimeSeats.AsQueryable().BuildMockDbSet();
        var mockSeatDbSet = new List<Seat> { seat }.AsQueryable().BuildMockDbSet();

        _mockShowTimeRepository
            .Setup(r => r.GetByIdAsync(showTimeId))
            .ReturnsAsync(showTime);

        _mockShowTimeSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockShowTimeSeatDbSet.Object);

        _mockSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockSeatDbSet.Object);

        // Act
        var result = await _seatService.GetSeatsByShowTimeAsync(showTimeId);

        // Assert
        Assert.Single(result);
        var seatResult = result.First();
        Assert.Equal(seatId, seatResult.SeatId);
        Assert.Equal("B", seatResult.Row);
        Assert.Equal(2, seatResult.Number);
        Assert.Equal(SeatType.VIP, seatResult.Type);
        Assert.Equal(SeatStatus.Available, seatResult.Status); // Expired hold should result in Available status

        // Clean up
        holdingSeats.Clear();
    }

    [Fact]
    public async Task GetSeatsByShowTimeAsync_PrioritizesShowTimeSeatStatusOverHoldingSeats()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = cinemaRoomId };
        var seat = new Seat
        {
            Id = seatId,
            CinemaRoomId = cinemaRoomId,
            Row = "C",
            Number = 3,
            Type = SeatType.Normal
        };

        // ShowTimeSeat record exists with Booked status
        var showTimeSeats = new List<ShowTimeSeat>
        {
            new() { SeatId = seatId, ShowTimeId = showTimeId, Status = SeatStatus.Booked }
        };

        // Use reflection to access the static _holdingSeats field
        var holdingSeatsField = typeof(SeatService).GetField("_holdingSeats", BindingFlags.NonPublic | BindingFlags.Static);
        var holdingSeats = (ConcurrentDictionary<(Guid, Guid), (Guid userId, DateTime expireAt)>)holdingSeatsField!.GetValue(null)!;

        // Clear any existing holds and add a valid hold (should be ignored since ShowTimeSeat exists)
        holdingSeats.Clear();
        holdingSeats.TryAdd((seatId, showTimeId), (userId, DateTime.UtcNow.AddMinutes(10)));

        // Mock the repositories
        var mockShowTimeSeatDbSet = showTimeSeats.AsQueryable().BuildMockDbSet();
        var mockSeatDbSet = new List<Seat> { seat }.AsQueryable().BuildMockDbSet();

        _mockShowTimeRepository
            .Setup(r => r.GetByIdAsync(showTimeId))
            .ReturnsAsync(showTime);

        _mockShowTimeSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockShowTimeSeatDbSet.Object);

        _mockSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockSeatDbSet.Object);

        // Act
        var result = await _seatService.GetSeatsByShowTimeAsync(showTimeId);

        // Assert
        Assert.Single(result);
        var seatResult = result.First();
        Assert.Equal(seatId, seatResult.SeatId);
        Assert.Equal("C", seatResult.Row);
        Assert.Equal(3, seatResult.Number);
        Assert.Equal(SeatType.Normal, seatResult.Type);
        Assert.Equal(SeatStatus.Booked, seatResult.Status); // Should use ShowTimeSeat status, not holding status

        // Clean up
        holdingSeats.Clear();
    }

    [Fact]
    public async Task HoldSeatsAsync_ThrowsIfShowTimeNotFound()
    {
        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ShowTime)null!);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _seatService.HoldSeatsAsync(Guid.NewGuid(), Guid.NewGuid(), new List<Guid> { Guid.NewGuid() }));
    }

    [Fact]
    public async Task HoldSeatsAsync_ThrowsIfNoSeatsInRoom()
    {
        var showTimeId = Guid.NewGuid();
        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = Guid.NewGuid() };
        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId)).ReturnsAsync(showTime);
        _mockSeatRepository.Setup(r => r.GetQueryable()).Returns(new List<Seat>().AsQueryable().BuildMock());

        await Assert.ThrowsAsync<InvalidOperationException>(() => _seatService.HoldSeatsAsync(Guid.NewGuid(), showTimeId, new List<Guid> { Guid.NewGuid() }));
    }

    [Fact]
    public async Task HoldSeatsAsync_EmptySeatList_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var emptySeatIds = new List<Guid>();

        // Act
        var result = await _seatService.HoldSeatsAsync(userId, showTimeId, emptySeatIds);

        // Assert
        Assert.Empty(result);

        // Verify warning logging was called
        _mockLoggerService.Verify(
            l => l.Warn($"User {userId} provided empty seat list for holding"),
            Times.Once);
    }

    [Fact]
    public async Task HoldSeatsAsync_NullSeatList_ThrowsExceptionDueToLogging()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();

        // Act & Assert
        // Currently the method fails due to string.Join with null parameter in logging
        // This happens before the intended null check
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _seatService.HoldSeatsAsync(userId, showTimeId, null!));

        Assert.Equal("An error occurred while holding seats. Please try again later.", ex.Message);
        Assert.IsType<ArgumentNullException>(ex.InnerException);
        Assert.Contains("Value cannot be null", ex.InnerException.Message);

        // Verify error logging was called
        _mockLoggerService.Verify(
            l => l.Error(It.Is<string>(msg =>
                msg.Contains($"Error holding seats for user {userId} for showtime {showTimeId}:"))),
            Times.Once);
    }

    [Fact]
    public async Task HoldSeatsAsync_DuplicateSeatIds_UsesDeduplicated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var duplicateSeatIds = new List<Guid> { seatId, seatId, seatId }; // Duplicate IDs

        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = cinemaRoomId };
        var seat = new Seat
        {
            Id = seatId,
            CinemaRoomId = cinemaRoomId,
            Row = "A",
            Number = 1,
            Type = SeatType.Normal,
            IsDeleted = false
        };

        // Setup mocks
        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId)).ReturnsAsync(showTime);

        var mockSeatDbSet = new List<Seat> { seat }.AsQueryable().BuildMockDbSet();
        _mockSeatRepository.Setup(r => r.GetQueryable()).Returns(mockSeatDbSet.Object);

        var mockShowTimeSeatDbSet = new List<ShowTimeSeat>().AsQueryable().BuildMockDbSet();
        _mockShowTimeSeatRepository.Setup(r => r.GetQueryable()).Returns(mockShowTimeSeatDbSet.Object);

        var mockBookingSeatDbSet = new List<BookingSeat>().AsQueryable().BuildMockDbSet();
        _mockBookingSeatRepository.Setup(r => r.GetQueryable()).Returns(mockBookingSeatDbSet.Object);

        // Clear holding seats
        var holdingSeatsField = typeof(SeatService).GetField("_holdingSeats", BindingFlags.NonPublic | BindingFlags.Static);
        var holdingSeats = (ConcurrentDictionary<(Guid, Guid), (Guid userId, DateTime expireAt)>)holdingSeatsField!.GetValue(null)!;
        holdingSeats.Clear();

        // Act
        var result = await _seatService.HoldSeatsAsync(userId, showTimeId, duplicateSeatIds);

        // Assert
        Assert.Single(result); // Should only hold one seat despite duplicates
        Assert.Equal(seatId, result[0].Id);

        // Verify warning logging for duplicates
        _mockLoggerService.Verify(
            l => l.Warn($"User {userId} provided duplicate seat IDs, using unique set instead"),
            Times.Once);

        // Clean up
        holdingSeats.Clear();
    }

    [Fact]
    public async Task HoldSeatsAsync_ExceedsSeatLimit_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();

        // Use only 3 new seat IDs to avoid logging issues
        var newSeatIds = Enumerable.Range(1, 3).Select(_ => Guid.NewGuid()).ToList();
        // Create additional seats for existing bookings/holds to reach the limit
        var existingBookedSeatIds = Enumerable.Range(1, 4).Select(_ => Guid.NewGuid()).ToList();
        var existingHeldSeatIds = Enumerable.Range(1, 2).Select(_ => Guid.NewGuid()).ToList();

        // Total: 4 existing bookings + 2 existing holds + 3 new = 9 seats (exceeds limit of 8)
        var allSeatIds = newSeatIds.Concat(existingBookedSeatIds).Concat(existingHeldSeatIds).ToList();
        var seats = allSeatIds.Select((id, index) => new Seat
        {
            Id = id,
            CinemaRoomId = cinemaRoomId,
            Row = "A",
            Number = index + 1,
            Type = SeatType.Normal,
            IsDeleted = false
        }).ToList();

        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = cinemaRoomId };

        // Create existing bookings (4 seats)
        var existingBookings = existingBookedSeatIds.Select(seatId => new BookingSeat
        {
            SeatId = seatId,
            Booking = new Booking
            {
                Id = Guid.NewGuid(),
                MemberId = userId,
                ShowtimeId = showTimeId,
                Status = "Completed"
            }
        }).ToList();

        // Setup mocks
        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId)).ReturnsAsync(showTime);

        var mockSeatDbSet = seats.AsQueryable().BuildMockDbSet();
        _mockSeatRepository.Setup(r => r.GetQueryable()).Returns(mockSeatDbSet.Object);

        var mockShowTimeSeatDbSet = new List<ShowTimeSeat>().AsQueryable().BuildMockDbSet();
        _mockShowTimeSeatRepository.Setup(r => r.GetQueryable()).Returns(mockShowTimeSeatDbSet.Object);

        var mockBookingSeatDbSet = existingBookings.AsQueryable().BuildMockDbSet();
        _mockBookingSeatRepository.Setup(r => r.GetQueryable()).Returns(mockBookingSeatDbSet.Object);

        // Set up existing holds (2 seats)
        var holdingSeatsField = typeof(SeatService).GetField("_holdingSeats", BindingFlags.NonPublic | BindingFlags.Static);
        var holdingSeats = (ConcurrentDictionary<(Guid, Guid), (Guid userId, DateTime expireAt)>)holdingSeatsField!.GetValue(null)!;
        holdingSeats.Clear();

        foreach (var seatId in existingHeldSeatIds)
        {
            holdingSeats.TryAdd((seatId, showTimeId), (userId, DateTime.UtcNow.AddMinutes(5)));
        }

        // Act & Assert
        // User currently has 4 booked + 2 held = 6 seats
        // Trying to hold 3 more would total 9 seats, exceeding the 8-seat limit
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _seatService.HoldSeatsAsync(userId, showTimeId, newSeatIds));

        // The outer catch block wraps the specific exception, so we need to check the inner exception
        Assert.Equal("An error occurred while holding seats. Please try again later.", ex.Message);
        Assert.NotNull(ex.InnerException);
        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Contains("You cannot hold more than 8 seats in total", ex.InnerException.Message);
        Assert.Contains("You currently have 6 seats", ex.InnerException.Message);
        Assert.Contains("trying to hold 3 more", ex.InnerException.Message);

        // Verify warning logging for seat limit
        _mockLoggerService.Verify(
            l => l.Warn(It.Is<string>(msg =>
                msg.Contains($"User {userId} attempted to exceed 8-seat limit (total: 9)"))),
            Times.Once);

        // Clean up
        holdingSeats.Clear();
    }

    [Fact]
    public async Task HoldSeatsAsync_ShowTimeNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var seatIds = new List<Guid> { Guid.NewGuid() };

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId)).ReturnsAsync((ShowTime)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _seatService.HoldSeatsAsync(userId, showTimeId, seatIds));

        Assert.IsType<KeyNotFoundException>(ex.InnerException);
        Assert.Contains($"Showtime with ID {showTimeId} not found", ex.InnerException.Message);

        // Verify warning logging
        _mockLoggerService.Verify(
            l => l.Warn($"Showtime {showTimeId} not found"),
            Times.Once);
    }

    [Fact]
    public async Task HoldSeatsAsync_NoSeatsInCinemaRoom_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var seatIds = new List<Guid> { Guid.NewGuid() };

        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = cinemaRoomId };

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId)).ReturnsAsync(showTime);

        var mockSeatDbSet = new List<Seat>().AsQueryable().BuildMockDbSet(); // Empty seats
        _mockSeatRepository.Setup(r => r.GetQueryable()).Returns(mockSeatDbSet.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _seatService.HoldSeatsAsync(userId, showTimeId, seatIds));

        Assert.IsType<KeyNotFoundException>(ex.InnerException);
        Assert.Contains($"No seats found for cinema room {cinemaRoomId}", ex.InnerException.Message);

        // Verify warning logging
        _mockLoggerService.Verify(
            l => l.Warn($"No seats found for cinema room {cinemaRoomId}"),
            Times.Once);
    }

    [Fact]
    public async Task HoldSeatsAsync_SeatNotInCinemaRoom_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var invalidSeatId = Guid.NewGuid();
        var validSeatId = Guid.NewGuid();

        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = cinemaRoomId };
        var validSeat = new Seat
        {
            Id = validSeatId,
            CinemaRoomId = cinemaRoomId,
            IsDeleted = false
        };

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId)).ReturnsAsync(showTime);

        var mockSeatDbSet = new List<Seat> { validSeat }.AsQueryable().BuildMockDbSet();
        _mockSeatRepository.Setup(r => r.GetQueryable()).Returns(mockSeatDbSet.Object);

        var mockBookingSeatDbSet = new List<BookingSeat>().AsQueryable().BuildMockDbSet();
        _mockBookingSeatRepository.Setup(r => r.GetQueryable()).Returns(mockBookingSeatDbSet.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _seatService.HoldSeatsAsync(userId, showTimeId, new List<Guid> { invalidSeatId }));

        Assert.IsType<ArgumentException>(ex.InnerException);
        Assert.Contains($"Seat {invalidSeatId} does not exist in the specified cinema room", ex.InnerException.Message);

        // Verify warning logging
        _mockLoggerService.Verify(
            l => l.Warn($"Seat {invalidSeatId} does not exist in cinema room {cinemaRoomId}"),
            Times.Once);
    }

    [Fact]
    public async Task HoldSeatsAsync_SeatAlreadyHeldByAnotherUser_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var seatId = Guid.NewGuid();

        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = cinemaRoomId };
        var seat = new Seat
        {
            Id = seatId,
            CinemaRoomId = cinemaRoomId,
            Row = "A",
            Number = 1,
            Type = SeatType.Normal,
            IsDeleted = false
        };

        // Setup mocks
        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId)).ReturnsAsync(showTime);

        var mockSeatDbSet = new List<Seat> { seat }.AsQueryable().BuildMockDbSet();
        _mockSeatRepository.Setup(r => r.GetQueryable()).Returns(mockSeatDbSet.Object);

        var mockShowTimeSeatDbSet = new List<ShowTimeSeat>().AsQueryable().BuildMockDbSet();
        _mockShowTimeSeatRepository.Setup(r => r.GetQueryable()).Returns(mockShowTimeSeatDbSet.Object);

        var mockBookingSeatDbSet = new List<BookingSeat>().AsQueryable().BuildMockDbSet();
        _mockBookingSeatRepository.Setup(r => r.GetQueryable()).Returns(mockBookingSeatDbSet.Object);

        // Set up existing hold by another user
        var holdingSeatsField = typeof(SeatService).GetField("_holdingSeats", BindingFlags.NonPublic | BindingFlags.Static);
        var holdingSeats = (ConcurrentDictionary<(Guid, Guid), (Guid userId, DateTime expireAt)>)holdingSeatsField!.GetValue(null)!;
        holdingSeats.Clear();
        holdingSeats.TryAdd((seatId, showTimeId), (anotherUserId, DateTime.UtcNow.AddMinutes(5)));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _seatService.HoldSeatsAsync(userId, showTimeId, new List<Guid> { seatId }));

        // Check the outer exception message
        Assert.Equal("An error occurred while holding seats. Please try again later.", ex.Message);

        // Check the inner exception for the specific seat message
        Assert.NotNull(ex.InnerException);
        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Contains($"Seat {seatId} is already held by another user", ex.InnerException.Message);

        // Verify warning logging
        _mockLoggerService.Verify(
            l => l.Warn(It.Is<string>(msg =>
                msg.Contains($"Seat {seatId} is already held by another user"))),
            Times.Once);

        // Clean up
        holdingSeats.Clear();
    }

    [Fact]
    public async Task HoldSeatsAsync_SeatAlreadyBooked_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var seatId = Guid.NewGuid();

        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = cinemaRoomId };
        var seat = new Seat
        {
            Id = seatId,
            CinemaRoomId = cinemaRoomId,
            IsDeleted = false
        };
        var showTimeSeat = new ShowTimeSeat
        {
            SeatId = seatId,
            ShowTimeId = showTimeId,
            Status = SeatStatus.Booked
        };

        // Setup mocks
        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId)).ReturnsAsync(showTime);

        var mockSeatDbSet = new List<Seat> { seat }.AsQueryable().BuildMockDbSet();
        _mockSeatRepository.Setup(r => r.GetQueryable()).Returns(mockSeatDbSet.Object);

        var mockShowTimeSeatDbSet = new List<ShowTimeSeat> { showTimeSeat }.AsQueryable().BuildMockDbSet();
        _mockShowTimeSeatRepository.Setup(r => r.GetQueryable()).Returns(mockShowTimeSeatDbSet.Object);

        var mockBookingSeatDbSet = new List<BookingSeat>().AsQueryable().BuildMockDbSet();
        _mockBookingSeatRepository.Setup(r => r.GetQueryable()).Returns(mockBookingSeatDbSet.Object);

        // Clear holding seats
        var holdingSeatsField = typeof(SeatService).GetField("_holdingSeats", BindingFlags.NonPublic | BindingFlags.Static);
        var holdingSeats = (ConcurrentDictionary<(Guid, Guid), (Guid userId, DateTime expireAt)>)holdingSeatsField!.GetValue(null)!;
        holdingSeats.Clear();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _seatService.HoldSeatsAsync(userId, showTimeId, new List<Guid> { seatId }));

        // Check the outer exception message (generic wrapper)
        Assert.Equal("An error occurred while holding seats. Please try again later.", ex.Message);

        // Check the inner exception for the specific seat message
        Assert.NotNull(ex.InnerException);
        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Contains($"Seat {seatId} is already Booked", ex.InnerException.Message);

        // Verify warning logging
        _mockLoggerService.Verify(
            l => l.Warn($"Seat {seatId} is already Booked"),
            Times.Once);

        // Clean up
        holdingSeats.Clear();
    }

    [Fact]
    public async Task HoldSeatsAsync_SeatAlreadySold_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var seatId = Guid.NewGuid();

        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = cinemaRoomId };
        var seat = new Seat
        {
            Id = seatId,
            CinemaRoomId = cinemaRoomId,
            IsDeleted = false
        };
        var showTimeSeat = new ShowTimeSeat
        {
            SeatId = seatId,
            ShowTimeId = showTimeId,
            Status = SeatStatus.Sold
        };

        // Setup mocks
        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId)).ReturnsAsync(showTime);

        var mockSeatDbSet = new List<Seat> { seat }.AsQueryable().BuildMockDbSet();
        _mockSeatRepository.Setup(r => r.GetQueryable()).Returns(mockSeatDbSet.Object);

        var mockShowTimeSeatDbSet = new List<ShowTimeSeat> { showTimeSeat }.AsQueryable().BuildMockDbSet();
        _mockShowTimeSeatRepository.Setup(r => r.GetQueryable()).Returns(mockShowTimeSeatDbSet.Object);

        var mockBookingSeatDbSet = new List<BookingSeat>().AsQueryable().BuildMockDbSet();
        _mockBookingSeatRepository.Setup(r => r.GetQueryable()).Returns(mockBookingSeatDbSet.Object);

        // Clear holding seats
        var holdingSeatsField = typeof(SeatService).GetField("_holdingSeats", BindingFlags.NonPublic | BindingFlags.Static);
        var holdingSeats = (ConcurrentDictionary<(Guid, Guid), (Guid userId, DateTime expireAt)>)holdingSeatsField!.GetValue(null)!;
        holdingSeats.Clear();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _seatService.HoldSeatsAsync(userId, showTimeId, new List<Guid> { seatId }));

        // Check the outer exception message (generic wrapper)
        Assert.Equal("An error occurred while holding seats. Please try again later.", ex.Message);

        // Check the inner exception for the specific seat message
        Assert.NotNull(ex.InnerException);
        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Contains($"Seat {seatId} is already Sold", ex.InnerException.Message);

        // Verify warning logging
        _mockLoggerService.Verify(
            l => l.Warn($"Seat {seatId} is already Sold"),
            Times.Once);

        // Clean up
        holdingSeats.Clear();
    }

    [Fact]
    public async Task HoldSeatsAsync_SuccessfullyHoldsNewSeats_ReturnsHeldSeats()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var seatIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = cinemaRoomId };
        var seats = seatIds.Select((id, index) => new Seat
        {
            Id = id,
            CinemaRoomId = cinemaRoomId,
            Row = "A",
            Number = index + 1,
            Type = SeatType.Normal,
            IsDeleted = false
        }).ToList();

        // Setup mocks
        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId)).ReturnsAsync(showTime);

        var mockSeatDbSet = seats.AsQueryable().BuildMockDbSet();
        _mockSeatRepository.Setup(r => r.GetQueryable()).Returns(mockSeatDbSet.Object);

        var mockShowTimeSeatDbSet = new List<ShowTimeSeat>().AsQueryable().BuildMockDbSet();
        _mockShowTimeSeatRepository.Setup(r => r.GetQueryable()).Returns(mockShowTimeSeatDbSet.Object);

        var mockBookingSeatDbSet = new List<BookingSeat>().AsQueryable().BuildMockDbSet();
        _mockBookingSeatRepository.Setup(r => r.GetQueryable()).Returns(mockBookingSeatDbSet.Object);

        // Clear holding seats
        var holdingSeatsField = typeof(SeatService).GetField("_holdingSeats", BindingFlags.NonPublic | BindingFlags.Static);
        var holdingSeats = (ConcurrentDictionary<(Guid, Guid), (Guid userId, DateTime expireAt)>)holdingSeatsField!.GetValue(null)!;
        holdingSeats.Clear();

        // Act
        var result = await _seatService.HoldSeatsAsync(userId, showTimeId, seatIds);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, seat => Assert.Equal(SeatType.Normal, seat.Type));
        Assert.Contains(result, seat => seat.Row == "A" && seat.Number == 1);
        Assert.Contains(result, seat => seat.Row == "A" && seat.Number == 2);

        // Verify seats are added to holding collection
        Assert.Equal(2, holdingSeats.Count);
        foreach (var seatId in seatIds)
        {
            Assert.True(holdingSeats.ContainsKey((seatId, showTimeId)));
            var holdInfo = holdingSeats[(seatId, showTimeId)];
            Assert.Equal(userId, holdInfo.userId);
            Assert.True(holdInfo.expireAt > DateTime.UtcNow);
        }

        // Verify success logging
        _mockLoggerService.Verify(
            l => l.Success(It.Is<string>(msg =>
                msg.Contains($"User {userId} successfully held 2 seats for showtime {showTimeId}"))),
            Times.Once);

        // Clean up
        holdingSeats.Clear();
    }

    [Fact]
    public async Task HoldSeatsAsync_DatabaseError_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var seatIds = new List<Guid> { Guid.NewGuid() };
        var expectedErrorMessage = "Database connection failed";

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId))
            .Throws(new Exception(expectedErrorMessage));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _seatService.HoldSeatsAsync(userId, showTimeId, seatIds));

        Assert.Equal("An error occurred while holding seats. Please try again later.", ex.Message);
        Assert.Equal(expectedErrorMessage, ex.InnerException?.Message);

        // Verify error logging
        _mockLoggerService.Verify(
            l => l.Error(It.Is<string>(msg =>
                msg.Contains($"Error holding seats for user {userId} for showtime {showTimeId}:") &&
                msg.Contains(expectedErrorMessage))),
            Times.Once);
    }

    [Fact]
    public async Task HoldSeatsAsync_VerifiesInfoLogging()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var seatId = Guid.NewGuid();

        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = cinemaRoomId };
        var seat = new Seat
        {
            Id = seatId,
            CinemaRoomId = cinemaRoomId,
            Row = "A",
            Number = 1,
            Type = SeatType.Normal,
            IsDeleted = false
        };

        // Setup mocks
        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId)).ReturnsAsync(showTime);

        var mockSeatDbSet = new List<Seat> { seat }.AsQueryable().BuildMockDbSet();
        _mockSeatRepository.Setup(r => r.GetQueryable()).Returns(mockSeatDbSet.Object);

        var mockShowTimeSeatDbSet = new List<ShowTimeSeat>().AsQueryable().BuildMockDbSet();
        _mockShowTimeSeatRepository.Setup(r => r.GetQueryable()).Returns(mockShowTimeSeatDbSet.Object);

        var mockBookingSeatDbSet = new List<BookingSeat>().AsQueryable().BuildMockDbSet();
        _mockBookingSeatRepository.Setup(r => r.GetQueryable()).Returns(mockBookingSeatDbSet.Object);

        // Clear holding seats
        var holdingSeatsField = typeof(SeatService).GetField("_holdingSeats", BindingFlags.NonPublic | BindingFlags.Static);
        var holdingSeats = (ConcurrentDictionary<(Guid, Guid), (Guid userId, DateTime expireAt)>)holdingSeatsField!.GetValue(null)!;
        holdingSeats.Clear();

        // Act
        await _seatService.HoldSeatsAsync(userId, showTimeId, new List<Guid> { seatId });

        // Assert - Verify all expected info logging calls
        _mockLoggerService.Verify(
            l => l.Info(It.Is<string>(msg =>
                msg.Contains($"User {userId} is attempting to hold seats for showtime {showTimeId}"))),
            Times.Once);

        _mockLoggerService.Verify(
            l => l.Info(It.Is<string>(msg =>
                msg.Contains($"User {userId} currently holds 0 other seats for showtime {showTimeId}"))),
            Times.Once);

        _mockLoggerService.Verify(
            l => l.Info(It.Is<string>(msg =>
                msg.Contains($"User {userId} has 0 seats in existing bookings for showtime {showTimeId}"))),
            Times.Once);

        // Clean up
        holdingSeats.Clear();
    }

    [Fact]
    public async Task GetSeatByIdAsync_ThrowsIfSeatNotFound()
    {
        _mockSeatRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Seat)null!);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _seatService.GetSeatByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetSeatByIdAsync_ThrowsIfShowTimeSeatNotFound()
    {
        var seatId = Guid.NewGuid();
        var seat = new Seat { Id = seatId };
        _mockSeatRepository.Setup(r => r.GetByIdAsync(seatId)).ReturnsAsync(seat);
        _mockShowTimeSeatRepository.Setup(r => r.GetQueryable()).Returns(new List<ShowTimeSeat>().AsQueryable().BuildMock());

        await Assert.ThrowsAsync<InvalidOperationException>(() => _seatService.GetSeatByIdAsync(seatId));
    }

    [Fact]
    public async Task HoldSeatsAsync_AttemptHoldBookedOrSoldSeats()
    {
        var userId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();
        var seatId = Guid.NewGuid();

        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = Guid.NewGuid() };
        var seat = new Seat { Id = seatId, CinemaRoomId = showTime.CinemaRoomId };
        var showTimeSeat = new ShowTimeSeat { SeatId = seatId, ShowTimeId = showTimeId, Status = SeatStatus.Booked };

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId)).ReturnsAsync(showTime);
        _mockSeatRepository.Setup(r => r.GetQueryable()).Returns(new List<Seat> { seat }.AsQueryable().BuildMock());
        _mockShowTimeSeatRepository.Setup(r => r.GetQueryable()).Returns(new List<ShowTimeSeat> { showTimeSeat }.AsQueryable().BuildMock());

        await Assert.ThrowsAsync<InvalidOperationException>(() => _seatService.HoldSeatsAsync(userId, showTimeId, new List<Guid> { seatId }));
    }


    [Fact]
    public void CleanupExpiredHolds_RemovesExpiredHolds()
    {
        var seatId = Guid.NewGuid();
        var showTimeId = Guid.NewGuid();

        // Dùng Reflection để lấy _holdingSeats
        var holdingSeatsField = typeof(SeatService).GetField("_holdingSeats", BindingFlags.NonPublic | BindingFlags.Static);
        var holdingSeats = (ConcurrentDictionary<(Guid, Guid), (Guid userId, DateTime expireAt)>)holdingSeatsField!.GetValue(null)!;

        holdingSeats.Clear();
        // Add expired and valid holds
        holdingSeats.TryAdd((seatId, showTimeId), (Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-1)));
        holdingSeats.TryAdd((Guid.NewGuid(), showTimeId), (Guid.NewGuid(), DateTime.UtcNow.AddMinutes(10)));

        var cleanupMethod = typeof(SeatService).GetMethod("CleanupExpiredHolds", BindingFlags.NonPublic | BindingFlags.Static);
        cleanupMethod!.Invoke(null, null);

        Assert.DoesNotContain(holdingSeats, kvp => kvp.Value.expireAt < DateTime.UtcNow);
    }

    [Fact]
    public void CleanupExpiredHolds_ValidHoldsRemain()
    {
        var seatId1 = Guid.NewGuid();
        var showTimeId1 = Guid.NewGuid();
        var seatId2 = Guid.NewGuid();
        var showTimeId2 = Guid.NewGuid();

        // Dùng Reflection để lấy _holdingSeats
        var holdingSeatsField = typeof(SeatService).GetField("_holdingSeats", BindingFlags.NonPublic | BindingFlags.Static);
        var holdingSeats = (ConcurrentDictionary<(Guid, Guid), (Guid userId, DateTime expireAt)>)holdingSeatsField!.GetValue(null)!;

        holdingSeats.Clear();

        // Thêm 2 hold hợp lệ (chưa hết hạn)
        holdingSeats.TryAdd((seatId1, showTimeId1), (Guid.NewGuid(), DateTime.UtcNow.AddMinutes(5)));
        holdingSeats.TryAdd((seatId2, showTimeId2), (Guid.NewGuid(), DateTime.UtcNow.AddMinutes(10)));

        // Gọi hàm cleanup
        var cleanupMethod = typeof(SeatService).GetMethod("CleanupExpiredHolds", BindingFlags.NonPublic | BindingFlags.Static);
        cleanupMethod!.Invoke(null, null);

        // Assert: vẫn còn đúng 2 hold hợp lệ
        Assert.Equal(2, holdingSeats.Count);
    }

    [Fact]
    public async Task GetShowTimeSeatStatusAsync_ReturnsAvailableIfNotHeld()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();

        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = cinemaRoomId };
        var seat = new Seat { Id = seatId, CinemaRoomId = cinemaRoomId };
        var showTimeSeat = new ShowTimeSeat { SeatId = seatId, ShowTimeId = showTimeId, Status = SeatStatus.Available };

        // Mock ShowTimeSeat
        var mockShowTimeSeatDbSet = new List<ShowTimeSeat> { showTimeSeat }
            .AsQueryable()
            .BuildMockDbSet();

        // Mock Seat
        var mockSeatDbSet = new List<Seat> { seat }
            .AsQueryable()
            .BuildMockDbSet();

        _mockShowTimeRepository
            .Setup(r => r.GetByIdAsync(showTimeId))
            .ReturnsAsync(showTime);

        _mockShowTimeSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockShowTimeSeatDbSet.Object);

        _mockSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockSeatDbSet.Object);

        // Act
        var result = await _seatService.GetShowTimeSeatStatusAsync(showTimeId);

        // Assert
        Assert.Contains(result, s => s.Status == SeatStatus.Available);
    }

    [Fact]
    public async Task GetShowTimeSeatStatusAsync_ReturnsBookedIfBooked()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();

        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = cinemaRoomId };
        var seat = new Seat { Id = seatId, CinemaRoomId = cinemaRoomId };
        var showTimeSeat = new ShowTimeSeat
        {
            SeatId = seatId,
            ShowTimeId = showTimeId,
            Status = SeatStatus.Booked
        };

        // Mock DbSets
        var mockShowTimeSeatDbSet = new List<ShowTimeSeat> { showTimeSeat }
            .AsQueryable()
            .BuildMockDbSet();

        var mockSeatDbSet = new List<Seat> { seat }
            .AsQueryable()
            .BuildMockDbSet();

        // Setup mock returns
        _mockShowTimeRepository
            .Setup(r => r.GetByIdAsync(showTimeId))
            .ReturnsAsync(showTime);

        _mockShowTimeSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockShowTimeSeatDbSet.Object);

        _mockSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockSeatDbSet.Object);

        // Act
        var result = await _seatService.GetShowTimeSeatStatusAsync(showTimeId);

        // Assert
        Assert.Contains(result, s => s.Status == SeatStatus.Booked);
    }

    [Fact]
    public async Task GetSeatsByShowTimeAsync_MixedSeatStatuses()
    {
        // Arrange
        var showTimeId = Guid.NewGuid();
        var cinemaRoomId = Guid.NewGuid();
        var seatIds = Enumerable.Range(1, 3).Select(_ => Guid.NewGuid()).ToList();

        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = cinemaRoomId };

        var showTimeSeats = new List<ShowTimeSeat>
    {
        new() { SeatId = seatIds[0], ShowTimeId = showTimeId, Status = SeatStatus.Holding },
        new() { SeatId = seatIds[1], ShowTimeId = showTimeId, Status = SeatStatus.Booked },
        new() { SeatId = seatIds[2], ShowTimeId = showTimeId, Status = SeatStatus.Sold }
    };

        var seats = new List<Seat>
    {
        new() { Id = seatIds[0], CinemaRoomId = cinemaRoomId },
        new() { Id = seatIds[1], CinemaRoomId = cinemaRoomId },
        new() { Id = seatIds[2], CinemaRoomId = cinemaRoomId }
    };

        var mockShowTimeSeatDbSet = showTimeSeats.AsQueryable().BuildMockDbSet();
        var mockSeatDbSet = seats.AsQueryable().BuildMockDbSet();

        _mockShowTimeRepository
            .Setup(r => r.GetByIdAsync(showTimeId))
            .ReturnsAsync(showTime);

        _mockShowTimeSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockShowTimeSeatDbSet.Object);

        _mockSeatRepository
            .Setup(r => r.GetQueryable())
            .Returns(mockSeatDbSet.Object);

        // Act
        var result = await _seatService.GetSeatsByShowTimeAsync(showTimeId);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, s => s.Status == SeatStatus.Holding);
        Assert.Contains(result, s => s.Status == SeatStatus.Booked);
        Assert.Contains(result, s => s.Status == SeatStatus.Sold);
    }


    [Fact]
    public async Task GetSeatsByShowTimeAsync_ShowTimeNotFound_Throws()
    {
        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ShowTime)null!);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _seatService.GetSeatsByShowTimeAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetSeatsByShowTimeAsync_LogsErrorOnException()
    {
        var showTimeId = Guid.NewGuid();
        var showTime = new ShowTime { Id = showTimeId, CinemaRoomId = Guid.NewGuid() };

        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(showTimeId)).ReturnsAsync(showTime);
        _mockShowTimeSeatRepository.Setup(r => r.GetQueryable()).Throws(new Exception("DB error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _seatService.GetSeatsByShowTimeAsync(showTimeId));
        _mockLoggerService.Verify(l => l.Error(It.IsAny<string>()), Times.AtLeastOnce());

    }

    [Fact]
    public async Task GetSeatByIdAsync_ReturnsShowTimeSeatDto_WhenSeatAndShowTimeSeatExist()
    {
        // Arrange
        var seatId = Guid.NewGuid();
        var seat = new Seat
        {
            Id = seatId,
            Row = "A",
            Number = 5,
            Type = SeatType.VIP
        };
        var showTimeSeat = new ShowTimeSeat
        {
            SeatId = seatId,
            Status = SeatStatus.Available
        };

        _mockSeatRepository.Setup(r => r.GetByIdAsync(seatId)).ReturnsAsync(seat);

        var mockShowTimeSeatDbSet = new List<ShowTimeSeat> { showTimeSeat }.AsQueryable().BuildMockDbSet();
        _mockShowTimeSeatRepository.Setup(r => r.GetQueryable()).Returns(mockShowTimeSeatDbSet.Object);

        // Act
        var result = await _seatService.GetSeatByIdAsync(seatId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(seatId, result.SeatId);
        Assert.Equal("A", result.Row);
        Assert.Equal(5, result.Number);
        Assert.Equal(SeatType.VIP, result.Type);
        Assert.Equal(SeatStatus.Available, result.Status);

        // Verify info logging was called
        _mockLoggerService.Verify(
            l => l.Info($"Retrieving seat with ID {seatId}"),
            Times.Once);

        // Verify success logging was called
        _mockLoggerService.Verify(
            l => l.Success($"Successfully retrieved seat with ID {seatId}"),
            Times.Once);
    }

    [Fact]
    public async Task GetSeatByIdAsync_ReturnsCorrectStatus_WhenShowTimeSeatIsBooked()
    {
        // Arrange
        var seatId = Guid.NewGuid();
        var seat = new Seat
        {
            Id = seatId,
            Row = "B",
            Number = 10,
            Type = SeatType.Normal
        };
        var showTimeSeat = new ShowTimeSeat
        {
            SeatId = seatId,
            Status = SeatStatus.Booked
        };

        _mockSeatRepository.Setup(r => r.GetByIdAsync(seatId)).ReturnsAsync(seat);

        var mockShowTimeSeatDbSet = new List<ShowTimeSeat> { showTimeSeat }.AsQueryable().BuildMockDbSet();
        _mockShowTimeSeatRepository.Setup(r => r.GetQueryable()).Returns(mockShowTimeSeatDbSet.Object);

        // Act
        var result = await _seatService.GetSeatByIdAsync(seatId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(seatId, result.SeatId);
        Assert.Equal("B", result.Row);
        Assert.Equal(10, result.Number);
        Assert.Equal(SeatType.Normal, result.Type);
        Assert.Equal(SeatStatus.Booked, result.Status);

        // Verify success logging was called
        _mockLoggerService.Verify(
            l => l.Success($"Successfully retrieved seat with ID {seatId}"),
            Times.Once);
    }

    [Fact]
    public async Task GetSeatByIdAsync_ReturnsCorrectStatus_WhenShowTimeSeatIsSold()
    {
        // Arrange
        var seatId = Guid.NewGuid();
        var seat = new Seat
        {
            Id = seatId,
            Row = "C",
            Number = 15,
            Type = SeatType.VIP
        };
        var showTimeSeat = new ShowTimeSeat
        {
            SeatId = seatId,
            Status = SeatStatus.Sold
        };

        _mockSeatRepository.Setup(r => r.GetByIdAsync(seatId)).ReturnsAsync(seat);

        var mockShowTimeSeatDbSet = new List<ShowTimeSeat> { showTimeSeat }.AsQueryable().BuildMockDbSet();
        _mockShowTimeSeatRepository.Setup(r => r.GetQueryable()).Returns(mockShowTimeSeatDbSet.Object);

        // Act
        var result = await _seatService.GetSeatByIdAsync(seatId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(seatId, result.SeatId);
        Assert.Equal("C", result.Row);
        Assert.Equal(15, result.Number);
        Assert.Equal(SeatType.VIP, result.Type);
        Assert.Equal(SeatStatus.Sold, result.Status);

        // Verify success logging was called
        _mockLoggerService.Verify(
            l => l.Success($"Successfully retrieved seat with ID {seatId}"),
            Times.Once);
    }

    [Fact]
    public async Task GetSeatByIdAsync_SeatNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var seatId = Guid.NewGuid();
        _mockSeatRepository.Setup(r => r.GetByIdAsync(seatId)).ReturnsAsync((Seat)null!);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _seatService.GetSeatByIdAsync(seatId));

        // Verify the inner exception is KeyNotFoundException
        Assert.IsType<KeyNotFoundException>(ex.InnerException);
        Assert.Equal("Seat not found.", ex.InnerException.Message);

        // Verify info logging was called
        _mockLoggerService.Verify(
            l => l.Info($"Retrieving seat with ID {seatId}"),
            Times.Once);

        // Verify warning logging was called
        _mockLoggerService.Verify(
            l => l.Warn($"Seat not found: {seatId}"),
            Times.Once);

        // Verify success logging was never called
        _mockLoggerService.Verify(
            l => l.Success(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task GetSeatByIdAsync_ShowTimeSeatNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var seatId = Guid.NewGuid();
        var seat = new Seat { Id = seatId, Row = "D", Number = 20, Type = SeatType.Normal };

        _mockSeatRepository.Setup(r => r.GetByIdAsync(seatId)).ReturnsAsync(seat);

        var mockShowTimeSeatDbSet = new List<ShowTimeSeat>().AsQueryable().BuildMockDbSet();
        _mockShowTimeSeatRepository.Setup(r => r.GetQueryable()).Returns(mockShowTimeSeatDbSet.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _seatService.GetSeatByIdAsync(seatId));

        // Verify the inner exception is KeyNotFoundException
        Assert.IsType<KeyNotFoundException>(ex.InnerException);
        Assert.Equal("ShowTimeSeat not found for the specified seat.", ex.InnerException.Message);

        // Verify info logging was called
        _mockLoggerService.Verify(
            l => l.Info($"Retrieving seat with ID {seatId}"),
            Times.Once);

        // Verify warning logging was called
        _mockLoggerService.Verify(
            l => l.Warn($"ShowTimeSeat not found for seat ID: {seatId}"),
            Times.Once);

        // Verify success logging was never called
        _mockLoggerService.Verify(
            l => l.Success(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task GetSeatByIdAsync_DatabaseError_ThrowsInvalidOperationException()
    {
        // Arrange
        var seatId = Guid.NewGuid();
        var expectedErrorMessage = "Database connection failed";

        _mockSeatRepository.Setup(r => r.GetByIdAsync(seatId))
            .Throws(new Exception(expectedErrorMessage));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _seatService.GetSeatByIdAsync(seatId));

        // Verify the exception message and inner exception
        Assert.Equal("An error occurred while retrieving the seat.", ex.Message);
        Assert.Equal(expectedErrorMessage, ex.InnerException?.Message);

        // Verify info logging was called
        _mockLoggerService.Verify(
            l => l.Info($"Retrieving seat with ID {seatId}"),
            Times.Once);

        // Verify error logging was called
        _mockLoggerService.Verify(
            l => l.Error($"Error retrieving seat with ID {seatId}: {expectedErrorMessage}"),
            Times.Once);

        // Verify success logging was never called
        _mockLoggerService.Verify(
            l => l.Success(It.IsAny<string>()),
            Times.Never);
    }
}