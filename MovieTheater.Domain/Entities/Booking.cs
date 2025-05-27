using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
    }
}
