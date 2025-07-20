using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.API.Controllers;
using MovieTheater.Application.Interfaces;
using MovieTheater.Domain.DTOs.BookingDTOs;
using MovieTheater.Domain.DTOs.InvoiceDTOs;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.UnitTest.Controllers
{
    public class InvoiceControllerTests
    {
        private readonly Mock<IInvoiceService> _invoiceServiceMock;
        private readonly Mock<IClaimsService> _claimsServiceMock;
        private readonly InvoiceController _controller;

        public InvoiceControllerTests()
        {
            _invoiceServiceMock = new Mock<IInvoiceService>();
            _claimsServiceMock = new Mock<IClaimsService>();
            _controller = new InvoiceController(_invoiceServiceMock.Object, _claimsServiceMock.Object);
        }

        [Fact]
        public async Task GetInvoice_ReturnsOk_WhenInvoiceExists()
        {
            var id = Guid.NewGuid();
            var invoice = new InvoiceDto { Id = id, Booking = new BookingSummaryDto() };
            _invoiceServiceMock.Setup(s => s.GetInvoiceByIdAsync(id)).ReturnsAsync(invoice);

            var result = await _controller.GetInvoice(id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetInvoice_ReturnsNotFound_WhenInvoiceDoesNotExist()
        {
            var id = Guid.NewGuid();
            _invoiceServiceMock.Setup(s => s.GetInvoiceByIdAsync(id)).ReturnsAsync((InvoiceDto)null!);

            var result = await _controller.GetInvoice(id);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task GetInvoice_ReturnsStatusCode_OnException()
        {
            var id = Guid.NewGuid();
            _invoiceServiceMock.Setup(s => s.GetInvoiceByIdAsync(id)).ThrowsAsync(new Exception("Test"));

            var result = await _controller.GetInvoice(id);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task GetInvoiceByBooking_ReturnsOk_WhenInvoiceExists()
        {
            var id = Guid.NewGuid();
            var invoice = new InvoiceDto { Id = id, Booking = new BookingSummaryDto() };
            _invoiceServiceMock.Setup(s => s.GetInvoiceByBookingIdAsync(id)).ReturnsAsync(invoice);

            var result = await _controller.GetInvoiceByBooking(id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetInvoiceByBooking_ReturnsNotFound_WhenInvoiceDoesNotExist()
        {
            var id = Guid.NewGuid();
            _invoiceServiceMock.Setup(s => s.GetInvoiceByBookingIdAsync(id)).ReturnsAsync((InvoiceDto)null!);

            var result = await _controller.GetInvoiceByBooking(id);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task GetInvoiceByBooking_ReturnsStatusCode_OnException()
        {
            var id = Guid.NewGuid();
            _invoiceServiceMock.Setup(s => s.GetInvoiceByBookingIdAsync(id)).ThrowsAsync(new Exception("Test"));

            var result = await _controller.GetInvoiceByBooking(id);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task GetUserInvoices_ReturnsOk()
        {
            var userId = Guid.NewGuid();
            _claimsServiceMock.SetupGet(s => s.GetCurrentUserId).Returns(userId);
            _invoiceServiceMock.Setup(s => s.GetUserInvoicesAsync(userId)).ReturnsAsync(new List<InvoiceDto>());

            var result = await _controller.GetUserInvoices();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetUserInvoices_ReturnsStatusCode_OnException()
        {
            var userId = Guid.NewGuid();
            _claimsServiceMock.SetupGet(s => s.GetCurrentUserId).Returns(userId);
            _invoiceServiceMock.Setup(s => s.GetUserInvoicesAsync(userId)).ThrowsAsync(new Exception("Test"));

            var result = await _controller.GetUserInvoices();

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task CreateInvoice_ReturnsOk()
        {
            var bookingId = Guid.NewGuid();
            var request = new CreateInvoiceRequest { PromotionId = Guid.NewGuid(), RequestedPoints = 10 };
            var invoice = new InvoiceDto { Id = Guid.NewGuid(), Booking = new BookingSummaryDto() };
            _invoiceServiceMock.Setup(s => s.CreateInvoiceAsync(bookingId, request.PromotionId, request.RequestedPoints)).ReturnsAsync(invoice);

            var result = await _controller.CreateInvoice(bookingId, request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task CreateInvoice_ReturnsNotFound_OnKeyNotFoundException()
        {
            var bookingId = Guid.NewGuid();
            var request = new CreateInvoiceRequest();
            _invoiceServiceMock.Setup(s => s.CreateInvoiceAsync(bookingId, request.PromotionId, request.RequestedPoints))
                .ThrowsAsync(new KeyNotFoundException("Not found"));

            var result = await _controller.CreateInvoice(bookingId, request);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task CreateInvoice_ReturnsBadRequest_OnInvalidOperationException()
        {
            var bookingId = Guid.NewGuid();
            var request = new CreateInvoiceRequest();
            _invoiceServiceMock.Setup(s => s.CreateInvoiceAsync(bookingId, request.PromotionId, request.RequestedPoints))
                .ThrowsAsync(new InvalidOperationException("Invalid"));

            var result = await _controller.CreateInvoice(bookingId, request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task CreateInvoice_ReturnsStatusCode_OnException()
        {
            var bookingId = Guid.NewGuid();
            var request = new CreateInvoiceRequest();
            _invoiceServiceMock.Setup(s => s.CreateInvoiceAsync(bookingId, request.PromotionId, request.RequestedPoints))
                .ThrowsAsync(new Exception("Test"));

            var result = await _controller.CreateInvoice(bookingId, request);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task UpdateInvoiceStatus_ReturnsOk()
        {
            var id = Guid.NewGuid();
            var request = new InvoiceStatusUpdateRequest { Status = "Paid" };
            var invoice = new InvoiceDto { Id = id, Booking = new BookingSummaryDto() };
            _invoiceServiceMock.Setup(s => s.UpdateInvoiceStatusAsync(id, request.Status)).ReturnsAsync(invoice);

            var result = await _controller.UpdateInvoiceStatus(id, request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task UpdateInvoiceStatus_ReturnsNotFound_OnKeyNotFoundException()
        {
            var id = Guid.NewGuid();
            var request = new InvoiceStatusUpdateRequest { Status = "Paid" };
            _invoiceServiceMock.Setup(s => s.UpdateInvoiceStatusAsync(id, request.Status))
                .ThrowsAsync(new KeyNotFoundException("Not found"));

            var result = await _controller.UpdateInvoiceStatus(id, request);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateInvoiceStatus_ReturnsStatusCode_OnException()
        {
            var id = Guid.NewGuid();
            var request = new InvoiceStatusUpdateRequest { Status = "Paid" };
            _invoiceServiceMock.Setup(s => s.UpdateInvoiceStatusAsync(id, request.Status))
                .ThrowsAsync(new Exception("Test"));

            var result = await _controller.UpdateInvoiceStatus(id, request);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task GetAllInvoices_ReturnsOk()
        {
            var pagination = new Pagination<InvoiceDto>(new List<InvoiceDto>(), 0, 1, 10);
            _invoiceServiceMock.Setup(s => s.GetAllInvoicesAsync(1, 10, null, null, false, null)).ReturnsAsync(pagination);

            var result = await _controller.GetAllInvoices();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetAllInvoices_ReturnsStatusCode_OnException()
        {
            _invoiceServiceMock.Setup(s => s.GetAllInvoicesAsync(1, 10, null, null, false, null))
                .ThrowsAsync(new Exception("Test"));

            var result = await _controller.GetAllInvoices();

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }
    }
}