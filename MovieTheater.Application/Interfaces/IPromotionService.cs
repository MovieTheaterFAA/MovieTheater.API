using MovieTheater.Domain.DTOs.PromotionDTOs;

namespace MovieTheater.Application.Interfaces
{
    public interface IPromotionService
    {
        Task<PromotionResponseDto?> AddPromotionAsync(PromotionRequestDto dto);
        Task<PromotionResponseDto?> UpdatePromotionAsync(Guid promotionId, PromotionUpdateDto dto);
        Task<bool> DeletePromotionAsync(Guid promotionId);
    }
}