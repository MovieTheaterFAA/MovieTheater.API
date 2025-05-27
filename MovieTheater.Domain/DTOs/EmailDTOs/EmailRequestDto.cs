namespace MovieTheater.Domain.DTOs.EmailDTOs
{
    public class EmailRequestDto
    {
        // Địa chỉ email người nhận
        public string To { get; set; }

        // Tên người dùng (dùng cho email đăng ký thành công)
        public string? UserName { get; set; }

        // Mã OTP (dùng cho xác thực OTP và quên mật khẩu)
        public string? Otp { get; set; }
    }
}
