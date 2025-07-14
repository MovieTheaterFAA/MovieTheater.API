using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.PaymentDTOs;

namespace MovieTheater.API.Controllers
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILoggerService _loggerService;
        private readonly string _frontendBaseUrl;

        public PaymentController(
            IPaymentService paymentService,
            ILoggerService loggerService,
            IConfiguration configuration)
        {
            _paymentService = paymentService;
            _loggerService = loggerService;
            _frontendBaseUrl =
                "https://movietheaterfe.ae-tao-fullstack-api.site";
        }

        /// <summary>
        /// Creates a checkout session for payment
        /// </summary>
        /// <param name="request">The checkout request containing invoice ID</param>
        /// <returns>The checkout session URL</returns>
        [HttpPost("create-checkout-session")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResult<CheckoutSessionResponse>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 404)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutRequest request)
        {
            try
            {
                _loggerService.Info($"Creating checkout session for invoice: {request.InvoiceId}");

                if (request.InvoiceId == Guid.Empty)
                {
                    _loggerService.Warn("Invalid invoice ID provided");
                    return BadRequest(ApiResult<object>.Failure("400", "Invalid invoice ID"));
                }

                var sessionUrl = await _paymentService.InitiatePaymentAsync(request.InvoiceId);

                _loggerService.Success($"Checkout session created successfully for invoice: {request.InvoiceId}");
                return Ok(ApiResult<CheckoutSessionResponse>.Success(
                    new CheckoutSessionResponse { Url = sessionUrl },
                    "200",
                    "Checkout session created successfully"
                ));
            }
            catch (KeyNotFoundException ex)
            {
                _loggerService.Warn($"Not found: {ex.Message}");
                return NotFound(ApiResult<object>.Failure("404", ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _loggerService.Warn($"Invalid operation: {ex.Message}");
                return BadRequest(ApiResult<object>.Failure("400", ex.Message));
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error creating checkout session: {ex.Message}");
                return StatusCode(500, ApiResult<object>.Failure("500", "An error occurred while creating the checkout session"));
            }
        }

        /// <summary>
        /// Handles payment success callback from Stripe
        /// </summary>
        /// <param name="session_id">The Stripe session ID</param>
        /// <returns>Redirect to the appropriate frontend page</returns>
        /// <summary>
        /// Handles payment success callback from Stripe
        /// </summary>
        /// <param name="session_id">The Stripe session ID</param>
        /// <returns>API result indicating payment verification status</returns>
        [HttpGet("success")]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> PaymentSuccess([FromQuery] string session_id)
        {
            try
            {
                _loggerService.Info($"Processing payment success for session: {session_id}");

                if (string.IsNullOrEmpty(session_id))
                {
                    _loggerService.Warn("No session ID provided");
                    return BadRequest(ApiResult<object>.Failure("400", "Missing session_id parameter."));
                }

                var isValid = await _paymentService.VerifyPaymentAsync(session_id);

                if (isValid)
                {
                    _loggerService.Success($"Payment verified successfully for session: {session_id}");
                    return Ok(ApiResult<object>.Success(
                        new { session_id },
                        "200",
                        "Payment verified successfully."
                    ));
                }

                _loggerService.Warn($"Payment verification failed for session: {session_id}");
                return BadRequest(ApiResult<object>.Failure("400", "Payment verification failed."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        /// <summary>
        /// Handles payment cancellation from Stripe
        /// </summary>
        /// <returns>Redirect to the cancellation page</returns>
        [HttpGet("cancel")]
        public async Task<IActionResult> PaymentCancel()
        {
            await Task.Yield();
            _loggerService.Info("Payment cancelled by user");
            return Redirect($"{_frontendBaseUrl}/payment/cancelled");
        }
    }
}