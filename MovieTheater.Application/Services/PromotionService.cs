using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.PromotionDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Application.Services
{
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

        public async Task<PromotionResponseDto> AddPromotionAsync(PromotionRequestDto promotionRequestDto)
        {
            try
            {
                _loggerService.Info($"[AddPromotionAsync] Start adding promotion for Title: {promotionRequestDto.Title}");

                // Kiểm tra nếu đã có khuyến mãi với Title trùng
                var existingPromotion = await _unitOfWork.Promotions.FirstOrDefaultAsync(p => p.Title == promotionRequestDto.Title && !p.IsDeleted);
                if (existingPromotion != null)
                {
                    _loggerService.Warn($"[AddPromotionAsync] Promotion with title '{promotionRequestDto.Title}' already exists.");
                    throw new InvalidOperationException("Promotion with the same title already exists.");
                }

                // Tạo mới Promotion
                var promotion = new Promotion
                {
                    Title = promotionRequestDto.Title,
                    StartTime = promotionRequestDto.StartTime,
                    EndTime = promotionRequestDto.EndTime,
                    DiscountValue = promotionRequestDto.DiscountValue,
                    Detail = promotionRequestDto.Detail,
                    Image = promotionRequestDto.Image,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _claimsService.GetCurrentUserId
                };

                // Thêm Promotion vào cơ sở dữ liệu
                await _unitOfWork.Promotions.AddAsync(promotion);
                await _unitOfWork.SaveChangesAsync();

                _loggerService.Success($"[AddPromotionAsync] Promotion '{promotion.Title}' added successfully.");

                // Trả về PromotionResponseDto
                var responseDto = new PromotionResponseDto
                {
                    Id = promotion.Id,
                    Title = promotion.Title,
                    StartTime = promotion.StartTime,
                    EndTime = promotion.EndTime,
                    DiscountValue = promotion.DiscountValue,
                    Detail = promotion.Detail,
                    Image = promotion.Image
                };

                return responseDto;
            }
            catch (DbUpdateException dbEx)
            {
                _loggerService.Error($"DbUpdateException: {dbEx.InnerException?.Message ?? dbEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[UpdatePromotionAsync] Error updating promotion: {ex.Message}");
                throw;
            }
        }

        public async Task<PromotionResponseDto> UpdatePromotionAsync(Guid promotionId, PromotionUpdateDto promotionUpdateDto)
        {
            try
            {
                _loggerService.Info($"[UpdatePromotionAsync] Start updating promotion with ID: {promotionId}");

                var promotion = await _unitOfWork.Promotions.GetByIdAsync(promotionId);
                if (promotion == null || promotion.IsDeleted)
                {
                    _loggerService.Warn($"[UpdatePromotionAsync] Promotion with ID '{promotionId}' not found.");
                    throw ErrorHelper.NotFound("Promotion not found.");
                }

                bool isUpdated = false;

                if (!string.IsNullOrWhiteSpace(promotionUpdateDto.Title) && promotion.Title != promotionUpdateDto.Title)
                {
                    var existing = await _unitOfWork.Promotions.FirstOrDefaultAsync(
                        p => p.Title == promotionUpdateDto.Title && p.Id != promotionId && !p.IsDeleted);
                    if (existing != null)
                    {
                        _loggerService.Warn($"[UpdatePromotionAsync] Promotion with title '{promotionUpdateDto.Title}' already exists.");
                        throw ErrorHelper.Conflict("Promotion with the same title already exists.");
                    }
                    promotion.Title = promotionUpdateDto.Title;
                    isUpdated = true;
                }
                if (promotionUpdateDto.StartTime.HasValue && promotion.StartTime != promotionUpdateDto.StartTime.Value)
                {
                    if (promotionUpdateDto.StartTime.Value < DateTime.UtcNow)
                        throw ErrorHelper.BadRequest("StartTime cannot be in the past.");
                    promotion.StartTime = promotionUpdateDto.StartTime.Value;
                    isUpdated = true;
                }
                if (promotionUpdateDto.EndTime.HasValue && promotion.EndTime != promotionUpdateDto.EndTime.Value)
                {
                    var proposedStart = promotionUpdateDto.StartTime ?? promotion.StartTime;

                    if (promotionUpdateDto.EndTime.Value < proposedStart)
                        throw ErrorHelper.BadRequest("EndTime must be after StartTime.");

                    promotion.EndTime = promotionUpdateDto.EndTime.Value;
                    isUpdated = true;
                }

                if (promotionUpdateDto.DiscountValue.HasValue && promotion.DiscountValue != promotionUpdateDto.DiscountValue.Value)
                {
                    promotion.DiscountValue = promotionUpdateDto.DiscountValue.Value;
                    isUpdated = true;
                }
                if (!string.IsNullOrWhiteSpace(promotionUpdateDto.Detail) && promotion.Detail != promotionUpdateDto.Detail)
                {
                    promotion.Detail = promotionUpdateDto.Detail;
                    isUpdated = true;
                }
                if (!string.IsNullOrWhiteSpace(promotionUpdateDto.Image) && promotion.Image != promotionUpdateDto.Image)
                {
                    promotion.Image = promotionUpdateDto.Image;
                    isUpdated = true;
                }

                if (!isUpdated)
                {
                    _loggerService.Warn($"[UpdatePromotionAsync] No changes detected for PromotionId: {promotionId}");
                    return new PromotionResponseDto
                    {
                        Id = promotion.Id,
                        Title = promotion.Title,
                        StartTime = promotion.StartTime,
                        EndTime = promotion.EndTime,
                        DiscountValue = promotion.DiscountValue,
                        Detail = promotion.Detail,
                        Image = promotion.Image
                    };
                }

                await _unitOfWork.Promotions.Update(promotion);
                await _unitOfWork.SaveChangesAsync();

                _loggerService.Success($"[UpdatePromotionAsync] Promotion '{promotion.Title}' updated successfully.");

                return new PromotionResponseDto
                {
                    Id = promotion.Id,
                    Title = promotion.Title,
                    StartTime = promotion.StartTime,
                    EndTime = promotion.EndTime,
                    DiscountValue = promotion.DiscountValue,
                    Detail = promotion.Detail,
                    Image = promotion.Image
                };
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[UpdatePromotionAsync] Error updating promotion: {ex.Message}");
                throw;
            }
        }
    }
}
