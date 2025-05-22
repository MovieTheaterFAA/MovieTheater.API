using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.Entities
{
    public class TicketSeat : BaseEntity
    {
        public Guid TicketId { get; set; }

        [ForeignKey(nameof(TicketId))]
        public Ticket Ticket { get; set; }

        public Guid SeatId { get; set; }

        [ForeignKey(nameof(SeatId))]
        public Seat Seat { get; set; }

        public decimal PricePerSeat { get; set; }
    }
}
