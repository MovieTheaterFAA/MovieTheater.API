using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.AuditLogDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;
using System.Text.Json;

namespace MovieTheater.Application.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _loggerService;

        public AuditLogService(IUnitOfWork unitOfWork, ILoggerService loggerService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
        }

        public async Task LogAsync(Guid adminId, AuditActionType actionType, string entityType, Guid entityId, object oldValue, object newValue, string changedFields, string reason = null)
        {
            var log = new AuditLog
            {
                AdminId = adminId,
                ActionType = actionType.ToString(),
                EntityType = entityType,
                EntityId = entityId,
                OldValue = JsonSerializer.Serialize(oldValue),
                NewValue = JsonSerializer.Serialize(newValue),
                ChangedFields = changedFields,
                Reason = reason,
                Timestamp = DateTime.UtcNow
            };

            await _unitOfWork.AuditLogs.AddAsync(log);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<Pagination<AuditLogDto>> ViewLogAsync(string? search, AuditActionType? actionType, string? entityType, bool isDescending, int page, int pageSize)
        {
            try
            {
                _loggerService.Info($"Fetching audit logs - Page {page}, PageSize {pageSize}, Search: {search}");

                var logs = await _unitOfWork.AuditLogs.GetAllAsync();
                var users = await _unitOfWork.Users.GetAllAsync();

                var query = logs.AsQueryable();

                if (actionType.HasValue)
                {
                    string actionTypeStr = actionType.Value.ToString();
                    query = query.Where(log => log.ActionType == actionTypeStr);
                }

                if (!string.IsNullOrWhiteSpace(entityType))
                {
                    query = query.Where(log => log.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowerSearch = search.ToLower();

                    var matchedAdminIds = users
                        .Where(u => !string.IsNullOrEmpty(u.FullName) && u.FullName.ToLower().Contains(lowerSearch))
                        .Select(u => u.Id)
                        .ToList();

                    query = query.Where(log =>
                        (!string.IsNullOrEmpty(log.EntityType) && log.EntityType.ToLower().Contains(lowerSearch)) ||
                        matchedAdminIds.Contains(log.AdminId)
                    );
                }

                var totalLogs = query.Count();

                query = isDescending
                    ? query.OrderByDescending(log => log.Timestamp)
                    : query.OrderBy(log => log.Timestamp);

                var pagedLogs = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var result = pagedLogs.Select(l => new AuditLogDto
                {
                    Id = l.Id,
                    AdminId = l.AdminId,
                    ActionType = Enum.TryParse<AuditActionType>(l.ActionType, true, out var parsedType) ? parsedType : null,
                    EntityType = l.EntityType,
                    EntityId = l.EntityId,
                    ChangedFields = l.ChangedFields,
                    OldValue = l.OldValue,
                    NewValue = l.NewValue,
                    Reason = l.Reason,
                    Timestamp = l.Timestamp,
                }).ToList();

                _loggerService.Success($"Retrieved {pagedLogs.Count} audit logs on page {page} successfully.");

                return new Pagination<AuditLogDto>(result, totalLogs, page, pageSize);
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Failed to retrieve audit logs. Exception: {ex.Message}");
                throw new Exception("An error occurred while retrieving audit logs. Please try again later.");
            }
        }
    }
}
