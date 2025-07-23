using MovieTheater.Domain.Enums;

namespace MovieTheater.Domain.Entities
{
    public class Movie : BaseEntity
    {
        public string Name { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public List<string>? Actors { get; set; }     // Danh sách dàn cast
        public List<string>? ActorsUrl { get; set; }  // Đường dẫn tới ảnh của diễn viên
        public string Director { get; set; }

        public int? RunningTime { get; set; }         // Thời lượng chiếu

        public string TrailerUrl { get; set; }

        public List<string> Genres { get; set; }      // Thể loại phim - MovieType

        public string Description { get; set; }

        public string? PosterImage { get; set; }
        public string? BackgroundImage { get; set; }
        public MovieStatus Status { get; set; }       // Trạng thái phim
        public float Rating { get; set; }             // Điểm đánh giá

        // Navigation
        public ICollection<ShowTime> Showtimes { get; set; }
    }
}