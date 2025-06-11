using System.Threading.Tasks;
using MovieTheater.Domain.DTOs.FoodAndDrinkDTOs;
using MovieTheater.Infrastructure.Commons;

namespace MovieTheater.Application.Interfaces
{
    public interface IFoodAndDrinkService
    {
        Task<Pagination<FoodAndDrinkResponseDto>> GetAllFoodAndDrinkAsync(
            string? search,
            string? sortBy,
            bool isDescending,
            int page,
            int pageSize);
        Task<FoodAndDrinkResponseDto> AddFoodAndDrinkAsync(FoodAndDrinkRequestDto dto);
        Task<bool> DeleteFoodAndDrinkAsync(Guid foodAndDrinkId);

    }
}