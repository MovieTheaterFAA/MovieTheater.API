namespace MovieTheater.Domain.Entities
{
    public class Event : BaseEntity
    {
        public string Name { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Detail { get; set; }
        public string Image { get; set; }

        // Navigation
        public ICollection<Promotion> Promotions { get; set; }
    }
}
