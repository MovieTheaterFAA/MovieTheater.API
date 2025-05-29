using MovieTheater.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.DTOs.UserDTOs
{
    public class UserForListDto
    {
        public Guid Id { get; set; }
        public string? FullName { get; set; }

        public Gender Sex { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public RoleType Role { get; set; }

        public int ScoreBalance { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsDeleted { get; set; }

    }
}
