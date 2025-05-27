namespace MovieTheater.Domain.DTOs.AuthenDTOs
{
    public class VerifyOtpDto
    {
        public string Email { get; set; }
        public string Otp { get; set; }
    }

    public class ResendOtpDto
    {
        public string Email { get; set; }
    }
}
