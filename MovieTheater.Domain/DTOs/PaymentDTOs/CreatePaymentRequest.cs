namespace MovieTheater.Domain.DTOs.PaymentDTOs
{
    public class CreatePaymentRequest
    {
        public Guid InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string Provider { get; set; }
        public string SessionId { get; set; } // Add this to store Stripe session ID
    }

    public class CreateCheckoutRequest
    {
        public Guid InvoiceId { get; set; }
    }
}
