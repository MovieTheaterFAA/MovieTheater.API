namespace MovieTheater.Domain.DTOs.MovieDTOs
{
    public class MovieResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<string> Actors { get; set; }
        public List<string> ActorsUrl { get; set; }
        public string Director { get; set; }
        public int? RunningTime { get; set; }
        public string TrailerUrl { get; set; }
        public List<string> Genres { get; set; }
        public string Description { get; set; }
        public string PosterImage { get; set; }
        public string BackgroundImage { get; set; }
    }
}