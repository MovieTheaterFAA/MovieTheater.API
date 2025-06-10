namespace MovieTheater.Domain.Entities
{
    public class Promotion : BaseEntity
    {
        public string Title { get; set; }
        public decimal DiscountValue { get; set; }
        public string Detail { get; set; }
        public string Image { get; set; }

        // Navigation
        public ICollection<Event> Events { get; set; }
    }
}
