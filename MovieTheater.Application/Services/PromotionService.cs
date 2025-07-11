using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.PromotionDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System.Text.Json;

namespace MovieTheater.Application.Services;

public class PromotionService : IPromotionService
{
    private readonly ILoggerService _loggerService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    private readonly IAuditLogService _auditLogService;
    private readonly IRedisService _redisService;

    public PromotionService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService, IAuditLogService auditLogService, IRedisService redisService)
    {
        _unitOfWork = unitOfWork;
        _loggerService = loggerService;
        _claimsService = claimsService;
        _auditLogService = auditLogService;
        _redisService = redisService;
    }

    public async Task<PromotionResponseDto?> AddPromotionAsync(PromotionRequestDto dto)
    {
        try
        {
            _loggerService.Info($"[AddPromotionAsync] Start adding promotion: {dto.Title}");

            // Kiểm tra nếu chương trình khuyến mãi đã tồn tại
            var existingPromotion = await _unitOfWork.Promotions.FirstOrDefaultAsync(p => p.Title == dto.Title && !p.IsDeleted);

            if (existingPromotion != null)
            {
                _loggerService.Warn($"[AddPromotionAsync] Promotion with title {dto.Title} already exists.");
                throw new InvalidOperationException("Promotion with this title already exists.");
            }

            // Kiểm tra EventId có hợp lệ hay không (sử dụng IUnitOfWork)
            var existingEvent = await _unitOfWork.Events.GetByIdAsync(dto.EventId);
            if (existingEvent == null)
            {
                _loggerService.Warn($"[AddPromotionAsync] Event with ID {dto.EventId} does not exist.");
                throw new KeyNotFoundException("Event with the provided ID does not exist.");
            }

            // Tạo đối tượng Promotion từ DTO
            var promotion = new Promotion
            {
                Title = dto.Title,
                DiscountValue = dto.DiscountValue,
                Detail = dto.Detail,
                IsDeleted = false,
                EventId = dto.EventId
            };

            var adminId = _claimsService.GetCurrentUserId;

            var newData = new
            {
                promotion.Title,
                promotion.DiscountValue,
                promotion.Detail,
                promotion.EventId
            };

            var changedFields = JsonSerializer.Serialize(new
            {
                promotion.Title,
                promotion.DiscountValue,
                promotion.Detail,
                promotion.EventId
            });
            // Thêm chương trình khuyến mãi vào cơ sở dữ liệu
            await _unitOfWork.Promotions.AddAsync(promotion);
            await _unitOfWork.SaveChangesAsync();
            await _redisService.RemoveByPatternAsync("event:list:");

            await _auditLogService.LogAsync
            (
            adminId,
            AuditActionType.Create,
            "Promotion",
            promotion.Id,
            null,
            newData,
            changedFields,
            "Admin created new promotion."
            );

            _loggerService.Success($"[AddPromotionAsync] Promotion {promotion.Title} added successfully.");

            // Trả về PromotionResponseDto
            return new PromotionResponseDto
            {
                Id = promotion.Id,
                Title = promotion.Title,
                DiscountValue = promotion.DiscountValue,
                Detail = promotion.Detail,
                EventId = promotion.EventId
            };
        }
        catch (DbUpdateException dbEx)
        {
            _loggerService.Error($"DbUpdateException: {dbEx.InnerException?.Message ?? dbEx.Message}");
            throw;
        }
    }
    public async Task<bool> DeletePromotionAsync(Guid promotionId)
    {
        try
        {
            var promotion = await _unitOfWork.Promotions.GetByIdAsync(promotionId);
            if (promotion == null || promotion.IsDeleted)
            {
                _loggerService.Warn($"Promotion with ID {promotionId} not found or already deleted.");
                return false;
            }

            var oldData = new
            {
                promotion.IsDeleted
            };

            await _unitOfWork.Promotions.SoftRemove(promotion);
            await _unitOfWork.SaveChangesAsync();
            await _redisService.RemoveByPatternAsync("event:list:");

            var newData = new
            {
                promotion.IsDeleted
            };

            var changedFields = JsonSerializer.Serialize(new
            {
                promotion.IsDeleted
            });

            var adminId = _claimsService.GetCurrentUserId;

            await _auditLogService.LogAsync
                        (
                        adminId,
                        AuditActionType.Delete,
                        "Promotion",
                        promotionId,
                        oldData,
                        newData,
                        changedFields,
                        "Admin deleted promotion."
                        );

            _loggerService.Info($"Successfully deleted promotion with ID {promotionId}.");
            return true;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"An error occurred while deleting promotion: {ex.Message}");
            return false;
        }
    }
    public async Task<PromotionResponseDto?> UpdatePromotionAsync(Guid promotionId, PromotionUpdateDto dto)
    {
        try
        {
            _loggerService.Info($"[UpdatePromotionAsync] Start updating promotion: {promotionId}");

            var promotion = await _unitOfWork.Promotions.GetByIdAsync(promotionId);
            if (promotion == null || promotion.IsDeleted)
            {
                _loggerService.Warn($"[UpdatePromotionAsync] Promotion with ID {promotionId} not found.");
                throw ErrorHelper.NotFound("Promotion not found.");
            }

            var oldData = new
            {
                promotion.Title,
                promotion.DiscountValue,
                promotion.Detail,
                promotion.EventId
            };
            bool isUpdated = false;

            if (!string.IsNullOrWhiteSpace(dto.Title) && promotion.Title != dto.Title)
            {
                var existing = await _unitOfWork.Promotions.FirstOrDefaultAsync(
                    p => p.Title == dto.Title && p.Id != promotionId && !p.IsDeleted);
                if (existing != null)
                {
                    _loggerService.Warn($"[UpdatePromotionAsync] Promotion with title '{dto.Title}' already exists.");
                    throw ErrorHelper.Conflict("Promotion with the same title already exists.");
                }
                promotion.Title = dto.Title;
                isUpdated = true;
            }

            if (dto.DiscountValue.HasValue && promotion.DiscountValue != dto.DiscountValue.Value)
            {
                if (dto.DiscountValue.Value <= 0 || dto.DiscountValue.Value > 1)
                    throw ErrorHelper.BadRequest("Discount value must be greater than zero and lower than 1.");
                promotion.DiscountValue = dto.DiscountValue.Value;
                isUpdated = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.Detail) && promotion.Detail != dto.Detail)
            {
                promotion.Detail = dto.Detail;
                isUpdated = true;
            }

            if (dto.EventId.HasValue && promotion.EventId != dto.EventId.Value)
            {
                var eventEntity = await _unitOfWork.Events.GetByIdAsync(dto.EventId.Value);
                if (eventEntity == null)
                {
                    _loggerService.Warn($"[UpdatePromotionAsync] Event with ID {dto.EventId.Value} does not exist.");
                    throw ErrorHelper.NotFound("Event with the provided ID does not exist.");
                }
                promotion.EventId = dto.EventId.Value;
                isUpdated = true;
            }

            if (!isUpdated)
            {
                _loggerService.Warn($"[UpdatePromotionAsync] No changes detected for PromotionId: {promotionId}");
                return new PromotionResponseDto
                {
                    Id = promotion.Id,
                    Title = promotion.Title,
                    DiscountValue = promotion.DiscountValue,
                    Detail = promotion.Detail,
                    EventId = promotion.EventId
                };
            }

            await _unitOfWork.Promotions.Update(promotion);
            await _unitOfWork.SaveChangesAsync();
            await _redisService.RemoveByPatternAsync("event:list:");

            var newData = new
            {
                promotion.Title,
                promotion.DiscountValue,
                promotion.Detail,
                promotion.EventId
            };

            var changedFields = JsonSerializer.Serialize(new
            {
                promotion.Title,
                promotion.DiscountValue,
                promotion.Detail,
                promotion.EventId
            });

            var adminId = _claimsService.GetCurrentUserId;

            await _auditLogService.LogAsync
                (
                adminId,
                AuditActionType.Update,
                "Promotion",
                promotionId,
                oldData,
                newData,
                changedFields,
                "Admin updated promotion information."
                );

            _loggerService.Success($"[UpdatePromotionAsync] Promotion '{promotion.Title}' updated successfully.");

            return new PromotionResponseDto
            {
                Id = promotion.Id,
                Title = promotion.Title,
                DiscountValue = promotion.DiscountValue,
                Detail = promotion.Detail,
                EventId = promotion.EventId
            };
        }
        catch (Exception ex)
        {
            _loggerService.Error($"[UpdatePromotionAsync] Error updating promotion{promotionId}: {ex.Message}");
            throw;
        }
    }


    public async Task<bool> ClaimPromotionAsync(Guid promotionId, Guid userId)
    {
        try
        {
            _loggerService.Info($"[ClaimPromotionAsync] User {userId} attempts to claim promotion {promotionId}");

            var promotion = await _unitOfWork.Promotions.GetByIdAsync(promotionId);
            if (promotion == null)
            {
                _loggerService.Warn($"[ClaimPromotionAsync] Promotion {promotionId} not found.");
                throw ErrorHelper.NotFound("Promotion not found.");
            }

            if (await HasUserClaimedPromotionAsync(promotionId, userId))
            {
                _loggerService.Warn($"[ClaimPromotionAsync] User {userId} already claimed promotion {promotionId}.");
                return false;
            }

            var claimedPromotion = new ClaimedPromotion
            {
                PromotionId = promotionId,
                UserId = userId,
                ClaimedAt = DateTime.UtcNow,
                IsUsed = false
            };

            await _unitOfWork.ClaimedPromotions.AddAsync(claimedPromotion);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.Success($"[ClaimPromotionAsync] User {userId} successfully claimed promotion {promotionId}.");
            return true;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"[ClaimPromotionAsync] Error: {ex.Message}");
            throw;
        }
    }
    public async Task<bool> UseClaimedPromotionAsync(Guid promotionId, Guid userId)
    {
        try
        {
            _loggerService.Info($"[UseClaimedPromotionAsync] User {userId} attempts to use promotion {promotionId}");

            var claimed = await _unitOfWork
                .ClaimedPromotions
                .GetQueryable()
                .FirstOrDefaultAsync(cp => cp.PromotionId == promotionId && cp.UserId == userId && !cp.IsUsed);

            if (claimed == null)
            {
                _loggerService.Warn($"[UseClaimedPromotionAsync] Claimed promotion not found or already used for user {userId}, promotion {promotionId}.");
                return false;
            }

            claimed.IsUsed = true;
            await _unitOfWork.SaveChangesAsync();

            _loggerService.Success($"[UseClaimedPromotionAsync] User {userId} used promotion {promotionId}.");
            return true;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"[UseClaimedPromotionAsync] Error: {ex.Message}");
            throw;
        }
    }

    public async Task<IEnumerable<PromotionResponseDto>> GetClaimedPromotionsByUserAsync(Guid userId)
    {
        try
        {
            _loggerService.Info($"[GetClaimedPromotionsByUserAsync] Get claimed promotions for user {userId}");

            var claimedPromotions = await _unitOfWork
                .ClaimedPromotions
                .GetQueryable()
                .Include(cp => cp.Promotion)
                .Where(cp => cp.UserId == userId && !cp.Promotion.IsDeleted)
                .ToListAsync();

            var result = claimedPromotions.Select(cp => new PromotionResponseDto
            {
                Id = cp.Promotion.Id,
                Title = cp.Promotion.Title,
                DiscountValue = cp.Promotion.DiscountValue,
                Detail = cp.Promotion.Detail,
                EventId = cp.Promotion.EventId,
                IsUsed = cp.IsUsed
            });

            return result;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"[GetClaimedPromotionsByUserAsync] Error: {ex.Message}");
            throw;
        }
    }

    //============== Helper Methods ==============
    public async Task<bool> HasUserClaimedPromotionAsync(Guid promotionId, Guid userId)
    {
        try
        {
            _loggerService.Info($"[HasUserClaimedPromotionAsync] Check if user {userId} claimed promotion {promotionId}");

            var claimed = await _unitOfWork
                .ClaimedPromotions
                .GetQueryable()
                .AnyAsync(cp => cp.PromotionId == promotionId && cp.UserId == userId);

            return claimed;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"[HasUserClaimedPromotionAsync] Error: {ex.Message}");
            throw;
        }
    }
}