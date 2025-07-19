using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.API.Controllers;
using MovieTheater.Application.Interfaces;
using MovieTheater.Domain.DTOs.SeatDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
namespace MovieTheater.UnitTest.Controllers
{
    public class SeatControllerTests
    {
        private readonly Mock<ISeatService> _seatServiceMock;
        private readonly Mock<IClaimsService> _claimsServiceMock;
        private readonly SeatController _controller;

        public SeatControllerTests()
        {
            _seatServiceMock = new Mock<ISeatService>();
            _claimsServiceMock = new Mock<IClaimsService>();
            _controller = new SeatController(_seatServiceMock.Object, _claimsServiceMock.Object);
        }

        [Fact]
        public async Task GetSeatsByCinemaRoom_ReturnsOkResult()
        {
            var cinemaRoomId = Guid.NewGuid();
            var seats = new List<SeatDto> { new() { Id = Guid.NewGuid(), Row = "A", Number = 1, Type = SeatType.Normal, CinemaRoomId = cinemaRoomId } };
            _seatServiceMock.Setup(s => s.GetSeatsByCinemaRoomAsync(cinemaRoomId)).ReturnsAsync(seats);

            var result = await _controller.GetSeatsByCinemaRoom(cinemaRoomId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetSeatsByCinemaRoom_HandlesException()
        {
            var cinemaRoomId = Guid.NewGuid();
            _seatServiceMock.Setup(s => s.GetSeatsByCinemaRoomAsync(cinemaRoomId)).ThrowsAsync(new Exception("Test"));

            var result = await _controller.GetSeatsByCinemaRoom(cinemaRoomId);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        [Fact]
        public async Task BatchCreateSeats_ReturnsOkResult()
        {
            var roomId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var dto = new BatchCreateSeatDto { Seats = new List<CreateSeatDto>() };
            var seats = new List<SeatDto> { new() { Id = Guid.NewGuid(), Row = "A", Number = 1, Type = SeatType.Normal, CinemaRoomId = roomId } };
            _claimsServiceMock.SetupGet(c => c.GetCurrentUserId).Returns(adminId);
            _seatServiceMock.Setup(s => s.BatchCreateSeatsAsync(roomId, dto, adminId)).ReturnsAsync(seats);

            var result = await _controller.BatchCreateSeats(roomId, dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task BatchCreateSeats_HandlesException()
        {
            var roomId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var dto = new BatchCreateSeatDto { Seats = new List<CreateSeatDto>() };
            _claimsServiceMock.SetupGet(c => c.GetCurrentUserId).Returns(adminId);
            _seatServiceMock.Setup(s => s.BatchCreateSeatsAsync(roomId, dto, adminId)).ThrowsAsync(new Exception("Test"));

            var result = await _controller.BatchCreateSeats(roomId, dto);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        [Fact]
        public async Task UpdateSeat_ReturnsOkResult()
        {
            var seatId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var dto = new UpdateSeatDto { Row = "A", Number = 1, Type = SeatType.Normal };
            var seat = new SeatDto { Id = seatId, Row = "A", Number = 1, Type = SeatType.Normal, CinemaRoomId = Guid.NewGuid() };
            _claimsServiceMock.SetupGet(c => c.GetCurrentUserId).Returns(adminId);
            _seatServiceMock.Setup(s => s.UpdateSeatAsync(seatId, dto, adminId)).ReturnsAsync(seat);

            var result = await _controller.UpdateSeat(seatId, dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task UpdateSeat_ReturnsNotFound_WhenSeatIsNull()
        {
            var seatId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var dto = new UpdateSeatDto { Row = "A", Number = 1, Type = SeatType.Normal };
            _claimsServiceMock.SetupGet(c => c.GetCurrentUserId).Returns(adminId);
            _seatServiceMock.Setup(s => s.UpdateSeatAsync(seatId, dto, adminId)).ReturnsAsync((SeatDto?)null);

            var result = await _controller.UpdateSeat(seatId, dto);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateSeat_HandlesException()
        {
            var seatId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var dto = new UpdateSeatDto { Row = "A", Number = 1, Type = SeatType.Normal };
            _claimsServiceMock.SetupGet(c => c.GetCurrentUserId).Returns(adminId);
            _seatServiceMock.Setup(s => s.UpdateSeatAsync(seatId, dto, adminId)).ThrowsAsync(new Exception("Test"));

            var result = await _controller.UpdateSeat(seatId, dto);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        [Fact]
        public async Task DeleteSeat_ReturnsOkResult()
        {
            var seatId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            _claimsServiceMock.SetupGet(c => c.GetCurrentUserId).Returns(adminId);
            _seatServiceMock.Setup(s => s.SoftDeleteSeatAsync(seatId, adminId)).ReturnsAsync(true);

            var result = await _controller.DeleteSeat(seatId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task DeleteSeat_ReturnsNotFound_WhenNotSuccess()
        {
            var seatId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            _claimsServiceMock.SetupGet(c => c.GetCurrentUserId).Returns(adminId);
            _seatServiceMock.Setup(s => s.SoftDeleteSeatAsync(seatId, adminId)).ReturnsAsync(false);

            var result = await _controller.DeleteSeat(seatId);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteSeat_HandlesException()
        {
            var seatId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            _claimsServiceMock.SetupGet(c => c.GetCurrentUserId).Returns(adminId);
            _seatServiceMock.Setup(s => s.SoftDeleteSeatAsync(seatId, adminId)).ThrowsAsync(new Exception("Test"));

            var result = await _controller.DeleteSeat(seatId);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        [Fact]
        public async Task HoldSeatsAsync_ReturnsBadRequest_WhenRequestIsNull()
        {
            var result = await _controller.HoldSeatsAsync(null!);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task HoldSeatsAsync_ReturnsBadRequest_WhenSeatIdsIsNullOrEmpty()
        {
            var dto = new HoldSeatsRequestDto { ShowTimeId = Guid.NewGuid(), SeatIds = null! };
            var result = await _controller.HoldSeatsAsync(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);

            dto.SeatIds = new List<Guid>();
            result = await _controller.HoldSeatsAsync(dto);

            badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task HoldSeatsAsync_ReturnsOk_WhenSeatsHeld()
        {
            var userId = Guid.NewGuid();
            var showTimeId = Guid.NewGuid();
            var seatIds = new List<Guid> { Guid.NewGuid() };
            var heldSeats = new List<SeatResponseDto> { new() { Id = seatIds[0], Row = "A", Number = 1, Type = SeatType.Normal } };
            var dto = new HoldSeatsRequestDto { ShowTimeId = showTimeId, SeatIds = seatIds };
            _claimsServiceMock.SetupGet(c => c.GetCurrentUserId).Returns(userId);
            _seatServiceMock.Setup(s => s.HoldSeatsAsync(userId, showTimeId, seatIds)).ReturnsAsync(heldSeats);

            var result = await _controller.HoldSeatsAsync(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task HoldSeatsAsync_ReturnsConflict_WhenNoSeatsHeld()
        {
            var userId = Guid.NewGuid();
            var showTimeId = Guid.NewGuid();
            var seatIds = new List<Guid> { Guid.NewGuid() };
            var dto = new HoldSeatsRequestDto { ShowTimeId = showTimeId, SeatIds = seatIds };
            _claimsServiceMock.SetupGet(c => c.GetCurrentUserId).Returns(userId);
            _seatServiceMock.Setup(s => s.HoldSeatsAsync(userId, showTimeId, seatIds)).ReturnsAsync(new List<SeatResponseDto>());

            var result = await _controller.HoldSeatsAsync(dto);

            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.NotNull(conflictResult.Value);
        }

        [Fact]
        public async Task HoldSeatsAsync_HandlesException()
        {
            var userId = Guid.NewGuid();
            var showTimeId = Guid.NewGuid();
            var seatIds = new List<Guid> { Guid.NewGuid() };
            var dto = new HoldSeatsRequestDto { ShowTimeId = showTimeId, SeatIds = seatIds };
            _claimsServiceMock.SetupGet(c => c.GetCurrentUserId).Returns(userId);
            _seatServiceMock.Setup(s => s.HoldSeatsAsync(userId, showTimeId, seatIds)).ThrowsAsync(new Exception("Test"));

            var result = await _controller.HoldSeatsAsync(dto);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        [Fact]
        public async Task GetSeatsByShowTimeAsync_ReturnsOkResult()
        {
            var showTimeId = Guid.NewGuid();
            var seats = new List<ShowTimeSeatDto> { new() { SeatId = Guid.NewGuid(), Row = "A", Number = 1, Type = SeatType.Normal, Status = SeatStatus.Available } };
            _seatServiceMock.Setup(s => s.GetSeatsByShowTimeAsync(showTimeId)).ReturnsAsync(seats);

            var result = await _controller.GetSeatsByShowTimeAsync(showTimeId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetSeatsByShowTimeAsync_HandlesException()
        {
            var showTimeId = Guid.NewGuid();
            _seatServiceMock.Setup(s => s.GetSeatsByShowTimeAsync(showTimeId)).ThrowsAsync(new Exception("Test"));

            var result = await _controller.GetSeatsByShowTimeAsync(showTimeId);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }
    }
}