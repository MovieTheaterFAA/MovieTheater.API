using MovieTheater.Domain.DTOs.AuditLogDTOs;
using MovieTheater.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Application.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(Guid adminId, AuditActionType actionType, string entityType, Guid entityId,
                  object oldValue, object newValue, string changedFields, string reason = null);
        Task<List<AuditLogDto>> ViewLogAsync();
    }
}
