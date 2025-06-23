using MovieTheater.Domain.Enums;

namespace MovieTheater.Domain.DTOs.SeatDTOs
{
    public class ShowTimeSeatDto
    {
        public Guid SeatId { get; set; }
        public string Row { get; set; }
        public int Number { get; set; }
        public SeatType Type { get; set; }
        public SeatStatus Status { get; set; }
    }
}
