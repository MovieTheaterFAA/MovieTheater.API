using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.EventDTOs;
using MovieTheater.Domain.DTOs.PromotionDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Application.Services
{
    public class EventService : IEventService
    {
        private readonly ILoggerService _loggerService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimsService _claimsService;
        private readonly IAuditLogService _auditLogService;
        private readonly IRedisService _redisService;
        private readonly IBlobService _blobService;

        public EventService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService, IAuditLogService auditLogService, IRedisService redisService, IBlobService blobService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
            _claimsService = claimsService;
            _auditLogService = auditLogService;
            _redisService = redisService;
            _blobService = blobService;
        }

        public async Task<EventResponseDto?> AddEventAsync(EventRequestDto dto)
        {
            _loggerService.Info($"[AddEventAsync] Start adding event: {dto.Name}");

            // Kiểm tra sự kiện có tồn tại với tên đã cho chưa
            var existingEvent = await _unitOfWork.Events.FirstOrDefaultAsync(e => e.Name == dto.Name && !e.IsDeleted);
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
                await _redisService.RemoveByPatternAsync("event:list:");
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
            };
        }

        public async Task<bool> DeleteEventByIdAsync(Guid eventId)
        {
            _loggerService.Info($"Attempting to delete Event with ID: {eventId}");

            try
            {
                var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId, e => e.Promotions);

                if (eventEntity == null)
                {
                    _loggerService.Warn($"Event with ID {eventId} not found.");
                    return false;
                }

                var oldValue = new
                {
                    eventEntity.IsDeleted
                };

                if (eventEntity.Promotions != null && eventEntity.Promotions.Any())
                {
                    await _unitOfWork.Promotions.SoftRemoveRange(eventEntity.Promotions.ToList());
                }

                await _unitOfWork.Events.SoftRemove(eventEntity);

                await _unitOfWork.SaveChangesAsync();
                await _redisService.RemoveByPatternAsync("event:list:");

                var newValue = new
                {
                    eventEntity.IsDeleted
                };

                var changedFields = JsonSerializer.Serialize(new
                {
                    eventEntity.IsDeleted
                });

                var adminId = _claimsService.GetCurrentUserId;

                await _auditLogService.LogAsync(
                    adminId,
                    AuditActionType.Delete,
                    "Event",
                    eventId,
                    oldValue,
                    newValue,
                    changedFields,
                    "Admin deleted event."
                );

                _loggerService.Info($"Successfully deleted Event with ID: {eventId}");
                return true;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error deleting Event with ID {eventId}: {ex.Message}");
                return false;
            }
        }

        public async Task<Pagination<EventResponseDto>> GetAllEventsAsync(string? search, string? sortBy, bool isDescending, int page, int pageSize)
        {
            try
            {
                var cacheKey = $"event:list:{search}:{sortBy}:{isDescending}:{page}:{pageSize}";
                var cached = await _redisService.GetAsync<Pagination<EventResponseDto>>(cacheKey);
                if (cached != null)
                {
                    _loggerService.Info($"[CACHE HIT] {cacheKey}");
                    return cached;
                }

                _loggerService.Info($"[CACHE MISS] {cacheKey} — Fetching from DB");

                var events = await _unitOfWork.Events.GetAllAsync(null, e => e.Promotions);
                var query = events.AsQueryable().Where(e => !e.IsDeleted);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowerSearch = search.ToLower();
                    query = query.Where(e => !string.IsNullOrEmpty(e.Name) && e.Name.ToLower().Contains(lowerSearch));
                }

                var totalEvents = query.Count();

                query = sortBy?.ToLower() switch
                {
                    "name" => isDescending ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name),
                    "starttime" => isDescending ? query.OrderByDescending(e => e.StartTime) : query.OrderBy(e => e.StartTime),
                    "endtime" => isDescending ? query.OrderByDescending(e => e.EndTime) : query.OrderBy(e => e.EndTime),
                    _ => query.OrderBy(e => e.Id)
                };

                var pagedEvents = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var result = pagedEvents.Select(e => new EventResponseDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    Detail = e.Detail,
                    Image = e.Image,
                    Promotions = e.Promotions?
                        .Where(p => !p.IsDeleted)
                        .Select(p => new PromotionResponseDto
                        {
                            Id = p.Id,
                            Title = p.Title,
                            DiscountValue = p.DiscountValue,
                            Detail = p.Detail,
                            EventId = p.EventId
                        }).ToList() ?? new List<PromotionResponseDto>()
                }).ToList();

                var paginated = new Pagination<EventResponseDto>(result, totalEvents, page, pageSize);
                await _redisService.SetAsync(cacheKey, paginated, TimeSpan.FromMinutes(5));

                _loggerService.Success($"Retrieved {result.Count} events on page {page} successfully.");
                return paginated;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Failed to retrieve events. Exception: {ex.Message}");
                throw new Exception("An error occurred while retrieving events. Please try again later.");
            }
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
                await _redisService.RemoveByPatternAsync("event:list:");

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

        public async Task CleanUpExpiredEventsAsync()
        {
            _loggerService.Info("[AutoCleanup] Checking for expired events...");

            try
            {
                var now = DateTime.UtcNow;
                var expiredEvents = await _unitOfWork.Events.GetAllAsync(
                    e => e.EndTime < now && !e.IsDeleted,
                    e => e.Promotions
                );

                foreach (var evt in expiredEvents)
                {
                    var oldValue = new { evt.IsDeleted };

                    if (evt.Promotions.Any())
                        await _unitOfWork.Promotions.SoftRemoveRange(evt.Promotions.ToList());

                    await _unitOfWork.Events.SoftRemove(evt);

                    var newValue = new { evt.IsDeleted };
                    var changedFields = JsonSerializer.Serialize(new { evt.IsDeleted });

                    await _auditLogService.LogAsync(
                        Guid.Empty,
                        AuditActionType.Delete,
                        "Event",
                        evt.Id,
                        oldValue,
                        newValue,
                        changedFields,
                        "System auto-deleted expired event."
                    );

                    _loggerService.Info($"[AutoCleanup] Deleted expired event: {evt.Name}");
                }

                await _unitOfWork.SaveChangesAsync();
                await _redisService.RemoveByPatternAsync("event:list:");
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[AutoCleanup] Error while cleaning up expired events: {ex.Message}");
            }
        }

        public async Task<EventResponseDto?> AddEventWithImageAsync(EventWithImageRequestDto dto)
        {
            _loggerService.Info($"[AddEventWithImageAsync] Start adding event: {dto.Name}");

            var existingEvent = await _unitOfWork.Events.FirstOrDefaultAsync(e => e.Name == dto.Name && !e.IsDeleted);
            if (existingEvent != null)
            {
                _loggerService.Warn($"[AddEventWithImageAsync] Event with name {dto.Name} already exists.");
                throw new InvalidOperationException("Event with this name already exists.");
            }

            var adminId = _claimsService.GetCurrentUserId;

            var newEvent = new Event
            {
                Name = dto.Name,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Detail = dto.Detail,
                Image = null
            };

            await _unitOfWork.Events.AddAsync(newEvent);
            await _unitOfWork.SaveChangesAsync();

            string? imageUrl = null;
            if (dto.File != null && dto.File.Length > 0)
            {
                var safeFileName = Path.GetFileName(dto.File.FileName);
                var folder = $"event-images/{newEvent.Id}";
                var objectName = $"{folder}/{safeFileName}";

                using var stream = dto.File.OpenReadStream();
                await _blobService.UploadFileAsync(safeFileName, stream, folder, CancellationToken.None);
                imageUrl = await _blobService.GetPreviewUrlAsync(objectName);

                if (imageUrl == null)
                    throw new Exception("Could not generate preview URL.");

                newEvent.Image = imageUrl;
                await _unitOfWork.Events.Update(newEvent);
                await _unitOfWork.SaveChangesAsync();
            }

            await _redisService.RemoveByPatternAsync("event:list:");

            var logData = new
            {
                newEvent.Name,
                newEvent.StartTime,
                newEvent.EndTime,
                newEvent.Detail,
                newEvent.Image
            };

            await _auditLogService.LogAsync(
                adminId,
                AuditActionType.Create,
                "Event",
                newEvent.Id,
                null,
                logData,
                JsonSerializer.Serialize(logData),
                "Admin created new event with image."
            );

            _loggerService.Success($"[AddEventWithImageAsync] Event {newEvent.Name} created.");

            return new EventResponseDto
            {
                Id = newEvent.Id,
                Name = newEvent.Name,
                StartTime = newEvent.StartTime,
                EndTime = newEvent.EndTime,
                Detail = newEvent.Detail,
                Image = newEvent.Image
            };
        }
    }

    public class EventAutoCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventAutoCleanupBackgroundService> _logger;

        public EventAutoCleanupBackgroundService(IServiceProvider serviceProvider, ILogger<EventAutoCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[EventAutoCleanup] Background service is running.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

                    await eventService.CleanUpExpiredEventsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[EventAutoCleanup] Error while executing cleanup.");
                }

                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }
    }
}