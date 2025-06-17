using MovieTheater.Domain.Enums;

namespace MovieTheater.Domain.DTOs.SeatDTOs
{
    public class SeatResponseDto
    {
        public string Row { get; set; }
        public int Number { get; set; }
        public SeatType Type { get; set; }
    }
}
