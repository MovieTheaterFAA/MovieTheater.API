using System.ComponentModel.DataAnnotations.Schema;

namespace MovieTheater.Domain.Entities
{
    public class Ticket : BaseEntity
    {
        public Guid BookingId { get; set; }

        [ForeignKey(nameof(BookingId))]
        public Booking Booking { get; set; }

        public DateTime IssuedAt { get; set; }

        public decimal TotalPrice { get; set; }

        // Navigation
        public ICollection<TicketSeat> TicketSeats { get; set; }
        public ICollection<TicketFoodAndDrink> TicketFoodAndDrinks { get; set; }
    }
}
