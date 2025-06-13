using MovieTheater.Domain.DTOs.AuditLogDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;
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
        Task<Pagination<AuditLogDto>> ViewLogAsync(string? search,AuditActionType? actionType, string? entityType,bool isDescending,int page,int pageSize);
    }
}
