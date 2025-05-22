using MovieTheater.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace MovieTheater.Domain.Entities
{
    public class User : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [MaxLength(255)]
        public string Password { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public Gender Sex { get; set; }

        public string Email { get; set; }

        // Căn cước công dân
        public string? CCCD { get; set; }

        [MaxLength(15)]
        public string PhoneNumber { get; set; }

        public string? Address { get; set; }

        public RoleType Role { get; set; }

        // Score của user để nhận khuyến mãi
        public int ScoreBalance { get; set; }

        // JWT Token
        [MaxLength(128)] public string? RefreshToken { get; set; }
        [MaxLength(128)] public DateTime? RefreshTokenExpiryTime { get; set; }

        // Status check email đã được verify hay chưa
        public bool IsEmailVerified { get; set; }

        public UserStatus UserStatus { get; set; }


        // Navigation
        public ICollection<Booking> Bookings { get; set; }
        public ICollection<ScoreHistory> ScoreHistory { get; set; }
    }
}
