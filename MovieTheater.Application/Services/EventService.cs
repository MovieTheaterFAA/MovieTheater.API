using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.EventDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System.Text.Json;

namespace MovieTheater.Application.Services
{
    public class EventService : IEventService
    {
        private readonly ILoggerService _loggerService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimsService _claimsService;
        private readonly IAuditLogService _auditLogService;

        public EventService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService, IAuditLogService auditLogService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
            _claimsService = claimsService;
            _auditLogService = auditLogService;
        }

        public async Task<EventResponseDto?> AddEventAsync(EventRequestDto dto)
        {
            _loggerService.Info($"[AddEventAsync] Start adding event: {dto.Name}");

            // Kiểm tra sự kiện có tồn tại với tên đã cho chưa
            var existingEvent = await _unitOfWork.Events.FirstOrDefaultAsync(e => e.Name == dto.Name);
            if (existingEvent != null)
            {
                _loggerService.Warn($"[AddEventAsync] Event with name {dto.Name} already exists.");
                throw new InvalidOperationException("Event with this name already exists.");
            }

            var adminId = _claimsService.GetCurrentUserId;

            // Tạo đối tượng Event từ DTO
            var newEvent = new Event
            {
                Name = dto.Name,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Detail = dto.Detail,
                Image = dto.Image,
            };

            var newData = new
            {
                newEvent.Name,
                newEvent.StartTime,
                newEvent.EndTime,
                newEvent.Detail,
                newEvent.Image,
            };

            var changgedFields = JsonSerializer.Serialize(new
            {
                newEvent.Name,
                newEvent.StartTime,
                newEvent.EndTime,
                newEvent.Detail,
                newEvent.Image,
            });



            // Thêm sự kiện vào cơ sở dữ liệu
            await _unitOfWork.Events.AddAsync(newEvent);

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                _loggerService.Error($"DbUpdateException: {dbEx.InnerException?.Message ?? dbEx.Message}");
                throw;
            }

            await _auditLogService.LogAsync
                (
                adminId,
                AuditActionType.Create,
                "Event",
                newEvent.Id,
                null,
                newData,
                changgedFields,
                "Admin created new event."
                );

            _loggerService.Success($"[AddEventAsync] Event {newEvent.Name} added successfully.");

            // Trả về DTO chứa thông tin của sự kiện đã thêm
            return new EventResponseDto
            {
                Id = newEvent.Id,
                Name = newEvent.Name,
                StartTime = newEvent.StartTime,
                EndTime = newEvent.EndTime,
                Detail = newEvent.Detail,
                Image = newEvent.Image,
            };
        }

        public async Task<EventResponseDto?> UpdateEventAsync(Guid eventId, EventUpdateDto dto)
        {
            try
            {
                _loggerService.Info($"[UpdateEventAsync] Start updating event: {eventId}");

                var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId);
                if (eventEntity == null || eventEntity.IsDeleted)
                {
                    _loggerService.Warn($"[UpdateEventAsync] Event with ID {eventId} not found.");
                    throw ErrorHelper.NotFound("Event not found.");
                }

                var oldData = new
                {
                    eventEntity.Name,
                    eventEntity.StartTime,
                    eventEntity.EndTime,
                    eventEntity.Detail,
                    eventEntity.Image,
                };

                bool isUpdated = false;

                if (!string.IsNullOrWhiteSpace(dto.Name) && eventEntity.Name != dto.Name)
                {
                    var existing = await _unitOfWork.Events.FirstOrDefaultAsync(
                        e => e.Name == dto.Name && e.Id != eventId && !e.IsDeleted);
                    if (existing != null)
                    {
                        _loggerService.Warn($"[UpdateEventAsync] Event with name '{dto.Name}' already exists.");
                        throw ErrorHelper.Conflict("Event with the same name already exists.");
                    }
                    eventEntity.Name = dto.Name;
                    isUpdated = true;
                }

                if (dto.StartTime.HasValue && eventEntity.StartTime != dto.StartTime.Value)
                {
                    if (dto.StartTime.Value <= DateTime.UtcNow)
                    {
                        _loggerService.Warn($"[UpdateEventAsync] Start time cannot be in the past for EventId: {eventId}");
                        throw ErrorHelper.BadRequest("Start time cannot be in the past.");
                    }
                    eventEntity.StartTime = dto.StartTime.Value;
                    isUpdated = true;
                }

                if (dto.EndTime.HasValue && eventEntity.EndTime != dto.EndTime.Value)
                {
                    if (dto.StartTime.HasValue && dto.EndTime.Value <= dto.StartTime.Value)
                        throw ErrorHelper.BadRequest("End time must be greater than start time.");
                    if (!dto.StartTime.HasValue && dto.EndTime.Value <= eventEntity.StartTime)
                        throw ErrorHelper.BadRequest("End time must be greater than start time.");
                    eventEntity.EndTime = dto.EndTime.Value;
                    isUpdated = true;
                }

                if (!string.IsNullOrWhiteSpace(dto.Detail) && eventEntity.Detail != dto.Detail)
                {
                    eventEntity.Detail = dto.Detail;
                    isUpdated = true;
                }

                if (!string.IsNullOrWhiteSpace(dto.Image) && eventEntity.Image != dto.Image)
                {
                    eventEntity.Image = dto.Image;
                    isUpdated = true;
                }

                if (!isUpdated)
                {
                    _loggerService.Warn($"[UpdateEventAsync] No changes detected for EventId: {eventId}");
                    return new EventResponseDto
                    {
                        Id = eventEntity.Id,
                        Name = eventEntity.Name,
                        StartTime = eventEntity.StartTime,
                        EndTime = eventEntity.EndTime,
                        Detail = eventEntity.Detail,
                        Image = eventEntity.Image
                    };
                }

                await _unitOfWork.Events.Update(eventEntity);
                await _unitOfWork.SaveChangesAsync();

                var newData = new
                {
                    eventEntity.Name,
                    eventEntity.StartTime,
                    eventEntity.EndTime,
                    eventEntity.Detail,
                    eventEntity.Image
                };

                var changedFields = JsonSerializer.Serialize(new
                {
                    eventEntity.Name,
                    eventEntity.StartTime,
                    eventEntity.EndTime,
                    eventEntity.Detail,
                    eventEntity.Image
                });

                var adminId = _claimsService.GetCurrentUserId;

                await _auditLogService.LogAsync
                    (
                    adminId,
                    AuditActionType.Update,
                    "Event",
                    eventId,
                    oldData,
                    newData,
                    changedFields,
                    "Admin updated event information."
                    );

                _loggerService.Success($"[UpdateEventAsync] Event '{eventEntity.Name}' updated successfully.");

                return new EventResponseDto
                {
                    Id = eventEntity.Id,
                    Name = eventEntity.Name,
                    StartTime = eventEntity.StartTime,
                    EndTime = eventEntity.EndTime,
                    Detail = eventEntity.Detail,
                    Image = eventEntity.Image
                };
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[UpdateEventAsync] Error updating event{eventId}: {ex.Message}");
                throw;
            }
        }
    }
}