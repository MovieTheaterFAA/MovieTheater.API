namespace MovieTheater.Domain.DTOs.TicketDTOs
{
    // Class to deserialize the QR code payload
    public class QrCodePayload
    {
        public Guid TicketId { get; set; }
        public string Hash { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
