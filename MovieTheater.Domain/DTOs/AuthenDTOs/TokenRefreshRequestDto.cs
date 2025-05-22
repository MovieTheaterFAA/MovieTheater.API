using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.DTOs.AuthenDTOs
{
    public class TokenRefreshRequestDto
    {
        public required string RefreshToken { get; set; }
    }
}
