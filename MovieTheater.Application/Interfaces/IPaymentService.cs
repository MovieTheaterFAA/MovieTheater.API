namespace MovieTheater.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<string> CreateCheckoutSessionAsync(Guid invoiceId, decimal amount, string currency = "vnd");
        Task<bool> VerifyPaymentAsync(string sessionId);
        Task<string> InitiatePaymentAsync(Guid invoiceId);
    }
}
