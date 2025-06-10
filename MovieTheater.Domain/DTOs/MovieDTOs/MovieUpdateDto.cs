namespace MovieTheater.Domain.DTOs.MovieDTOs
{
    public class MovieUpdateDto
    {
        public string? Name { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public List<string>? Actors { get; set; }      // Danh sách dàn cast
        public List<string>? ActorsUrl { get; set; }   // Đường dẫn tới ảnh của diễn viên
        public string? Director { get; set; }

        public int? RunningTime { get; set; }         // Thời lượng chiếu

        public string? TrailerUrl { get; set; }

        public List<string>? Genres { get; set; }      // Thể loại phim - MovieType

        public string? Description { get; set; }

        public string? PosterImage { get; set; }
    }
}
