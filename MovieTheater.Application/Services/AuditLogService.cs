using Microsoft.VisualBasic;
using MovieTheater.Application.Interfaces;
using MovieTheater.Domain.DTOs.AuditLogDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MovieTheater.Application.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuditLogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

        public async Task<List<AuditLogDto>> ViewLogAsync()
        {
            var logs = await _unitOfWork.AuditLogs.GetAllAsync();

            var logDtos = logs.Select(log => new AuditLogDto
            {
                Id = log.Id,
                AdminId = log.AdminId,
                ActionType = log.ActionType,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                ChangedFields = log.ChangedFields,
                OldValue = log.OldValue,
                NewValue = log.NewValue,
                Timestamp = log.Timestamp,
                Reason = log.Reason
            }).ToList();

            return logDtos;
        }
    }
}
