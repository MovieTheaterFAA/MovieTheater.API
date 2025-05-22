using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.Entities
{
    public class CinemaRoom : BaseEntity
    {
        public string Name { get; set; }

        public int SeatQuantity { get; set; }

        // Navigation
        public ICollection<Seat> Seats { get; set; }
        public ICollection<ShowTime> Showtimes { get; set; }
    }
}
