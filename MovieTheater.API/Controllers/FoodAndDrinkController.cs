using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.FoodAndDrinkDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers
{
    [Route("api/food-and-drinks")]
    [ApiController]
    public class FoodAndDrinkController : ControllerBase
    {
        private readonly IFoodAndDrinkService _foodAndDrinkService;

        public FoodAndDrinkController(IFoodAndDrinkService foodAndDrinkService)
        {
            _foodAndDrinkService = foodAndDrinkService;
        }
        [HttpGet]
        [Authorize]
        [SwaggerOperation(
            Summary = "Get all food and drinks",
            Description = "Get paginated list of food and drinks with optional filters.")]
        [ProducesResponseType(typeof(ApiResult<Pagination<FoodAndDrinkResponseDto>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetAllFoodAndDrinksAsync(
        [FromQuery, SwaggerParameter(Description = "Search by food or drink name (optional)")] string? search,
        [FromQuery, SwaggerParameter(Description = "Sort by field: Name, Price, Type (optional)")] string? sortBy,
        [FromQuery, SwaggerParameter(Description = "Sort descending? Default: false")] bool isDescending = false,
        [FromQuery, SwaggerParameter(Description = "Page number, starts at 1")] int page = 1,
        [FromQuery, SwaggerParameter(Description = "Items per page")] int pageSize = 10,
        [FromQuery, SwaggerParameter(Description = "Filter by food type")] FoodType? type = null)
        {
            try
            {
                if (page < 1 || pageSize < 1)
                    return BadRequest(ApiResult<object>.Failure("400", "Invalid pagination parameters"));

                var result = await _foodAndDrinkService.GetAllFoodAndDrinkAsync(search, sortBy, isDescending, page, pageSize, type);

                return Ok(ApiResult<Pagination<FoodAndDrinkResponseDto>>.Success(result, "200", "Get food and drinks successfully"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
            Summary = "Update food or drink",
            Description = "Update food or drink item by its ID. Requires Admin privileges.")]
        [ProducesResponseType(typeof(ApiResult<FoodAndDrinkResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> UpdateFoodAndDrinkAsync(Guid id, [FromBody] FoodAndDrinkRequestDto dto)
        {
            try
            {
                var result = await _foodAndDrinkService.UpdateFoodAndDrinkAsync(id, dto);
                return Ok(ApiResult<FoodAndDrinkResponseDto>.Success(result, "200", "Food or drink updated successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPost]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
            Summary = "Add a new food and drink item",
            Description = "Creates a new food and drink item with the provided information. Requires Admin privileges.")]
        [ProducesResponseType(typeof(ApiResult<FoodAndDrinkResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> AddFoodAndDrinkAsync([FromBody] FoodAndDrinkRequestDto foodAndDrinkDto)
        {
            try
            {
                var result = await _foodAndDrinkService.AddFoodAndDrinkAsync(foodAndDrinkDto);
                return Ok(ApiResult<FoodAndDrinkResponseDto>.Success(result!, "200", "Added food and drink item successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<FoodAndDrinkResponseDto>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
            Summary = "Delete food or drink",
            Description = "Delete food or drink item by its ID.")]
        [ProducesResponseType(typeof(ApiResult<bool>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> DeleteFoodAndDrink(Guid id)
        {
            try
            {
                var result = await _foodAndDrinkService.DeleteFoodAndDrinkAsync(id);
                return Ok(ApiResult<bool>.Success(result, "200", "Food or drink deleted successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

    }
}