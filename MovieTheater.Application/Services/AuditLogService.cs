using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.AuditLogDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System.Text.Json;

namespace MovieTheater.Application.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _loggerService;
        private readonly IRedisService _redisService;

        public AuditLogService(IUnitOfWork unitOfWork, ILoggerService loggerService, IRedisService redisService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
            _redisService = redisService;
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
            await _redisService.RemoveByPatternAsync("auditlog:list:");
        }

        public async Task<Pagination<AuditLogDto>> ViewLogAsync(string? search, AuditActionType? actionType, string? entityType, bool isDescending, int page, int pageSize)
        {
            try
            {
                var cacheKey = $"auditlog:list:{search}:{actionType}:{entityType}:{isDescending}:{page}:{pageSize}";
                var cached = await _redisService.GetAsync<Pagination<AuditLogDto>>(cacheKey);
                if (cached != null)
                {
                    _loggerService.Info($"[CACHE HIT] {cacheKey}");
                    return cached;
                }

                _loggerService.Info($"[CACHE MISS] {cacheKey} — Fetching from DB");

                var logs = await _unitOfWork.AuditLogs.GetAllAsync();
                var users = await _unitOfWork.Users.GetAllAsync();
                var query = logs.AsQueryable();

                if (actionType.HasValue)
                    query = query.Where(log => log.ActionType == actionType.Value.ToString());

                if (!string.IsNullOrWhiteSpace(entityType))
                    query = query.Where(log => log.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase));

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

                var paginated = new Pagination<AuditLogDto>(result, totalLogs, page, pageSize);
                await _redisService.SetAsync(cacheKey, paginated, TimeSpan.FromMinutes(5));

                _loggerService.Success($"Retrieved {pagedLogs.Count} audit logs on page {page} successfully.");
                return paginated;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Failed to retrieve audit logs. Exception: {ex.Message}");
                throw new Exception("An error occurred while retrieving audit logs. Please try again later.");
            }
        }

    }
}
