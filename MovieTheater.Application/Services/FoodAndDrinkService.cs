using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.FoodAndDrinkDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;
using System.Text.Json;

namespace MovieTheater.Application.Services
{
    public class FoodAndDrinkService : IFoodAndDrinkService
    {
        private readonly ILoggerService _loggerService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimsService _claimsService;
        private readonly IAuditLogService _auditLogService;
        private readonly IRedisService _redisService;

        public FoodAndDrinkService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService, IAuditLogService auditLogService, IRedisService redisService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
            _claimsService = claimsService;
            _auditLogService = auditLogService;
            _redisService = redisService;
        }

        public async Task<Pagination<FoodAndDrinkResponseDto>> GetAllFoodAndDrinkAsync(string? search, string? sortBy, bool isDescending, int page, int pageSize, FoodType? type = null)
        {
            try
            {
                _loggerService.Info($"Fetching food and drinks - Page {page}, PageSize {pageSize}, Search: {search}");

                string cacheKey = $"fooddrink:list:{search}:{sortBy}:{isDescending}:{page}:{pageSize}:{type}";
                var cached = await _redisService.GetAsync<Pagination<FoodAndDrinkResponseDto>>(cacheKey);
                if (cached != null) return cached;

                var foodAndDrinks = await _unitOfWork.FoodAndDrinks.GetAllAsync(f => !f.IsDeleted);
                var query = foodAndDrinks.AsQueryable();

                if (type.HasValue)
                    query = query.Where(f => f.Type == type.Value);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowerSearch = search.ToLower();
                    query = query.Where(f =>
                        (!string.IsNullOrEmpty(f.Name) && f.Name.ToLower().Contains(lowerSearch)) ||
                        (!string.IsNullOrEmpty(f.Description) && f.Description.ToLower().Contains(lowerSearch)) ||
                        f.Type.ToString().ToLower().Contains(lowerSearch)
                    );
                }

                var totalItems = await query.CountAsync();

                query = sortBy?.ToLower() switch
                {
                    "name" => isDescending ? query.OrderByDescending(f => f.Name) : query.OrderBy(f => f.Name),
                    "price" => isDescending ? query.OrderByDescending(f => f.Price) : query.OrderBy(f => f.Price),
                    "type" => isDescending ? query.OrderByDescending(f => f.Type) : query.OrderBy(f => f.Type),
                    _ => query.OrderBy(f => f.Id)
                };

                var pagedItems = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = pagedItems.Select(f => new FoodAndDrinkResponseDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Description = f.Description,
                    Price = f.Price,
                    Type = f.Type,
                    ImageUrl = f.ImageUrl,
                    IsAvailable = f.IsAvailable
                }).ToList();

                var response = new Pagination<FoodAndDrinkResponseDto>(result, totalItems, page, pageSize);
                await _redisService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));
                _loggerService.Success($"Retrieved {result.Count} food/drink items on page {page} successfully.");

                return response;
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
                await _redisService.RemoveByPatternAsync("fooddrink:list:");
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

        public async Task<FoodAndDrinkResponseDto> UpdateFoodAndDrinkAsync(Guid id, FoodAndDrinkRequestDto dto)
        {
            _loggerService.Info($"[UpdateFoodAndDrinkAsync] Start updating food and drink: {dto.Name}");

            var foodAndDrink = await _unitOfWork.FoodAndDrinks.GetByIdAsync(id);
            if (foodAndDrink == null || foodAndDrink.IsDeleted)
            {
                _loggerService.Warn($"[UpdateFoodAndDrinkAsync] Food or drink with ID {id} not found or deleted.");
                throw ErrorHelper.NotFound("Food or drink not found.");
            }

            var existing = await _unitOfWork.FoodAndDrinks.FirstOrDefaultAsync(
                f => f.Name == dto.Name && f.Id != id && !f.IsDeleted);
            if (existing != null)
            {
                _loggerService.Warn($"[UpdateFoodAndDrinkAsync] Food or drink with name '{dto.Name}' already exists.");
                throw ErrorHelper.Conflict("Food or drink with the same name already exists.");
            }

            var oldData = new
            {
                foodAndDrink.Name,
                foodAndDrink.Description,
                foodAndDrink.Price,
                foodAndDrink.Type,
                foodAndDrink.ImageUrl,
                foodAndDrink.IsAvailable
            };

            bool isUpdated = false;

            if (!string.IsNullOrWhiteSpace(dto.Name) && foodAndDrink.Name != dto.Name)
            {
                foodAndDrink.Name = dto.Name;
                isUpdated = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.Description) && foodAndDrink.Description != dto.Description)
            {
                foodAndDrink.Description = dto.Description;
                isUpdated = true;
            }

            if (dto.Price != foodAndDrink.Price)
            {
                foodAndDrink.Price = dto.Price;
                isUpdated = true;
            }

            if (dto.Type != foodAndDrink.Type)
            {
                foodAndDrink.Type = dto.Type;
                isUpdated = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.ImageUrl) && foodAndDrink.ImageUrl != dto.ImageUrl)
            {
                foodAndDrink.ImageUrl = dto.ImageUrl;
                isUpdated = true;
            }

            if (dto.IsAvailable != foodAndDrink.IsAvailable)
            {
                foodAndDrink.IsAvailable = dto.IsAvailable;
                isUpdated = true;
            }

            if (!isUpdated)
            {
                _loggerService.Warn($"[UpdateFoodAndDrinkAsync] No changes detected for FoodAndDrink ID: {id}");
                return new FoodAndDrinkResponseDto
                {
                    Id = foodAndDrink.Id,
                    Name = foodAndDrink.Name,
                    Description = foodAndDrink.Description,
                    Price = foodAndDrink.Price,
                    Type = foodAndDrink.Type,
                    ImageUrl = foodAndDrink.ImageUrl,
                    IsAvailable = foodAndDrink.IsAvailable
                };
            }

            await _unitOfWork.FoodAndDrinks.Update(foodAndDrink);
            await _unitOfWork.SaveChangesAsync();
            await _redisService.RemoveByPatternAsync("fooddrink:list:");

            var newData = new
            {
                foodAndDrink.Name,
                foodAndDrink.Description,
                foodAndDrink.Price,
                foodAndDrink.Type,
                foodAndDrink.ImageUrl,
                foodAndDrink.IsAvailable
            };

            var changedFields = JsonSerializer.Serialize(newData);
            var adminId = _claimsService.GetCurrentUserId;

            await _auditLogService.LogAsync(
                adminId,
                AuditActionType.Update,
                "FoodAndDrink",
                foodAndDrink.Id,
                oldData,
                newData,
                changedFields,
                "Admin updated food and drink."
            );

            _loggerService.Success($"[UpdateFoodAndDrinkAsync] Food and drink {foodAndDrink.Name} updated successfully.");

            return new FoodAndDrinkResponseDto
            {
                Id = foodAndDrink.Id,
                Name = foodAndDrink.Name,
                Description = foodAndDrink.Description,
                Price = foodAndDrink.Price,
                Type = foodAndDrink.Type,
                ImageUrl = foodAndDrink.ImageUrl,
                IsAvailable = foodAndDrink.IsAvailable
            };
        }

        public async Task<bool> DeleteFoodAndDrinkAsync(Guid foodAndDrinkId)
        {
            try
            {
                var foodAndDrink = await _unitOfWork.FoodAndDrinks.GetByIdAsync(foodAndDrinkId);
                if (foodAndDrink == null || foodAndDrink.IsDeleted)
                {
                    _loggerService.Warn($"FoodAndDrink with ID {foodAndDrinkId} not found or already deleted.");
                    return false;
                }

                var oldValue = new
                {
                    foodAndDrink.IsDeleted
                };

                await _unitOfWork.FoodAndDrinks.SoftRemove(foodAndDrink);
                await _unitOfWork.SaveChangesAsync();
                await _redisService.RemoveByPatternAsync("fooddrink:list:");

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

        private async Task<bool> FoodAndDrinkExistsAsync(string name)
        {
            var existingFoodAndDrink = await _unitOfWork.FoodAndDrinks.FirstOrDefaultAsync(f => f.Name == name && !f.IsDeleted);
            return existingFoodAndDrink != null;
        }

    }
}