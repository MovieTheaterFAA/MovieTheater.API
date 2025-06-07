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
    [Route("api/promotion")]
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

        [HttpPost()]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
            Summary = "Add a new promotion",
            Description = "Creates a new promotion with the provided information. Requires Admin privileges."
        )]
        [ProducesResponseType(typeof(ApiResult<PromotionResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> AddPromotionAsync([FromBody, SwaggerParameter("New promotion data to be added")] PromotionRequestDto promotionRequestDto)
        {
            try
            {
                // Call the service to add promotion
                var result = await _promotionService.AddPromotionAsync(promotionRequestDto);

                // Return success response with the added promotion
                return Ok(ApiResult<PromotionResponseDto>.Success(result, "200", "Added promotion successfully."));
            }
            catch (Exception ex)
            {
                // Handle any exception and return an error response
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<PromotionResponseDto>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }
    }
}
