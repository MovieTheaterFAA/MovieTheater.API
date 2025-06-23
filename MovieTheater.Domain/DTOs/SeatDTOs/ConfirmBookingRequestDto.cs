namespace MovieTheater.Domain.DTOs.SeatDTOs
{
    public class ConfirmBookingRequestDto
    {
        public Guid ShowTimeId { get; set; }
        public List<Guid> SeatIds { get; set; }
    }
}
