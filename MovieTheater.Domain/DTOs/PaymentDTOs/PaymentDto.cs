namespace MovieTheater.Domain.DTOs.PaymentDTOs
{
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string Provider { get; set; }
        public string PaymentReference { get; set; }
        public string Status { get; set; }
    }
}
