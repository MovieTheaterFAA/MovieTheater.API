using Microsoft.Extensions.Options;
using MovieTheater.API.Configuration;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Infrastructure.Interfaces;
using Stripe.Checkout;

namespace MovieTheater.Application.Services
{
    public class StripePaymentService : IPaymentService
    {
        private readonly ILoggerService _loggerService;
        private readonly StripeSettings _stripeSettings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRedisService _redisService;
        public StripePaymentService(
            IOptions<StripeSettings> stripeSettings,
            ILoggerService loggerService,
            IUnitOfWork unitOfWork,
            IRedisService redisService)
        {
            _stripeSettings = stripeSettings.Value;
            _loggerService = loggerService;
            _unitOfWork = unitOfWork;
            _redisService = redisService;
        }

        public async Task<string> CreateCheckoutSessionAsync(Guid invoiceId, decimal amount, string currency = "vnd")
        {
            try
            {
                _loggerService.Info($"Creating Stripe checkout session for invoice: {invoiceId}, amount: {amount}");

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = (long)(amount * 100), // Stripe requires amount in smallest currency unit (cents)
                                Currency = currency,
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Ticket Payment",
                                    Description = $"Payment for Invoice #{invoiceId}"
                                }
                            },
                            Quantity = 1
                        }
                    },
                    Mode = "payment",
                    SuccessUrl = "https://movietheaterfe.ae-tao-fullstack-api.site/payment/success?session_id={CHECKOUT_SESSION_ID}",
                    CancelUrl = "https://movietheaterfe.ae-tao-fullstack-api.site/payment/cancel",
                    // Add metadata to track the invoice ID
                    Metadata = new Dictionary<string, string>
                    {
                        { "invoiceId", invoiceId.ToString() }
                    }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                _loggerService.Success($"Stripe checkout session created successfully: {session.Id}");
                return session.Url;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error creating Stripe checkout session: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> VerifyPaymentAsync(string sessionId)
        {
            try
            {
                _loggerService.Info($"Verifying Stripe payment for session: {sessionId}");

                var service = new SessionService();
                var session = await service.GetAsync(sessionId);

                if (session.PaymentStatus == "paid")
                {
                    _loggerService.Success($"Payment verified successfully for session: {sessionId}");
                    return true;
                }

                _loggerService.Warn($"Payment verification failed for session: {sessionId}, status: {session.PaymentStatus}");
                return false;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error verifying Stripe payment: {ex.Message}");
                throw;
            }
        }

        public async Task<string> InitiatePaymentAsync(Guid invoiceId)
        {
            try
            {
                _loggerService.Info($"Initiating payment for invoice {invoiceId}");

                var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId, i => i.Booking);
                if (invoice == null)
                {
                    _loggerService.Warn($"Invoice {invoiceId} not found");
                    throw new KeyNotFoundException($"Invoice with ID {invoiceId} not found");
                }

                // Update invoice status to Processing
                invoice.Status = "Processing";
                await _unitOfWork.Invoices.Update(invoice);
                await _unitOfWork.SaveChangesAsync();

                // Set expiration time for payment
                var paymentExpiryTime = DateTime.UtcNow.AddMinutes(15);
                await _redisService.SetAsync(
                    $"payment:expiry:{invoiceId}",
                    paymentExpiryTime,
                    TimeSpan.FromMinutes(20));

                // Create Stripe checkout session
                var checkoutUrl = await CreateCheckoutSessionAsync(
                    invoiceId,
                    invoice.Amount
                );

                _loggerService.Success($"Payment initiated for invoice {invoiceId}");
                return checkoutUrl;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error initiating payment: {ex.Message}");
                throw;
            }
        }
    }
}