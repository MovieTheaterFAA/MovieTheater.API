namespace MovieTheater.Domain.DTOs.BookingDTOs
{
    public class BookingSeatDto
    {
        public Guid SeatId { get; set; }
        public string Row { get; set; }
        public int Number { get; set; }
    }
}
