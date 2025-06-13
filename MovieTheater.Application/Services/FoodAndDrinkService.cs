using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.FoodAndDrinkDTOs;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;
using MovieTheater.Domain.Entities;
using System.Text.Json;
using MovieTheater.Domain.Enums;

namespace MovieTheater.Application.Services
{
    public class FoodAndDrinkService : IFoodAndDrinkService
    {
        private readonly ILoggerService _loggerService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimsService _claimsService;
        private readonly IAuditLogService _auditLogService;

        public FoodAndDrinkService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService, IAuditLogService auditLogService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
            _claimsService = claimsService;
            _auditLogService = auditLogService;
        }

        public async Task<Pagination<FoodAndDrinkResponseDto>> GetAllFoodAndDrinkAsync(string? search, string? sortBy, bool isDescending, int page, int pageSize)
        {
            try
            {
                _loggerService.Info($"Fetching food and drinks - Page {page}, PageSize {pageSize}, Search: {search}");

                var foodAndDrinks = await _unitOfWork.FoodAndDrinks.GetAllAsync();

                var query = foodAndDrinks.AsQueryable();

                // Filter by search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowerSearch = search.ToLower();
                    query = query.Where(f =>
                        (!string.IsNullOrEmpty(f.Name) && f.Name.ToLower().Contains(lowerSearch)) ||
                        (!string.IsNullOrEmpty(f.Description) && f.Description.ToLower().Contains(lowerSearch)) ||
                        f.Type.ToString().ToLower().Contains(lowerSearch)
                    );
                }

                var totalItems = query.Count();

                // Sorting
                query = sortBy?.ToLower() switch
                {
                    "name" => isDescending ? query.OrderByDescending(f => f.Name) : query.OrderBy(f => f.Name),
                    "price" => isDescending ? query.OrderByDescending(f => f.Price) : query.OrderBy(f => f.Price),
                    "type" => isDescending ? query.OrderByDescending(f => f.Type) : query.OrderBy(f => f.Type),
                    _ => query.OrderBy(f => f.Id)
                };

                var pagedItems = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var result = pagedItems.Select(f => new FoodAndDrinkResponseDto
                {
                    Name = f.Name,
                    Description = f.Description,
                    Price = f.Price,
                    Type = f.Type,
                    ImageUrl = f.ImageUrl,
                    IsAvailable = f.IsAvailable
                }).ToList();

                _loggerService.Success($"Retrieved {result.Count} food/drink items on page {page} successfully.");

                return new Pagination<FoodAndDrinkResponseDto>(result, totalItems, page, pageSize);
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Failed to retrieve food and drinks. Exception: {ex.Message}");
                throw new Exception("An error occurred while retrieving food and drink items. Please try again later.");
            }
        }

        public async Task<FoodAndDrinkResponseDto> AddFoodAndDrinkAsync(FoodAndDrinkRequestDto dto)
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
            };

            var adminId = _claimsService.GetCurrentUserId;

            var newData = new
            {
                foodAndDrink.Name,
                foodAndDrink.Description,
                foodAndDrink.Price,
                foodAndDrink.Type,
                foodAndDrink.ImageUrl,
                foodAndDrink.IsAvailable,
            };

            var changedFields = JsonSerializer.Serialize(new
            {
                foodAndDrink.Name,
                foodAndDrink.Description,
                foodAndDrink.Price,
                foodAndDrink.Type,
                foodAndDrink.ImageUrl,
                foodAndDrink.IsAvailable,
            });
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
            await _auditLogService.LogAsync
                    (
                    adminId,
                    AuditActionType.Create,
                    "FoodAndDrink",
                    foodAndDrink.Id,
                    null,
                    newData,
                    changedFields,
                    "Admin created new food and drink."
                    );
            _loggerService.Success($"[AddFoodAndDrinkAsync] Food and drink {foodAndDrink.Name} added successfully.");

            return new FoodAndDrinkResponseDto
            {
                Id = foodAndDrink.Id,
                Name = foodAndDrink.Name,
                Description = foodAndDrink.Description,
                Price = foodAndDrink.Price,
                Type = foodAndDrink.Type,
                ImageUrl = foodAndDrink.ImageUrl,
                IsAvailable = foodAndDrink.IsAvailable,
            };
        }

        private async Task<bool> FoodAndDrinkExistsAsync(string name)
        {
            var existingFoodAndDrink = await _unitOfWork.FoodAndDrinks.FirstOrDefaultAsync(f => f.Name == name);
            return existingFoodAndDrink != null;
        }

        public async Task<bool> DeleteFoodAndDrinkAsync(Guid foodAndDrinkId)
        {
            try
            {
                var foodAndDrink = await _unitOfWork.FoodAndDrinks.GetByIdAsync(foodAndDrinkId);
                if (foodAndDrink == null)
                {
                    _loggerService.Warn($"FoodAndDrink with ID {foodAndDrinkId} not found.");
                    return false;
                }

                var oldValue = new
                {
                    foodAndDrink.IsDeleted
                };
          
                await _unitOfWork.FoodAndDrinks.SoftRemove(foodAndDrink);
                await _unitOfWork.SaveChangesAsync();

                var newValue = new
                {
                    foodAndDrink.IsDeleted
                };

                var changedFields = JsonSerializer.Serialize(new
                {
                    foodAndDrink.IsDeleted
                });

                var adminId = _claimsService.GetCurrentUserId;

                await _auditLogService.LogAsync
                        (
                        adminId,
                        AuditActionType.Delete,
                        "FoodAndDrink",
                        foodAndDrinkId,
                        oldValue,
                        newValue,
                        changedFields,
                        "Admin deleted food and drink."
                        );

                _loggerService.Info($"Successfully deleted FoodAndDrink with ID {foodAndDrinkId}.");
                return true;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"An error occurred while deleting FoodAndDrink: {ex.Message}");
                return false;
            }
        }

    }
}