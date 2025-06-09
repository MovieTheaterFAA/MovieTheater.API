namespace MovieTheater.Domain.Entities
{
    public class Promotion : BaseEntity
    {
        public string Title { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public decimal DiscountValue { get; set; }

        public string Detail { get; set; }
        public string Image { get; set; }
    }
}
