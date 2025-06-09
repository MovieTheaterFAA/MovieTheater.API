using System.ComponentModel.DataAnnotations.Schema;

namespace MovieTheater.Domain.Entities
{
    public class Booking : BaseEntity
    {
        public Guid MemberId { get; set; }

        [ForeignKey(nameof(MemberId))]
        public User Member { get; set; }

        public Guid ShowtimeId { get; set; }

        [ForeignKey(nameof(ShowtimeId))]
        public ShowTime Showtime { get; set; }

        public DateTime BookingDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; }

        // Navigation
        public ICollection<BookingSeat> BookingSeats { get; set; }

        public ICollection<Ticket> Tickets { get; set; }
        public Invoice Invoice { get; set; }
        public ICollection<ScoreHistory> ScoreHistories { get; set; }
        public ICollection<BookingFood> BookingFoods { get; set; }
    }
}