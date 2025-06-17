using MovieTheater.Domain.DTOs.FoodAndDrinkDTOs;

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
        Task<FoodAndDrinkResponseDto> UpdateFoodAndDrinkAsync(Guid id, FoodAndDrinkRequestDto dto);
        Task<FoodAndDrinkResponseDto> AddFoodAndDrinkAsync(FoodAndDrinkRequestDto dto);
        Task<bool> DeleteFoodAndDrinkAsync(Guid foodAndDrinkId);

    }
}