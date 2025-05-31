using MovieTheater.Domain.Enums;

namespace MovieTheater.Domain.DTOs.AuthenDTOs
{
    public class ResendOtpRequestDto
    {
        public string Email { get; set; }
        public OtpPurpose Purpose { get; set; }
    }
}