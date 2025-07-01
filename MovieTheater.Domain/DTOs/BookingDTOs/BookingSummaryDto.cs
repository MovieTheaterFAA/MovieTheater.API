namespace MovieTheater.Domain.DTOs.BookingDTOs
{
    public class BookingSummaryDto
    {
        public Guid Id { get; set; }
        public string MemberName { get; set; }
        public string MovieTitle { get; set; }
        public DateTime ShowDate { get; set; }
        public int SeatCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
