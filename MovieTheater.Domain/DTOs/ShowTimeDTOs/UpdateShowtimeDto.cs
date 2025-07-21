namespace MovieTheater.Domain.DTOs.ShowTimeDTOs
{
    public class UpdateShowtimeDto
    {
        public Guid MovieId { get; set; }
        public Guid CinemaRoomId { get; set; }
        public DateTime ShowDate { get; set; }
    }
}
