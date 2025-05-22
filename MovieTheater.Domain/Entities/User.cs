using MovieTheater.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace MovieTheater.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Account { get; set; }

        public string Password { get; set; }

        public string FullName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public Gender Sex { get; set; }

        public string Email { get; set; }

        public string CCCD { get; set; }

        public string Phone { get; set; }

        public string Address { get; set; }

        public RoleType Role { get; set; }

        public bool IsLocked { get; set; }

        public int ScoreBalance { get; set; }

        // Navigation
        public ICollection<Booking> Bookings { get; set; }
        public ICollection<ScoreHistory> ScoreHistory { get; set; }
    }
}
