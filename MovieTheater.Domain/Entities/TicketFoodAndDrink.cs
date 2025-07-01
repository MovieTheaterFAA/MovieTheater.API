using System.ComponentModel.DataAnnotations.Schema;

namespace MovieTheater.Domain.Entities
{
    public class TicketFoodAndDrink : BaseEntity
    {
        public Guid TicketId { get; set; }
        [ForeignKey(nameof(TicketId))]
        public Ticket Ticket { get; set; }

        public Guid FoodAndDrinkId { get; set; }
        [ForeignKey(nameof(FoodAndDrinkId))]
        public FoodAndDrink FoodAndDrink { get; set; }

        public int Quantity { get; set; }
    }
}
