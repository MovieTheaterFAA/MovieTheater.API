using MovieTheater.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace MovieTheater.Domain.Entities
{
    public class ShowTimeSeat : BaseEntity
    {
        public Guid ShowTimeId { get; set; }
        [ForeignKey(nameof(ShowTimeId))]
        public ShowTime ShowTime { get; set; }

        public Guid SeatId { get; set; }
        [ForeignKey(nameof(SeatId))]
        public Seat Seat { get; set; }

        public SeatStatus Status { get; set; } // e.g. Available, Reserved, Locked
    }

}
