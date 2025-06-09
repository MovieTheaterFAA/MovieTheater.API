namespace MovieTheater.Domain.Enums
{
    public enum RoleType
    {
        Customer = 0,   // Người dùng chưa đăng ký tài khoản (no authen)
        Member = 1,     // Customer đã verify otp (Book tickets, view booking history, manage profile)
        Employee = 2,   // Handle offline booking
        Admin = 3,       // Quản lý (Manage members, employees, movies, showtimes, cinema rooms, and bookings)
    }
}