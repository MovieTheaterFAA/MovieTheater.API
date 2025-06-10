using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.FoodAndDrinkDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Application.Services
{
    public class FoodAndDrinkService : IFoodAndDrinkService
    {
        private readonly ILoggerService _loggerService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimsService _claimsService;

        public FoodAndDrinkService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
            _claimsService = claimsService;
        }
        public async Task<FoodAndDrinkResponseDTO> AddFoodAndDrinkAsync(FoodAndDrinkRequestDto dto)
        {
            _loggerService.Info($"[AddFoodAndDrinkAsync] Start adding food and drink: {dto.Name}");

            // Kiểm tra món ăn/thức uống đã tồn tại chưa
            if (await FoodAndDrinkExistsAsync(dto.Name))
            {
                _loggerService.Warn($"[AddFoodAndDrinkAsync] Food or drink {dto.Name} already exists.");
                throw ErrorHelper.Conflict("Food or drink with the same name already exists.");
            }

            // Chuyển đổi DTO thành entity FoodAndDrink
            var foodAndDrink = new FoodAndDrink
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Type = dto.Type,
                ImageUrl = dto.ImageUrl,
                IsAvailable = dto.IsAvailable,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _claimsService.GetCurrentUserId // Hoặc Guid.Empty nếu không có
            };

            // Thêm món ăn/thức uống vào cơ sở dữ liệu
            await _unitOfWork.FoodAndDrinks.AddAsync(foodAndDrink);

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                _loggerService.Error($"DbUpdateException: {dbEx.InnerException?.Message ?? dbEx.Message}");
                throw;
            }

            _loggerService.Success($"[AddFoodAndDrinkAsync] Food and drink {foodAndDrink.Name} added successfully.");

            // Trả về DTO response
            return new FoodAndDrinkResponseDTO
            {
                Id = foodAndDrink.Id,
                Name = foodAndDrink.Name,
                Description = foodAndDrink.Description,
                Price = foodAndDrink.Price,
                Type = foodAndDrink.Type,
                ImageUrl = foodAndDrink.ImageUrl,
                IsAvailable = foodAndDrink.IsAvailable,
                CreatedAt = foodAndDrink.CreatedAt,
                CreatedBy = foodAndDrink.CreatedBy,
                UpdatedAt = foodAndDrink.UpdatedAt,
                UpdatedBy = foodAndDrink.UpdatedBy,
                DeletedAt = foodAndDrink.DeletedAt,
                DeletedBy = foodAndDrink.DeletedBy
            };
        }
        private async Task<bool> FoodAndDrinkExistsAsync(string name)
        {
            var existingFoodAndDrink = await _unitOfWork.FoodAndDrinks.FirstOrDefaultAsync(f => f.Name == name);
            return existingFoodAndDrink != null;
        }
    }
}
