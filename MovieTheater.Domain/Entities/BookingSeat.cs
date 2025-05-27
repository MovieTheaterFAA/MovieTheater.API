using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
