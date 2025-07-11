using MovieTheater.Domain.DTOs.PromotionDTOs;

namespace MovieTheater.Application.Interfaces
{
    public interface IPromotionService
    {
        Task<PromotionResponseDto?> AddPromotionAsync(PromotionRequestDto dto);
        Task<PromotionResponseDto?> UpdatePromotionAsync(Guid promotionId, PromotionUpdateDto dto);
        Task<bool> DeletePromotionAsync(Guid promotionId);

        // User promotions
        Task<bool> ClaimPromotionAsync(Guid promotionId, Guid userId);
        Task<bool> UseClaimedPromotionAsync(Guid promotionId, Guid userId);
        Task<bool> HasUserClaimedPromotionAsync(Guid promotionId, Guid userId);
        Task<IEnumerable<PromotionResponseDto>> GetClaimedPromotionsByUserAsync(Guid userId);
    }
}