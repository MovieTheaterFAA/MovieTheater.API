using MovieTheater.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.FoodAndDrinkDTOs;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers
{
    [Route("api/foodanddrink")]
    [ApiController]
    public class FoodAndDrinkController : ControllerBase
    {
        private readonly IFoodAndDrinkService _foodAndDrinkService;
        private readonly IClaimsService _claimsService;
        private readonly ILoggerService _loggerService;

        public FoodAndDrinkController(IFoodAndDrinkService foodAndDrinkService, IClaimsService claimsService, ILoggerService loggerService)
        {
            _foodAndDrinkService = foodAndDrinkService;
            _claimsService = claimsService;
            _loggerService = loggerService;
        }
        [HttpGet()]
        [Authorize]
        [SwaggerOperation(Summary = "Get all food and drinks", Description = "Get paginated list of food and drinks with optional filters.")]
        [ProducesResponseType(typeof(ApiResult<Pagination<FoodAndDrinkResponseDto>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetAllFoodAndDrinksAsync(
        [FromQuery, SwaggerParameter(Description = "Search by food or drink name (optional)")] string? search,
        [FromQuery, SwaggerParameter(Description = "Sort by field: Name, Price, Type (optional)")] string? sortBy,
        [FromQuery, SwaggerParameter(Description = "Sort descending? Default: false")] bool isDescending = false,
        [FromQuery, SwaggerParameter(Description = "Page number, starts at 1")] int page = 1,
        [FromQuery, SwaggerParameter(Description = "Items per page")] int pageSize = 10)
        {
            try
            {
                if (page < 1 || pageSize < 1)
                    return BadRequest(ApiResult<object>.Failure("400", "Invalid pagination parameters"));

                var result = await _foodAndDrinkService.GetAllFoodAndDrinkAsync(search, sortBy, isDescending, page, pageSize);

                return Ok(ApiResult<Pagination<FoodAndDrinkResponseDto>>.Success(result, "200", "Get food and drinks successfully"));
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
