namespace MovieTheater.Domain.DTOs.ShowTimeDTOs
{
    public class ShowtimeResponseDTO
    {
        public Guid Id { get; set; }
        public Guid MovieId { get; set; }
        public Guid CinemaRoomId { get; set; }
        public DateTime ShowDate { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
