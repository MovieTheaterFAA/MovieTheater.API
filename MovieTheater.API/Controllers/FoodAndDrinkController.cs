using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.FoodAndDrinkDTOs;
using MovieTheater.Infrastructure.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers
{
    [Route("api/food-and-drinks")]
    [ApiController]
    public class FoodAndDrinkController : ControllerBase
    {
        private readonly IFoodAndDrinkService _foodAndDrinkService;
        private readonly IClaimsService _claimsService;

        public FoodAndDrinkController(IFoodAndDrinkService foodAndDrinkService, IClaimsService claimsService)
        {
            _foodAndDrinkService = foodAndDrinkService;
            _claimsService = claimsService;
        }
        // API to add a new food and drink item
        [HttpPost]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
            Summary = "Add a new food and drink item",
            Description = "Creates a new food and drink item with the provided information. Requires Admin privileges.")]
        [ProducesResponseType(typeof(ApiResult<FoodAndDrinkResponseDTO>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> AddFoodAndDrinkAsync([FromBody] FoodAndDrinkRequestDto foodAndDrinkDto)
        {
            try
            {
                var result = await _foodAndDrinkService.AddFoodAndDrinkAsync(foodAndDrinkDto);
                return Ok(ApiResult<FoodAndDrinkResponseDTO>.Success(result!, "200", "Added food and drink item successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<FoodAndDrinkResponseDTO>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }
    }
}
