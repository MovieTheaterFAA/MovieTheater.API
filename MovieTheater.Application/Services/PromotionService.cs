using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.PromotionDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;

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
}