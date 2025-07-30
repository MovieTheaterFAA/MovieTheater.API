namespace MovieTheater.Domain.DTOs.StatisticDTOs
{
    public class MonthlyMovieRevenueDto
    {
        public Guid MovieId { get; set; }
        public string MovieName { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalTickets { get; set; }
    }
}
