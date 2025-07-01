namespace MovieTheater.Domain.DTOs.PaymentDTOs
{
    public class PaymentStatusResponse
    {
        public Guid InvoiceId { get; set; }
        public string Status { get; set; }
        public DateTime? PaymentDate { get; set; }
    }
}
