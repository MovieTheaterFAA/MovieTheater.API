using System.ComponentModel.DataAnnotations.Schema;

namespace MovieTheater.Domain.Entities
{
    public class BookingSeat : BaseEntity
    {
        public Guid BookingId { get; set; }

        [ForeignKey(nameof(BookingId))]
        public Booking Booking { get; set; }

        public Guid SeatId { get; set; }

        [ForeignKey(nameof(SeatId))]
        public Seat Seat { get; set; }
    }
}
