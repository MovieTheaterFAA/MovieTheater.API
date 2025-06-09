namespace MovieTheater.Domain.Entities
{
    public class CinemaRoom : BaseEntity
    {
        public string Name { get; set; }

        public int SeatQuantity { get; set; }

        // Navigation
        public ICollection<Seat> Seats { get; set; }
        public ICollection<ShowTime> Showtimes { get; set; }
    }
}
