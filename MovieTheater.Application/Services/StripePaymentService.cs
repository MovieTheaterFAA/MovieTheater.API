using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.Entities;
using MovieTheater.Infrastructure.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace MovieTheater.Application.Services
{
    public class StripePaymentService : IPaymentService
    {
        private readonly ILoggerService _loggerService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRedisService _redisService;
        private readonly IStripeClient _stripeClient;
        private readonly string _baseUrl = "https://movietheaterfe.ae-tao-fullstack-api.site";

        public StripePaymentService(
            ILoggerService loggerService,
            IUnitOfWork unitOfWork,
            IRedisService redisService,
            IStripeClient stripeClient)
        {
            _loggerService = loggerService;
            _unitOfWork = unitOfWork;
            _redisService = redisService;
            _stripeClient = stripeClient;

            // Log Stripe client initialization
            _loggerService.Info("Stripe payment service initialized");
        }

        public async Task<string> CreateCheckoutSessionAsync(Guid invoiceId, decimal amount, string currency = "vnd")
        {
            try
            {
                _loggerService.Info($"Creating Stripe checkout session for invoice: {invoiceId}, amount: {amount}");

                // Validate amount
                if (amount <= 0)
                {
                    throw new ArgumentException("Payment amount must be greater than zero");
                }

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = (long)(amount * 100), // Stripe requires amount in smallest currency unit
                                Currency = currency,
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Movie Theater Tickets",
                                    Description = $"Payment for Invoice #{invoiceId}"
                                }
                            },
                            Quantity = 1
                        }
                    },
                    Mode = "payment",
                    SuccessUrl = $"{_baseUrl}/payment/success?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{_baseUrl}/payment/cancel",
                    Metadata = new Dictionary<string, string>
                    {
                        { "invoiceId", invoiceId.ToString() }
                    }
                };

                var service = new SessionService(_stripeClient);
                var session = await service.CreateAsync(options);

                // Cache session ID with invoice ID for later reference
                await _redisService.SetAsync(
                    $"stripe:session:{session.Id}",
                    invoiceId.ToString(),
                    TimeSpan.FromHours(24));

                _loggerService.Success($"Stripe checkout session created successfully: {session.Id}");
                return session.Url;
            }
            catch (StripeException ex)
            {
                _loggerService.Error($"Stripe API error: {ex.StripeError?.Message ?? ex.Message}");
                throw;
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

                if (string.IsNullOrEmpty(sessionId))
                {
                    _loggerService.Warn("Attempted to verify payment with null or empty session ID");
                    return false;
                }

                var service = new SessionService(_stripeClient);
                var session = await service.GetAsync(sessionId);

                if (session.PaymentStatus == "paid")
                {
                    _loggerService.Success($"Payment verified successfully for session: {sessionId}");

                    // If invoice ID is in metadata, process payment
                    if (session.Metadata.TryGetValue("invoiceId", out string invoiceIdStr) &&
                        Guid.TryParse(invoiceIdStr, out Guid invoiceId))
                    {
                        await ProcessSuccessfulPaymentAsync(invoiceId, session);
                    }
                    else
                    {
                        // Also try to get from Redis if not in metadata
                        var cachedInvoiceId = await _redisService.GetAsync<string>($"stripe:session:{sessionId}");
                        if (!string.IsNullOrEmpty(cachedInvoiceId) && Guid.TryParse(cachedInvoiceId, out invoiceId))
                        {
                            await ProcessSuccessfulPaymentAsync(invoiceId, session);
                        }
                    }

                    return true;
                }

                _loggerService.Warn($"Payment verification failed for session: {sessionId}, status: {session.PaymentStatus}");
                return false;
            }
            catch (StripeException ex)
            {
                _loggerService.Error($"Stripe API error during verification: {ex.StripeError?.Message ?? ex.Message}");
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

                // Check if invoice is already paid
                if (invoice.Status == "Paid")
                {
                    _loggerService.Warn($"Invoice {invoiceId} is already paid");
                    throw new InvalidOperationException($"Invoice {invoiceId} is already paid");
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

        private async Task ProcessSuccessfulPaymentAsync(Guid invoiceId, Session session)
        {
            try
            {
                _loggerService.Info($"Processing successful payment for invoice {invoiceId}");

                var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId);
                if (invoice == null)
                {
                    _loggerService.Warn($"Invoice {invoiceId} not found during payment completion");
                    return;
                }

                // Check if payment has already been processed
                var existingPayment = await _unitOfWork.Payments.FirstOrDefaultAsync(
                    p => p.InvoiceId == invoiceId && p.PaymentReference == session.Id);

                if (existingPayment != null)
                {
                    _loggerService.Info($"Payment for session {session.Id} already processed");
                    return;
                }

                // Update invoice status
                invoice.Status = "Paid";
                await _unitOfWork.Invoices.Update(invoice);

                // Create payment record
                var payment = new Payment
                {
                    InvoiceId = invoiceId,
                    PaymentDate = DateTime.UtcNow,
                    Amount = session.AmountTotal.Value / 100m, // Convert from cents
                    Provider = "Stripe",
                    PaymentReference = session.Id,
                    Status = "Completed"
                };

                await _unitOfWork.Payments.AddAsync(payment);
                await _unitOfWork.SaveChangesAsync();

                // Clear payment cache
                await _redisService.RemoveAsync($"payment:expiry:{invoiceId}");

                _loggerService.Success($"Payment successfully processed for invoice {invoiceId}");
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error processing successful payment: {ex.Message}");
                throw;
            }
        }
    }
}