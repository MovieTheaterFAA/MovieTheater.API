namespace MovieTheater.Domain.Enums
{
    public enum RoleType
    {
        Customer,   // Người dùng chưa đăng ký tài khoản (Browse movies, view showtimes, see promotions/pricing)
        Member,     // User sau khi đăng ký tài khoản (Book tickets, view booking history, manage profile)
        Employee,   // search/manage members, sell tickets, handle bookings, and manage promotions.
        Admin       // Quản lý (Manage movies, showtimes, cinema rooms, and bookings)
    }
}
