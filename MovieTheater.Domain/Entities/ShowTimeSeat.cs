using MovieTheater.Domain.Enums;

namespace MovieTheater.Domain.Entities
{
    public class ShowTimeSeat : BaseEntity
    {
        public Guid ShowTimeId { get; set; }
        public ShowTime ShowTime { get; set; }

        public Guid SeatId { get; set; }
        public Seat Seat { get; set; }

        public SeatStatus Status { get; set; } // e.g. Available, Reserved, Locked
    }

}
