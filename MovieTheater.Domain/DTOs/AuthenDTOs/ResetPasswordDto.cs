namespace MovieTheater.Domain.DTOs.AuthenDTOs
{
    public class ResetPasswordDto
    {
        public string Email { get; set; }
        public string NewPassword { get; set; }
        public string Otp { get; set; }
    }
}