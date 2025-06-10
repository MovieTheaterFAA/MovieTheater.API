using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
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

        public PromotionController(IPromotionService promotionService, IClaimsService claimsService)
        {
            _promotionService = promotionService;
            _claimsService = claimsService;
        }

        // API to add a new promotion
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

    }
}
