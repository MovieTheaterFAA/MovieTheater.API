using MovieTheater.Domain.Enums;

namespace MovieTheater.Domain.DTOs.BookingDTOs
{
    public class BookingResult
    {
        public Guid BookingId { get; set; }
        public Guid InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public BookingStatus Status { get; set; }
    }
}
