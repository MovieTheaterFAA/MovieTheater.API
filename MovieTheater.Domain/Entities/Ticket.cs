using MovieTheater.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace MovieTheater.Domain.Entities
{
    public class Ticket : BaseEntity
    {
        public Guid BookingId { get; set; }

        [ForeignKey(nameof(BookingId))]
        public Booking Booking { get; set; }
        public DateTime IssuedAt { get; set; }
        public string GuestPhoneNumber { get; set; } // Optional for offline tickets
        public decimal TotalPrice { get; set; }
        public TicketType TicketType { get; set; } // Enum for Online or Offline ticket

        // Navigation
        public ICollection<TicketSeat> TicketSeats { get; set; }
        public ICollection<TicketFoodAndDrink> TicketFoodAndDrinks { get; set; }
    }
}
