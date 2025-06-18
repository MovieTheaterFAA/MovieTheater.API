namespace MovieTheater.Domain.Entities
{
    public class Promotion : BaseEntity
    {
        public string Title { get; set; }
        public decimal DiscountValue { get; set; }
        public string Detail { get; set; }

        // Navigation
        public Guid EventId { get; set; }

        public Event Event { get; set; }
    }
}