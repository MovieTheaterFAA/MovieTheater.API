using MovieTheater.Domain.DTOs.PromotionDTOs;

namespace MovieTheater.Application.Interfaces
{
    public interface IPromotionService
    {
        Task<List<PromotionResponseDto>> GetAllPromotionListAsync();
    }
}
