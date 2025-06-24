using MovieTheater.Domain.Enums;

namespace MovieTheater.Domain.DTOs.SeatDTOs
{
    public class SeatDto
    {
        public Guid Id { get; set; }
        public string Row { get; set; }
        public int Number { get; set; }
        public SeatType Type { get; set; }
        public Guid CinemaRoomId { get; set; }
    }

    public class CreateSeatDto
    {
        public string Row { get; set; }
        public int Number { get; set; }
        public SeatType Type { get; set; }
    }

    public class BatchCreateSeatDto
    {
        public List<CreateSeatDto> Seats { get; set; }
    }

    public class UpdateSeatDto
    {
        public string Row { get; set; }
        public int Number { get; set; }
        public SeatType Type { get; set; }
    }
}
