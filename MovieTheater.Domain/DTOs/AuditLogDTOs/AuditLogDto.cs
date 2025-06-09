using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.DTOs.AuditLogDTOs
{
    public class AuditLogDto
    {
        public Guid Id { get; set;}
        public Guid AdminId { get; set; }
        public string ActionType { get; set; }      // e.g., Create, Update, Delete
        public string EntityType { get; set; }      // e.g., Employee, Movie, etc.
        public Guid EntityId { get; set; }          // Entity ID that was modified
        public string ChangedFields { get; set; }   // JSON or string representation of changed fields
        public string OldValue { get; set; }        // JSON or string representation of old value (for updates)
        public string NewValue { get; set; }        // JSON or string representation of new value (for updates)
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // test fix
        public string Reason { get; set; }
    }
}
