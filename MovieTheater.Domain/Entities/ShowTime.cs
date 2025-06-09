using System.ComponentModel.DataAnnotations.Schema;

namespace MovieTheater.Domain.Entities
{
    public class ShowTime : BaseEntity
    {
        public Guid MovieId { get; set; }

        [ForeignKey(nameof(MovieId))]
        public Movie Movie { get; set; }

        public Guid CinemaRoomId { get; set; }

        [ForeignKey(nameof(CinemaRoomId))]
        public CinemaRoom CinemaRoom { get; set; }

        public DateTime ShowDate { get; set; }     // Ngày công chiếu

        public TimeSpan Duration { get; set; }  

        // Navigation
        public ICollection<Booking> Bookings { get; set; }
    }
}
