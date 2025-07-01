using MovieTheater.Domain.Enums;

namespace MovieTheater.Domain.Entities
{
    public class FoodAndDrink : BaseEntity
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public FoodType Type { get; set; } // Enum: Food, Drink, Combo
        public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; } = true;
        public ICollection<BookingFood> BookingFoods { get; set; }
        public ICollection<TicketFoodAndDrink> TicketFoodAndDrinks { get; set; }
    }
}