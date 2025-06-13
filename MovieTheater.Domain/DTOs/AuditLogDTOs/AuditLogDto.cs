using MovieTheater.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.DTOs.AuditLogDTOs
{
    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public Guid AdminId { get; set; }
        public AuditActionType? ActionType { get; set; }
        public string? EntityType { get; set; }
        public Guid EntityId { get; set; }
        public string? ChangedFields { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Reason { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
