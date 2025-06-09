using System.ComponentModel.DataAnnotations.Schema;

namespace MovieTheater.Domain.Entities
{
    public class BookingFood
    {
        public Guid BookingId { get; set; }

        [ForeignKey(nameof(BookingId))]
        public Booking Booking { get; set; }

        public Guid FoodAndDrinkId { get; set; }

        [ForeignKey(nameof(FoodAndDrinkId))]
        public FoodAndDrink FoodAndDrink { get; set; }

        public int Quantity { get; set; }
    }
}