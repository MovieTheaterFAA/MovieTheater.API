namespace MovieTheater.Domain.Enums
{
    public enum RoleType
    {
        Customer,   // Người dùng chưa đăng ký tài khoản (no authen)
        Member,     // Customer đã verify otp (Book tickets, view booking history, manage profile)
        Employee,   // Handle offline booking
        Admin       // Quản lý (Manage members, employees, movies, showtimes, cinema rooms, and bookings)
    }
}