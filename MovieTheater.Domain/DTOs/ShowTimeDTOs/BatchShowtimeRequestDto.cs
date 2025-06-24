namespace MovieTheater.Domain.DTOs.ShowTimeDTOs
{
    public class BatchShowtimeRequestDto
    {
        public class BatchShowTimeRequestDto
        {
            public Guid CinemaRoomId { get; set; }
            public DateTime ShowDate { get; set; }
            public List<SingleShowTimeDto> ShowTimes { get; set; } = new();
        }

        public class SingleShowTimeDto
        {
            public Guid MovieId { get; set; }
            public DateTime StartTime { get; set; }
        }
    }
}