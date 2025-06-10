using MovieTheater.Domain.DTOs.FoodAndDrinkDTOs;

namespace MovieTheater.Application.Interfaces
{
    public interface IFoodAndDrinkService
    {
        Task<FoodAndDrinkResponseDTO> AddFoodAndDrinkAsync(FoodAndDrinkRequestDto dto);
    }
}
