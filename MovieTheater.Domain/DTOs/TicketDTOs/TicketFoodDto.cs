namespace MovieTheater.Domain.DTOs.TicketDTOs
{
    public class TicketFoodDto
    {
        public Guid FoodId { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
