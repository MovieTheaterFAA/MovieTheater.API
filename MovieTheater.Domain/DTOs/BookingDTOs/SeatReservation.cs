namespace MovieTheater.Domain.DTOs.BookingDTOs
{
    public class SeatReservation
    {
        public string ReservationCode { get; set; }
        public Guid ShowTimeId { get; set; }
        public List<Guid> SeatIds { get; set; }
        public DateTime ExpiryTime { get; set; }
    }
}
