namespace MovieTheater.Domain.DTOs.MovieDTOs
{
    public class MovieRequestDto
    {
        public string Name { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Director { get; set; }
        public int? RunningTime { get; set; }
        public string TrailerUrl { get; set; }
        public List<string> Genres { get; set; }
        public string Description { get; set; }
        public float Rating { get; set; }
    }
}
