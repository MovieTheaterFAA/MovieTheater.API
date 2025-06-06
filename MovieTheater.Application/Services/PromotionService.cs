using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
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
                CreatedBy = _claimsService.GetCurrentUserId // Gán CreatedBy từ ClaimsService
            };

            // Thêm Promotion vào cơ sở dữ liệu
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
    }
}
