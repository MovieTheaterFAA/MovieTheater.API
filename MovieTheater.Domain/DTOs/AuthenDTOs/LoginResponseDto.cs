using MovieTheater.Domain.DTOs.UserDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.DTOs.AuthenDTOs
{
    // Trả về JWT Token sau khi login
    public class LoginResponseDto
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }

        public UserDto? User { get; set; }
    }
}
