using MovieTheater.Domain.DTOs.BookingDTOs;

namespace MovieTheater.Domain.DTOs.InvoiceDTOs
{
    public class InvoiceDto
    {
        public Guid Id { get; set; }
        public Guid BookingId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string? promotion { get; set; }
        public BookingSummaryDto Booking { get; set; }
    }
}
