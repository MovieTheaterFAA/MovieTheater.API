using MovieTheater.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.DTOs.UserDTOs
{
    public class CurrentUserDto
    {
        public string Account { get; set; }

        public string Password { get; set; }

        public string FullName { get; set; }



        public string Email { get; set; }

        public RoleType Role { get; set; }

        public int ScoreBalance { get; set; }
    }
}
