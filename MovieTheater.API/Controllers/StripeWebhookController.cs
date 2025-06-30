using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MovieTheater.API.Configuration;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using Stripe;
using Stripe.Checkout;
using System.Text.Json;

namespace MovieTheater.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILoggerService _loggerService;
        private readonly string _endpointSecret;

        public StripeWebhookController(
            IPaymentService paymentService,
            ILoggerService loggerService,
            IOptions<StripeSettings> stripeSettings)
        {
            _paymentService = paymentService;
            _loggerService = loggerService;
            _endpointSecret = stripeSettings.Value.WebhookSecret;

            if (string.IsNullOrEmpty(_endpointSecret))
            {
                _loggerService.Warn("Stripe webhook secret is missing in configuration!");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Index()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            // Log incoming webhook data for debugging
            _loggerService.Info($"Received Stripe webhook data: {json.Substring(0, Math.Min(100, json.Length))}...");

            try
            {
                // If webhook secret is missing, log warning and try to process without verification
                if (string.IsNullOrEmpty(_endpointSecret))
                {
                    _loggerService.Warn("Processing webhook without signature verification - NOT RECOMMENDED FOR PRODUCTION");
                    var jsonEvent = JsonSerializer.Deserialize<JsonElement>(json);
                    var eventType = jsonEvent.GetProperty("type").GetString();
                    _loggerService.Info($"Webhook event type (unverified): {eventType}");

                    // Continue with basic processing...
                    return Ok();
                }

                // Verify webhook signature
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _endpointSecret
                );

                _loggerService.Info($"Received Stripe webhook: {stripeEvent.Type}");

                // Handle the event based on its type
                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session != null)
                    {
                        if (session.Metadata != null &&
                            session.Metadata.TryGetValue("invoiceId", out string? invoiceIdStr) &&
                            !string.IsNullOrEmpty(invoiceIdStr) &&
                            Guid.TryParse(invoiceIdStr, out Guid invoiceId))
                        {
                            await _paymentService.VerifyPaymentAsync(session.Id);
                            _loggerService.Success($"Payment processed for invoice {invoiceId}, session {session.Id}");
                        }
                        else
                        {
                            _loggerService.Warn($"Could not extract invoice ID from session metadata: {JsonSerializer.Serialize(session.Metadata)}");
                        }
                    }
                }
                else if (stripeEvent.Type == "checkout.session.expired")
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session != null)
                    {
                        _loggerService.Warn($"Payment session expired: {session.Id}");
                    }
                }

                return Ok();
            }
            catch (StripeException e)
            {
                _loggerService.Error($"Stripe webhook error: {e.Message}");
                return BadRequest(new { Error = e.Message });
            }
            catch (Exception ex)
            {
                _loggerService.Error($"General error processing webhook: {ex.Message}");
                return StatusCode(500, new { Error = "Internal server error processing webhook" });
            }
        }
    }
}