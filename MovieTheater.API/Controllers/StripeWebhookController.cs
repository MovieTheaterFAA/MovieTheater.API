using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MovieTheater.API.Configuration;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.PaymentDTOs;
using Stripe;
using Stripe.Checkout;
using System.Text.Json;

namespace MovieTheater.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ILoggerService _loggerService;
        private readonly StripeSettings _stripeSettings;
        private readonly string _endpointSecret;

        public StripeWebhookController(
            IInvoiceService invoiceService,
            ILoggerService loggerService,
            IOptions<StripeSettings> stripeSettings)
        {
            _invoiceService = invoiceService;
            _loggerService = loggerService;
            _stripeSettings = stripeSettings.Value;
            _endpointSecret = _stripeSettings.WebhookSecret;
        }

        [HttpPost]
        public async Task<IActionResult> Index()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _endpointSecret
                );

                _loggerService.Info($"Received Stripe webhook: {stripeEvent.Type}");

                // Handle the event based on its type
                if (stripeEvent.Type == "checkout.session.expired")
                {
                    var session = stripeEvent.Data.Object as Session;

                    // Extract the invoice ID from the metadata
                    if (session.Metadata.TryGetValue("invoiceId", out string invoiceIdStr) &&
                        Guid.TryParse(invoiceIdStr, out Guid invoiceId))
                    {
                        var paymentRequest = new CreatePaymentRequest
                        {
                            InvoiceId = invoiceId,
                            Amount = session.AmountTotal.Value / 100m, // Convert from cents
                            Provider = "Stripe",
                            SessionId = session.Id
                        };

                        await _invoiceService.ProcessPaymentAsync(paymentRequest);
                        _loggerService.Success($"Payment processed for invoice {invoiceId}, session {session.Id}");
                    }
                    else
                    {
                        _loggerService.Warn($"Could not extract invoice ID from session metadata: {JsonSerializer.Serialize(session.Metadata)}");
                    }
                }
                else if (stripeEvent.Type == "checkout.session.expired")
                {
                    var session = stripeEvent.Data.Object as Session;
                    _loggerService.Warn($"Payment session expired: {session.Id}");

                    // You might want to update the invoice status or notify the user
                    if (session.Metadata.TryGetValue("invoiceId", out string invoiceIdStr) &&
                        Guid.TryParse(invoiceIdStr, out Guid invoiceId))
                    {
                        await _invoiceService.UpdateInvoiceStatusAsync(invoiceId, "PaymentFailed");
                    }
                }

                return Ok();
            }
            catch (StripeException e)
            {
                _loggerService.Error($"Stripe webhook error: {e.Message}");
                return BadRequest();
            }
        }
    }
}