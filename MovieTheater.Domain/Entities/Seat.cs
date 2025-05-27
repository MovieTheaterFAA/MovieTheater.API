using MovieTheater.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.Entities
{
    public class Seat : BaseEntity
    {
        public Guid CinemaRoomId { get; set; }

        [ForeignKey(nameof(CinemaRoomId))]
        public CinemaRoom CinemaRoom { get; set; }

        public string SeatNumber { get; set; }

        public SeatType Type { get; set; }

        public SeatStatus Status { get; set; }

        // Navigation
        public ICollection<BookingSeat> BookingSeats { get; set; }
        public ICollection<TicketSeat> TicketSeats { get; set; }
    }
}
