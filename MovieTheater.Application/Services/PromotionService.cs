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

            // Tạo đối tượng Promotion từ DTO
            var promotion = new Promotion
            {
                Title = dto.Title,
                DiscountValue = dto.DiscountValue,
                Detail = dto.Detail,
                Image = dto.Image,
                IsDeleted = dto.IsDeleted,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _claimsService.GetCurrentUserId // ID của người dùng tạo
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
                IsDeleted = promotion.IsDeleted,
                CreatedAt = promotion.CreatedAt,
                CreatedBy = promotion.CreatedBy,
                UpdatedAt = promotion.UpdatedAt,
                UpdatedBy = promotion.UpdatedBy,
                DeletedAt = promotion.DeletedAt,
                DeletedBy = promotion.DeletedBy
            };
        }
    }
}
