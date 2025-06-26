namespace MovieTheater.Domain.DTOs.BookingDTOs
{
    public class ReservationResult
    {
        public bool Success { get; set; }
        public string ReservationCode { get; set; }
        public DateTime ExpiryTime { get; set; }
        public List<Guid> UnavailableSeats { get; set; } = new List<Guid>();
    }
}
