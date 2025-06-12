using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.PromotionDTOs;
using MovieTheater.Infrastructure.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers
{
    [Route("api/promotions")]
    [ApiController]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionService _promotionService;
        private readonly IClaimsService _claimsService;
        private readonly ILoggerService _loggerService;

        public PromotionController(IPromotionService promotionService, IClaimsService claimsService, ILoggerService loggerService)
        {
            _promotionService = promotionService;
            _claimsService = claimsService;
            _loggerService = loggerService;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get all promotions", Description = "Retrieve full list of all promotions without filtering.")]
        [ProducesResponseType(typeof(ApiResult<List<PromotionResponseDto>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetAllPromotionsAsync()
        {
            try
            {
                var promotions = await _promotionService.GetAllPromotionListAsync();
                return Ok(ApiResult<List<PromotionResponseDto>>.Success(promotions, "200", "Retrieved promotions successfully"));
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
            Summary = "Add a new promotion",
            Description = "Creates a new promotion with the provided information. Requires Admin privileges.")]
        [ProducesResponseType(typeof(ApiResult<PromotionResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> AddPromotionAsync([FromBody] PromotionRequestDto promotionDto)
        {
            try
            {
                var result = await _promotionService.AddPromotionAsync(promotionDto);
                return Ok(ApiResult<PromotionResponseDto>.Success(result!, "200", "Added promotion successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<PromotionResponseDto>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPut("{Id}")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
            Summary = "Update a promotion",
            Description = "Update an existing promotion. Requires Admin privileges.")]
        [ProducesResponseType(typeof(ApiResult<PromotionResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> UpdatePromotionAsync([FromRoute] Guid Id, [FromBody] PromotionUpdateDto dto)
        {
            try
            {

                var result = await _promotionService.UpdatePromotionAsync(Id, dto);
                return Ok(ApiResult<PromotionResponseDto>.Success(result!, "200", "Updated promotion successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<PromotionResponseDto>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
            Summary = "Delete promotion",
            Description = "Delete a promotion by its ID.")]
        [ProducesResponseType(typeof(ApiResult<bool>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> DeletePromotion(Guid id)
        {
            try
            {
                var result = await _promotionService.DeletePromotionAsync(id);
                if (!result)
                {
                    return BadRequest(ApiResult<object>.Failure("400", "Promotion not found or could not be deleted."));
                }

                return Ok(ApiResult<bool>.Success(true, "200", "Promotion deleted successfully."));
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