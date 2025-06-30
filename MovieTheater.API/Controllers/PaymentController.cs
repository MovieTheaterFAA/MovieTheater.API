using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.PaymentDTOs;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IInvoiceService _invoiceService;
        private readonly ILoggerService _loggerService;
        private readonly IClaimsService _claimsService;

        public PaymentController(
            IPaymentService paymentService,
            IInvoiceService invoiceService,
            ILoggerService loggerService,
            IClaimsService claimsService)
        {
            _paymentService = paymentService;
            _invoiceService = invoiceService;
            _loggerService = loggerService;
            _claimsService = claimsService;
        }

        [HttpPost("create-checkout-session")]
        [Authorize]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutRequest request)
        {
            try
            {
                var sessionUrl = await _paymentService.InitiatePaymentAsync(request.InvoiceId);

                return Ok(new { url = sessionUrl });
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error creating checkout session: {ex.Message}");
                return StatusCode(500, "An error occurred while creating the checkout session");
            }
        }

        [HttpGet("success")]
        public async Task<IActionResult> PaymentSuccess([FromQuery] string session_id)
        {
            try
            {
                var isValid = await _paymentService.VerifyPaymentAsync(session_id);

                if (isValid)
                {
                    return Redirect($"https://movietheaterfe.ae-tao-fullstack-api.site/payment/thankyou?success=true&session_id={session_id}");
                }

                return Redirect("https://movietheaterfe.ae-tao-fullstack-api.site/payment/thankyou?success=false");
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error processing payment success: {ex.Message}");
                return Redirect("https://movietheaterfe.ae-tao-fullstack-api.site/payment/error");
            }
        }

        [HttpGet("cancel")]
        public IActionResult PaymentCancel()
        {
            // Redirect to a cancellation page on the frontend
            return Redirect("https://movietheaterfe.ae-tao-fullstack-api.site/payment/cancelled");
        }
    }
}