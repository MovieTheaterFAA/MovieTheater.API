using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.API.Controllers;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.TicketDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace MovieTheater.UnitTest.Controllers
{
    public class TicketControllerTests
    {
        private readonly Mock<ITicketService> _mockTicketService;
        private readonly Mock<IClaimsService> _mockClaimsService;
        private readonly TicketController _controller;

        public TicketControllerTests()
        {
            _mockTicketService = new Mock<ITicketService>();
            _mockClaimsService = new Mock<IClaimsService>();
            _controller = new TicketController(_mockTicketService.Object, _mockClaimsService.Object);
        }

        [Fact]
        public async Task GetAllTicketsForAdmin_ReturnsOk_WhenValid()
        {
            var tickets = new Pagination<TicketResponseDto>(new List<TicketResponseDto>(), 0, 1, 10);
            _mockTicketService.Setup(s => s.GetAllTicketsAsync(1, 10, null, null, false, null)).ReturnsAsync(tickets);

            var result = await _controller.GetAllTicketsForAdmin(1, 10, null, null, false, null);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<Pagination<TicketResponseDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(tickets, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetAllTicketsForAdmin_ReturnsBadRequest_WhenInvalidPagination()
        {
            var result = await _controller.GetAllTicketsForAdmin(0, 0, null, null, false, null);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequest.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetAllTicketsForAdmin_ReturnsError_WhenException()
        {
            _mockTicketService.Setup(s => s.GetAllTicketsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TicketType?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("fail"));

            var result = await _controller.GetAllTicketsForAdmin(1, 10, null, null, false, null);

            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetTicket_ReturnsOk_WhenFound()
        {
            var ticketId = Guid.NewGuid();
            var ticket = new TicketResponseDto { Id = ticketId };
            _mockTicketService.Setup(s => s.GetTicketByIdAsync(ticketId)).ReturnsAsync(ticket);

            var result = await _controller.GetTicket(ticketId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<TicketResponseDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(ticket, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetTicket_ReturnsNotFound_WhenKeyNotFound()
        {
            var ticketId = Guid.NewGuid();
            _mockTicketService.Setup(s => s.GetTicketByIdAsync(ticketId)).ThrowsAsync(new KeyNotFoundException("not found"));

            var result = await _controller.GetTicket(ticketId);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(notFound.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetTicket_ReturnsError_WhenException()
        {
            var ticketId = Guid.NewGuid();
            _mockTicketService.Setup(s => s.GetTicketByIdAsync(ticketId)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.GetTicket(ticketId);

            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetUserTickets_ReturnsOk_WhenSuccess()
        {
            var userId = Guid.NewGuid();
            var tickets = new List<TicketResponseDto> { new TicketResponseDto { Id = Guid.NewGuid() } };
            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(userId);
            _mockTicketService.Setup(s => s.GetUserTicketsAsync(userId)).ReturnsAsync(tickets);

            var result = await _controller.GetUserTickets();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<IEnumerable<TicketResponseDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(tickets, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetUserTickets_ReturnsError_WhenException()
        {
            var userId = Guid.NewGuid();
            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(userId);
            _mockTicketService.Setup(s => s.GetUserTicketsAsync(userId)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.GetUserTickets();

            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task CreateOfflineTicket_ReturnsOk_WhenSuccess()
        {
            var request = new CreateOfflineTicketRequest { GuestPhoneNumber = "0123456789", ShowtimeId = Guid.NewGuid(), SeatIds = new List<Guid>() };
            var ticket = new TicketResponseDto { Id = Guid.NewGuid() };
            _mockTicketService.Setup(s => s.CreateOfflineTicketAsync(request)).ReturnsAsync(ticket);

            var result = await _controller.CreateOfflineTicket(request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<TicketResponseDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(ticket, apiResult.Value.Data);
        }

        [Fact]
        public async Task CreateOfflineTicket_ReturnsError_WhenException()
        {
            var request = new CreateOfflineTicketRequest { GuestPhoneNumber = "0123456789", ShowtimeId = Guid.NewGuid(), SeatIds = new List<Guid>() };
            _mockTicketService.Setup(s => s.CreateOfflineTicketAsync(request)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.CreateOfflineTicket(request);

            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetTicketQrCode_ReturnsOk_WhenSuccess()
        {
            var ticketId = Guid.NewGuid();
            var qr = "data:image/png;base64,abc";
            _mockTicketService.Setup(s => s.GenerateTicketQRCodeAsync(ticketId)).ReturnsAsync(qr);

            var result = await _controller.GetTicketQrCode(ticketId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<string>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(qr, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetTicketQrCode_ReturnsError_WhenException()
        {
            var ticketId = Guid.NewGuid();
            _mockTicketService.Setup(s => s.GenerateTicketQRCodeAsync(ticketId)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.GetTicketQrCode(ticketId);

            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task VerifyTicket_ReturnsOk_WhenSuccess()
        {
            var payload = new QrCodePayload { TicketId = Guid.NewGuid(), Hash = "hash", ExpiresAt = DateTime.UtcNow.AddMinutes(10) };
            var verifyResult = new TicketVerificationResultDto { IsValid = true, Message = "Ticket verified", Ticket = new TicketResponseDto { Id = payload.TicketId } };
            _mockTicketService.Setup(s => s.VerifyTicketQRCodeAsync(payload)).ReturnsAsync(verifyResult);

            var result = await _controller.VerifyTicket(payload);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<TicketVerificationResultDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(verifyResult, apiResult.Value.Data);
        }

        [Fact]
        public async Task VerifyTicket_ReturnsBadRequest_WhenJsonException()
        {
            var payload = new QrCodePayload { TicketId = Guid.NewGuid(), Hash = "hash", ExpiresAt = DateTime.UtcNow.AddMinutes(10) };
            _mockTicketService.Setup(s => s.VerifyTicketQRCodeAsync(payload)).ThrowsAsync(new JsonException("Invalid"));

            var result = await _controller.VerifyTicket(payload);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequest.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task VerifyTicket_ReturnsError_WhenException()
        {
            var payload = new QrCodePayload { TicketId = Guid.NewGuid(), Hash = "hash", ExpiresAt = DateTime.UtcNow.AddMinutes(10) };
            _mockTicketService.Setup(s => s.VerifyTicketQRCodeAsync(payload)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.VerifyTicket(payload);

            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }
    }
}