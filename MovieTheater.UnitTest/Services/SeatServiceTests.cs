using Microsoft.EntityFrameworkCore;
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
using System.Text.Json;

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
    public async Task GetShowTimeSeatStatusAsync_ThrowsIfShowTimeNotFound()
    {
        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ShowTime)null!);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _seatService.GetShowTimeSeatStatusAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetSeatsByShowTimeAsync_ThrowsIfShowTimeNotFound()
    {
        _mockShowTimeRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ShowTime)null!);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _seatService.GetSeatsByShowTimeAsync(Guid.NewGuid()));
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
}