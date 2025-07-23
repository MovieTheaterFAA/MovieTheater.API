namespace MovieTheater.Domain.DTOs.StatisticDTOs
{
    public class MonthlyFoodAndDrinkRevenueDto
    {
        public Guid FoodAndDrinkId { get; set; }
        public string FoodAndDrinkName { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalSold { get; set; }
    }
}
