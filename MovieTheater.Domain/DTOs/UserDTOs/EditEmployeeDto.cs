using MovieTheater.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.DTOs.UserDTOs
{
    public class EditEmployeeDto
    {
        public string? FullName { get; set; }

        public string? Password { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public Gender? Sex { get; set; }

        public string? CCCD { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }
    }
}
