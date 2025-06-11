namespace MovieTheater.Domain.DTOs.EventDTOs
{
    public class EventResponseDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string Detail { get; set; }

        public string Image { get; set; }
    }
}