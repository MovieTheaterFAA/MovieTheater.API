using MovieTheater.Domain.Enums;

namespace MovieTheater.Domain.DTOs.EmailDTOs
{
    public class UpdateEmployeeCredentialsEmailDto
    {
        public string To { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public Gender? Sex { get; set; }
        public string CCCD { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
    }
}
