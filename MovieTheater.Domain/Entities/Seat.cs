using MovieTheater.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace MovieTheater.Domain.Entities
{
    public class Seat : BaseEntity
    {
        public Guid CinemaRoomId { get; set; }

        [ForeignKey(nameof(CinemaRoomId))]
        public CinemaRoom CinemaRoom { get; set; }
        public string Row { get; set; }          // E.g. "A", "B"
        public int Number { get; set; }          // E.g. 1, 2, 3
        public SeatType Type { get; set; }
        public SeatStatus Status { get; set; }

        // Navigation
        public ICollection<BookingSeat> BookingSeats { get; set; }
        public ICollection<TicketSeat> TicketSeats { get; set; }
    }
}
