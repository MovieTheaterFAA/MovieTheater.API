using MovieTheater.Domain.Enums;

namespace MovieTheater.Domain.DTOs.UserDTOs
{
    public class CurrentUserDto
    {
        public string FullName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public Gender Sex { get; set; }

        public string Email { get; set; }

        public string? CCCD { get; set; }

        public string PhoneNumber { get; set; }

        public string? Address { get; set; }

        public RoleType Role { get; set; }

        public int ScoreBalance { get; set; }


        public string? AvatarUrl { get; set; }
    }
}
