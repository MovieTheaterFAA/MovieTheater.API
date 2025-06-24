namespace MovieTheater.Domain.Entities
{
    public class AuditLog : BaseEntity
    {
        public Guid AdminId { get; set; }
        public string ActionType { get; set; }      // e.g., Create, Update, Delete
        public string EntityType { get; set; }      // e.g., Employee, Movie, etc.
        public Guid EntityId { get; set; }
        public string? ChangedFields { get; set; }
        public string? OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Reason { get; set; }
    }
}
