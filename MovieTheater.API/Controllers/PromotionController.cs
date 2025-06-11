using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.PromotionDTOs;
using MovieTheater.Infrastructure.Commons;
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
    }
}