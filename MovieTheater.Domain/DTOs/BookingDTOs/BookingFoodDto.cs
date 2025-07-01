namespace MovieTheater.Domain.DTOs.BookingDTOs
{
    public class BookingFoodDto
    {
        public Guid FoodId { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
