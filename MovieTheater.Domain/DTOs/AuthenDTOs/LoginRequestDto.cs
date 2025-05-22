using System.ComponentModel;


namespace MovieTheater.Domain.DTOs.AuthenDTOs
{
    public class LoginRequestDto
    {
        [DefaultValue("ch1mple@gmail.com")]
        public required string? Email { get; set; }

        [DefaultValue("Ch1mple@")] 
        public required string? Password { get; set; }
    }
}
