using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.CinemaRoomDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System.Text.Json;

namespace MovieTheater.Application.Services
{
    public class CinemaRoomService : ICinemaRoomService
    {
        private readonly ILoggerService _loggerService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogService _auditLogService;

        public CinemaRoomService(IUnitOfWork unitOfWork, ILoggerService loggerService, IAuditLogService auditLogService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
            _auditLogService = auditLogService;
        }

        public async Task<Pagination<CinemaRoomDto>> GetAllCinemaRoomAsync(string? search, string? sortBy, bool isDescending, int page, int pageSize)
        {
            try
            {
                _loggerService.Info($"[CinemaRoomService] Fetching cinema rooms. Page: {page}, PageSize: {pageSize}, Search: {search}");

                var query = _unitOfWork.CinemaRooms.GetQueryable().Where(x => !x.IsDeleted);

                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(x => x.Name.Contains(search));

                query = sortBy?.ToLower() switch
                {
                    "name" => isDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
                    "type" => isDescending ? query.OrderByDescending(x => x.Type) : query.OrderBy(x => x.Type),
                    _ => query.OrderBy(x => x.Name)
                };

                var total = await query.CountAsync();
                var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
                    .Select(x => new CinemaRoomDto { Id = x.Id, Name = x.Name, Type = x.Type })
                    .ToListAsync();

                _loggerService.Success($"[CinemaRoomService] Retrieved {items.Count} cinema rooms on page {page}.");

                return new Pagination<CinemaRoomDto>(items, total, page, pageSize);
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[CinemaRoomService] Error fetching cinema rooms: {ex.Message}");
                throw new Exception("An error occurred while fetching cinema rooms. Please try again later.");
            }
        }

        public async Task<CinemaRoomDto?> GetCinemaRoomByIdAsync(Guid id)
        {
            try
            {
                _loggerService.Info($"[CinemaRoomService] Fetching cinema room detail for Id: {id}");

                var room = await _unitOfWork.CinemaRooms.GetQueryable()
                    .Where(x => x.Id == id && !x.IsDeleted)
                    .Select(x => new CinemaRoomDto { Id = x.Id, Name = x.Name, Type = x.Type })
                    .FirstOrDefaultAsync();

                if (room == null)
                    _loggerService.Warn($"[CinemaRoomService] Cinema room with Id {id} not found.");

                return room;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[CinemaRoomService] Error fetching cinema room detail: {ex.Message}");
                throw new Exception("An error occurred while fetching cinema room detail. Please try again later.");
            }
        }

        public async Task<CinemaRoomDto> CreateCinemaRoomAsync(CreateCinemaRoomDto dto, Guid adminId)
        {
            try
            {
                _loggerService.Info($"[CinemaRoomService] Creating cinema room: {dto.Name}");

                var entity = new CinemaRoom
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    Type = dto.Type,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = adminId
                };
                await _unitOfWork.CinemaRooms.AddAsync(entity);
                await _unitOfWork.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    adminId,
                    AuditActionType.Create,
                    "CinemaRoom",
                    entity.Id,
                    null,
                    new { entity.Name, entity.Type },
                    JsonSerializer.Serialize(dto),
                    "Created new cinema room"
                );

                _loggerService.Success($"[CinemaRoomService] Cinema room '{entity.Name}' created successfully.");

                return new CinemaRoomDto { Id = entity.Id, Name = entity.Name, Type = entity.Type };
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[CinemaRoomService] Error creating cinema room: {ex.Message}");
                throw new Exception("An error occurred while creating cinema room. Please try again later.");
            }
        }

        public async Task<CinemaRoomDto?> UpdateCinemaRoomAsync(Guid id, UpdateCinemaRoomDto dto, Guid adminId)
        {
            try
            {
                _loggerService.Info($"[CinemaRoomService] Updating cinema room Id: {id}");

                var entity = await _unitOfWork.CinemaRooms.GetByIdAsync(id);
                if (entity == null || entity.IsDeleted)
                {
                    _loggerService.Warn($"[CinemaRoomService] Cinema room with Id {id} not found.");
                    return null;
                }

                var oldData = new { entity.Name, entity.Type };

                entity.Name = dto.Name;
                entity.Type = dto.Type;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = adminId;

                await _unitOfWork.CinemaRooms.Update(entity);
                await _unitOfWork.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    adminId,
                    AuditActionType.Update,
                    "CinemaRoom",
                    entity.Id,
                    oldData,
                    new { entity.Name, entity.Type },
                    JsonSerializer.Serialize(dto),
                    "Updated cinema room"
                );

                _loggerService.Success($"[CinemaRoomService] Cinema room '{entity.Name}' updated successfully.");

                return new CinemaRoomDto { Id = entity.Id, Name = entity.Name, Type = entity.Type };
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[CinemaRoomService] Error updating cinema room: {ex.Message}");
                throw new Exception("An error occurred while updating cinema room. Please try again later.");
            }
        }

        public async Task<bool> SoftDeleteCinemaRoomAsync(Guid id, Guid adminId)
        {
            try
            {
                _loggerService.Info($"[CinemaRoomService] Soft deleting cinema room Id: {id}");

                var entity = await _unitOfWork.CinemaRooms.GetByIdAsync(id);
                if (entity == null || entity.IsDeleted)
                {
                    _loggerService.Warn($"[CinemaRoomService] Cinema room with Id {id} not found.");
                    return false;
                }

                var oldData = new { entity.Name, entity.Type, entity.IsDeleted };

                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                entity.DeletedBy = adminId;

                await _unitOfWork.CinemaRooms.Update(entity);
                await _unitOfWork.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    adminId,
                    AuditActionType.Delete,
                    "CinemaRoom",
                    entity.Id,
                    oldData,
                    new { entity.IsDeleted },
                    JsonSerializer.Serialize(new { entity.IsDeleted }),
                    "Soft deleted cinema room"
                );

                _loggerService.Success($"[CinemaRoomService] Cinema room '{entity.Name}' soft deleted successfully.");

                return true;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[CinemaRoomService] Error soft deleting cinema room: {ex.Message}");
                throw new Exception("An error occurred while deleting cinema room. Please try again later.");
            }
        }
    }
}