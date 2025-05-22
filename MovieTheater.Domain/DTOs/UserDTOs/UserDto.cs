using MovieTheater.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.DTOs.UserDTOs
{
    public class UserDto
    {
        public Guid UserId { get; set; }
        public string? FullName { get; set; }

        public string? Password { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public Gender Sex { get; set; }

        public string? Email { get; set; }

        public string? CCCD { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public RoleType Role { get; set; }

        public int ScoreBalance { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
