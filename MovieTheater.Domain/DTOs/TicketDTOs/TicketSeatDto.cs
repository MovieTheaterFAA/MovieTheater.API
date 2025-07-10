namespace MovieTheater.Domain.DTOs.TicketDTOs
{
    public class TicketSeatDto
    {
        public Guid SeatId { get; set; }
        public string Row { get; set; }
        public int Number { get; set; }
    }
}
