using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
