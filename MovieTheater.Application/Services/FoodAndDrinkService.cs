using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.FoodAndDrinkDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Infrastructure.Commons;
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

        public async Task<Pagination<FoodAndDrinkResponseDto>> GetAllFoodAndDrinkAsync(string? search, string? sortBy, bool isDescending, int page, int pageSize)
        {
            try
            {
                _loggerService.Info($"Fetching food and drinks - Page {page}, PageSize {pageSize}, Search: {search}");

                var foodAndDrinks = await _unitOfWork.FoodAndDrink.GetAllAsync();

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
    }
}

