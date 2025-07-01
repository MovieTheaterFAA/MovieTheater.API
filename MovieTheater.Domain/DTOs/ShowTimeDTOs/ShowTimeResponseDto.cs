namespace MovieTheater.Domain.DTOs.ShowTimeDTOs
{
    public class ShowtimeResponseDTO
    {
        public Guid Id { get; set; }
        public Guid MovieId { get; set; }
        public Guid CinemaRoomId { get; set; }
        public DateTime ShowDate { get; set; }
        public TimeSpan Duration { get; set; }

        // Expose only the date part
        public DateOnly Date => DateOnly.FromDateTime(ShowDate);

        // Expose only the time part
        public TimeSpan StartTime => ShowDate.TimeOfDay;
    }
}