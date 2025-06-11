using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.PromotionDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Application.Services;

public class PromotionService : IPromotionService
{
    private readonly ILoggerService _loggerService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;

    public PromotionService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService)
    {
        _unitOfWork = unitOfWork;
        _loggerService = loggerService;
        _claimsService = claimsService;
    }

    public async Task<List<PromotionResponseDto>> GetAllPromotionListAsync()
    {
        try
        {
            var allPromotions = await _unitOfWork.Promotions.GetAllAsync();

            var result = allPromotions.Select(p => new PromotionResponseDto
            {
                Id = p.Id,
                Title = p.Title,
                DiscountValue = p.DiscountValue,
                Detail = p.Detail,
                Image = p.Image
            }).ToList();

            _loggerService.Success($"Retrieved {result.Count} promotions successfully.");

            return new List<PromotionResponseDto>();
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error retrieving promotions: {ex.Message}");
            throw;
        }
    }

    public async Task<PromotionResponseDto?> AddPromotionAsync(PromotionRequestDto dto)
    {
        _loggerService.Info($"[AddPromotionAsync] Start adding promotion: {dto.Title}");

        // Kiểm tra nếu chương trình khuyến mãi đã tồn tại
        var existingPromotion = await _unitOfWork.Promotions.FirstOrDefaultAsync(p => p.Title == dto.Title);

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
            Image = dto.Image,
            IsDeleted = false,
            EventId = dto.EventId
        };

        // Thêm chương trình khuyến mãi vào cơ sở dữ liệu
        await _unitOfWork.Promotions.AddAsync(promotion);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException dbEx)
        {
            _loggerService.Error($"DbUpdateException: {dbEx.InnerException?.Message ?? dbEx.Message}");
            throw;
        }

        _loggerService.Success($"[AddPromotionAsync] Promotion {promotion.Title} added successfully.");

        // Trả về PromotionResponseDto
        return new PromotionResponseDto
        {
            Id = promotion.Id,
            Title = promotion.Title,
            DiscountValue = promotion.DiscountValue,
            Detail = promotion.Detail,
            Image = promotion.Image,
            EventId = promotion.EventId
        };
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

            if (!string.IsNullOrWhiteSpace(dto.Image) && promotion.Image != dto.Image)
            {
                promotion.Image = dto.Image;
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
                    Image = promotion.Image,
                    EventId = promotion.EventId
                };
            }

            await _unitOfWork.Promotions.Update(promotion);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.Success($"[UpdatePromotionAsync] Promotion '{promotion.Title}' updated successfully.");

            return new PromotionResponseDto
            {
                Id = promotion.Id,
                Title = promotion.Title,
                DiscountValue = promotion.DiscountValue,
                Detail = promotion.Detail,
                Image = promotion.Image,
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