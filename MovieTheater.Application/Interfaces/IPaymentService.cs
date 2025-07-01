namespace MovieTheater.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<string> CreateCheckoutSessionAsync(Guid invoiceId);
        Task<bool> VerifyPaymentAsync(string sessionId);
        Task<string> InitiatePaymentAsync(Guid invoiceId);
        Task ProcessFailPaymentAsync(Guid invoiceId);
    }
}
