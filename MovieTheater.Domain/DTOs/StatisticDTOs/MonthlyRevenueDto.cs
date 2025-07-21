namespace MovieTheater.Domain.DTOs.StatisticDTOs
{
    public class MonthlyRevenueDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
