using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.API.Controllers;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.BookingDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MovieTheater.UnitTest.Controllers
{
    public class BookingControllerTests
    {
        private readonly Mock<IBookingService> _mockBookingService;
        private readonly Mock<IClaimsService> _mockClaimsService;
        private readonly BookingController _controller;

        public BookingControllerTests()
        {
            _mockBookingService = new Mock<IBookingService>();
            _mockClaimsService = new Mock<IClaimsService>();
            _controller = new BookingController(_mockBookingService.Object, _mockClaimsService.Object);
        }

        [Fact]
        public async Task GetBookingByIdAsync_ReturnsOk_WhenFound()
        {
            var bookingId = Guid.NewGuid();
            var booking = new BookingResponseDto { Id = bookingId };
            _mockBookingService.Setup(s => s.GetBookingByIdAsync(bookingId)).ReturnsAsync(booking);

            var result = await _controller.GetBooking(bookingId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<BookingResponseDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(booking, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetBookingByIdAsync_ReturnsNotFound_WhenNull()
        {
            var bookingId = Guid.NewGuid();
            _mockBookingService.Setup(s => s.GetBookingByIdAsync(bookingId)).ReturnsAsync((BookingResponseDto)null!);

            var result = await _controller.GetBooking(bookingId);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(notFound.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetBookingByIdAsync_ReturnsError_WhenException()
        {
            var bookingId = Guid.NewGuid();
            _mockBookingService.Setup(s => s.GetBookingByIdAsync(bookingId)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.GetBooking(bookingId);

            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetUserBookingsAsync_ReturnsOk()
        {
            var userId = Guid.NewGuid();
            var bookings = new List<BookingResponseDto> { new BookingResponseDto { Id = Guid.NewGuid() } };
            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(userId);
            _mockBookingService.Setup(s => s.GetUserBookingsAsync(userId)).ReturnsAsync(bookings);

            var result = await _controller.GetUserBookings();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<IEnumerable<BookingResponseDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(bookings, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetUserBookingsAsync_ReturnsError_WhenException()
        {
            var userId = Guid.NewGuid();
            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(userId);
            _mockBookingService.Setup(s => s.GetUserBookingsAsync(userId)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.GetUserBookings();

            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetAllBookingsAsync_ReturnsOk()
        {
            var bookings = new Pagination<BookingResponseDto> { Items = new List<BookingResponseDto>() };
            _mockBookingService.Setup(s => s.GetAllBookingsAsync(1, 10, null, null, false, null)).ReturnsAsync(bookings);

            var result = await _controller.GetAllBookings(1, 10, null, null, false, null);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<Pagination<BookingResponseDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(bookings, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetAllBookingsAsync_ReturnsError_WhenException()
        {
            _mockBookingService.Setup(s => s.GetAllBookingsAsync(1, 10, null, null, false, null)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.GetAllBookings(1, 10, null, null, false, null);

            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task CreateBookingAsync_ReturnsOk_WhenSuccess()
        {
            var userId = Guid.NewGuid();
            var request = new CreateBookingRequest();
            var booking = new BookingDto();
            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(userId);
            _mockBookingService.Setup(s => s.CreateBookingAsync(userId, request)).ReturnsAsync(booking);

            var result = await _controller.CreateBooking(request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<BookingDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(booking, apiResult.Value.Data);
        }

        [Fact]
        public async Task CreateBookingAsync_ReturnsError_WhenException()
        {
            var userId = Guid.NewGuid();
            var request = new CreateBookingRequest();
            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(userId);
            _mockBookingService.Setup(s => s.CreateBookingAsync(userId, request)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.CreateBooking(request);

            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task CancelBookingAsync_ReturnsOk_WhenSuccess()
        {
            var bookingId = Guid.NewGuid();

            var fakeBooking = new BookingResponseDto { Id = bookingId };

            _mockBookingService
                .Setup(s => s.GetBookingByIdAsync(bookingId))
                .ReturnsAsync(fakeBooking);

            _mockBookingService
                .Setup(s => s.CancelBookingAsync(bookingId))
                .ReturnsAsync(true);

            var result = await _controller.CancelBooking(bookingId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(okResult.Value);

            Assert.True(apiResult.IsSuccess);
            Assert.Equal("Cancelled booking successfully", apiResult.Value.Message);
        }


        [Fact]
        public async Task CancelBookingAsync_ReturnsBadRequest_WhenFail()
        {
            var bookingId = Guid.NewGuid();
            var fakeBooking = new BookingResponseDto { Id = bookingId };

            _mockBookingService
                .Setup(s => s.GetBookingByIdAsync(bookingId))
                .ReturnsAsync(fakeBooking);

            _mockBookingService
                .Setup(s => s.CancelBookingAsync(bookingId))
                .ReturnsAsync(false); // mock thất bại

            var result = await _controller.CancelBooking(bookingId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequest.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task CancelBookingAsync_ReturnsError_WhenException()
        {
            var bookingId = Guid.NewGuid();
            _mockBookingService.Setup(s => s.CancelBookingAsync(bookingId)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.CancelBooking(bookingId);

            var objectResult = Assert.IsType<NotFoundObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }
        [Fact]
        public async Task CreateBookingAsync_ReturnsBadRequest_WhenArgumentException()
        {
            var userId = Guid.NewGuid();
            var request = new CreateBookingRequest();
            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(userId);
            _mockBookingService.Setup(s => s.CreateBookingAsync(userId, request))
                .ThrowsAsync(new ArgumentException("invalid argument"));

            var result = await _controller.CreateBooking(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequest.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task CreateBookingAsync_ReturnsBadRequest_WhenInvalidOperationException()
        {
            var userId = Guid.NewGuid();
            var request = new CreateBookingRequest();
            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(userId);
            _mockBookingService.Setup(s => s.CreateBookingAsync(userId, request))
                .ThrowsAsync(new InvalidOperationException("invalid operation"));

            var result = await _controller.CreateBooking(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequest.Value);
            Assert.False(apiResult.IsSuccess);
        }
        [Fact]
        public async Task CancelBookingAsync_ReturnsStatusCode_WhenException()
        {
            var bookingId = Guid.NewGuid();
            _mockBookingService.Setup(s => s.GetBookingByIdAsync(bookingId))
                .ReturnsAsync(new BookingResponseDto { Id = bookingId });
            _mockBookingService.Setup(s => s.CancelBookingAsync(bookingId))
                .ThrowsAsync(new Exception("unexpected error"));

            var result = await _controller.CancelBooking(bookingId);

            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }
    }
}