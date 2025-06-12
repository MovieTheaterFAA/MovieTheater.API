namespace MovieTheater.Domain.DTOs.SeatDTOs
{
    public class HoldSeatsRequestDto
    {
        public Guid ShowTimeId { get; set; }
        public List<Guid> SeatIds { get; set; }
    }
}
