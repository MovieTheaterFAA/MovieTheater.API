using System.Threading.Tasks;
using MovieTheater.Domain.DTOs.PromotionDTOs;

namespace MovieTheater.Application.Interfaces
{
    public interface IPromotionService
    {
        Task<List<PromotionResponseDto>> GetAllPromotionListAsync();
        Task<PromotionResponseDto?> AddPromotionAsync(PromotionRequestDto dto);
        Task<bool> DeletePromotionAsync(Guid promotionId);

    }
}