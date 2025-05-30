namespace MovieTheater.Domain.DTOs.EmailDTOs
{
    public class EmployeeCredentialsEmailDto
    {

        // Địa chỉ email người nhận
        public string To { get; set; }

        // Tên đăng nhập của nhân viên
        public string UserName { get; set; }

        // Mật khẩu tạm thời hoặc mật khẩu gửi cho nhân viên
        public string Password { get; set; }
    }
}
