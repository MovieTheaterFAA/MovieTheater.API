using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using MovieTheater.API.Controllers;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.PaymentDTOs;


namespace MovieTheater.UnitTest.Controllers
{
    public class PaymentControllerTests
    {
        private readonly Mock<IPaymentService> _paymentServiceMock;
        private readonly Mock<ILoggerService> _loggerServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly PaymentController _controller;

        public PaymentControllerTests()
        {
            _paymentServiceMock = new Mock<IPaymentService>();
            _loggerServiceMock = new Mock<ILoggerService>();
            _configurationMock = new Mock<IConfiguration>();
            _controller = new PaymentController(_paymentServiceMock.Object, _loggerServiceMock.Object, _configurationMock.Object);
        }

        [Fact]
        public async Task CreateCheckoutSession_ReturnsOk_WhenValid()
        {
            var invoiceId = Guid.NewGuid();
            var request = new CreateCheckoutRequest { InvoiceId = invoiceId };
            var sessionUrl = "https://checkout.url";
            _paymentServiceMock.Setup(s => s.InitiatePaymentAsync(invoiceId)).ReturnsAsync(sessionUrl);

            var result = await _controller.CreateCheckoutSession(request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task CreateCheckoutSession_ReturnsBadRequest_WhenInvoiceIdIsEmpty()
        {
            var request = new CreateCheckoutRequest { InvoiceId = Guid.Empty };

            var result = await _controller.CreateCheckoutSession(request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task CreateCheckoutSession_ReturnsNotFound_OnKeyNotFoundException()
        {
            var invoiceId = Guid.NewGuid();
            var request = new CreateCheckoutRequest { InvoiceId = invoiceId };
            _paymentServiceMock.Setup(s => s.InitiatePaymentAsync(invoiceId))
                .ThrowsAsync(new KeyNotFoundException("Not found"));

            var result = await _controller.CreateCheckoutSession(request);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task CreateCheckoutSession_ReturnsBadRequest_OnInvalidOperationException()
        {
            var invoiceId = Guid.NewGuid();
            var request = new CreateCheckoutRequest { InvoiceId = invoiceId };
            _paymentServiceMock.Setup(s => s.InitiatePaymentAsync(invoiceId))
                .ThrowsAsync(new InvalidOperationException("Invalid"));

            var result = await _controller.CreateCheckoutSession(request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task CreateCheckoutSession_ReturnsStatusCode500_OnException()
        {
            var invoiceId = Guid.NewGuid();
            var request = new CreateCheckoutRequest { InvoiceId = invoiceId };
            _paymentServiceMock.Setup(s => s.InitiatePaymentAsync(invoiceId))
                .ThrowsAsync(new Exception("Test"));

            var result = await _controller.CreateCheckoutSession(request);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task PaymentSuccess_ReturnsOk_WhenValid()
        {
            var sessionId = "valid_session";
            _paymentServiceMock.Setup(s => s.VerifyPaymentAsync(sessionId)).ReturnsAsync(true);

            var result = await _controller.PaymentSuccess(sessionId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task PaymentSuccess_ReturnsBadRequest_WhenSessionIdIsNullOrEmpty()
        {
            var result = await _controller.PaymentSuccess(null!);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);

            result = await _controller.PaymentSuccess(string.Empty);

            badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task PaymentSuccess_ReturnsBadRequest_WhenVerificationFails()
        {
            var sessionId = "invalid_session";
            _paymentServiceMock.Setup(s => s.VerifyPaymentAsync(sessionId)).ReturnsAsync(false);

            var result = await _controller.PaymentSuccess(sessionId);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task PaymentSuccess_ReturnsStatusCode_OnException()
        {
            var sessionId = "error_session";
            _paymentServiceMock.Setup(s => s.VerifyPaymentAsync(sessionId)).ThrowsAsync(new Exception("Test"));

            var result = await _controller.PaymentSuccess(sessionId);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task PaymentCancel_RedirectsToFrontendCancelPage()
        {
            var result = await _controller.PaymentCancel();

            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Contains("/payment/cancelled", redirectResult.Url);
        }
    }
}