namespace MovieTheater.Domain.Enums
{
    public enum MovieStatus
    {
        ComingSoon = 0, // Phim sắp chiếu
        NowShowing = 1, // Phim đang chiếu
        Ended = 2,      // Phim đã kết thúc chiếu
        Cancelled = 3   // Phim bị hủy
    }
}