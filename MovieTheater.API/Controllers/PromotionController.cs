using Microsoft.AspNetCore.Authorization;
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

        [HttpGet("{id}")]
        [AllowAnonymous]
        [SwaggerOperation(
        Summary = "Get promotion by ID",
        Description = "Returns a promotion by its ID."
        )]
        [ProducesResponseType(typeof(ApiResult<PromotionResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 404)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetPromotionAsync([FromRoute] Guid id)
        {
            try
            {
                var result = await _promotionService.GetPromotionAsync(id);
                if (result == null)
                    return NotFound(ApiResult<object>.Failure("404", "Promotion not found."));
                return Ok(ApiResult<PromotionResponseDto>.Success(result, "200", "Promotion retrieved successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [SwaggerOperation(
            Summary = "Get all promotions",
            Description = "Returns all available promotions."
        )]
        [ProducesResponseType(typeof(ApiResult<IEnumerable<PromotionResponseDto>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetAllPromotionsAsync()
        {
            try
            {
                var result = await _promotionService.GetAllPromotionsAsync();
                return Ok(ApiResult<IEnumerable<PromotionResponseDto>>.Success(result, "200", "Promotions retrieved successfully."));
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

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
            Summary = "Update a promotion",
            Description = "Update an existing promotion. Requires Admin privileges.")]
        [ProducesResponseType(typeof(ApiResult<PromotionResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> UpdatePromotionAsync([FromRoute] Guid id, [FromBody] PromotionUpdateDto dto)
        {
            try
            {

                var result = await _promotionService.UpdatePromotionAsync(id, dto);
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


        [HttpPost("{id}/claim")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Claim a promotion",
            Description = "User claims a promotion. Returns false if already claimed.")]
        [ProducesResponseType(typeof(ApiResult<bool>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> ClaimPromotion([FromRoute] Guid id)
        {
            try
            {
                var userId = _claimsService.GetCurrentUserId;
                var result = await _promotionService.ClaimPromotionAsync(id, userId);
                if (!result)
                    return BadRequest(ApiResult<object>.Failure("400", "Promotion already claimed or not found."));
                return Ok(ApiResult<bool>.Success(true, "200", "Promotion claimed successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPost("{promotionId}/claim-for/{userId}")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
        Summary = "Admin claims a promotion for a member",
        Description = "Admin claims a promotion for a specific user (member)."
        )]
        [ProducesResponseType(typeof(ApiResult<bool>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> AdminClaimPromotionForMember([FromRoute] Guid promotionId, [FromRoute] Guid userId)
        {
            try
            {
                var result = await _promotionService.ClaimPromotionAsync(promotionId, userId);
                if (!result)
                    return BadRequest(ApiResult<object>.Failure("400", "Promotion already claimed or not found for this user."));
                return Ok(ApiResult<bool>.Success(true, "200", "Promotion claimed for member successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPost("{id}/use")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Use a claimed promotion",
            Description = "User marks a claimed promotion as used. Returns false if not claimed or already used.")]
        [ProducesResponseType(typeof(ApiResult<bool>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> UseClaimedPromotion([FromRoute] Guid id)
        {
            try
            {
                var userId = _claimsService.GetCurrentUserId;
                var result = await _promotionService.UseClaimedPromotionAsync(id, userId);
                if (!result)
                    return BadRequest(ApiResult<object>.Failure("400", "Promotion not claimed or already used."));
                return Ok(ApiResult<bool>.Success(true, "200", "Promotion used successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpGet("claimed")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Get all promotions claimed by the user",
            Description = "Returns a list of promotions claimed by the current user.")]
        [ProducesResponseType(typeof(ApiResult<IEnumerable<PromotionResponseDto>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetClaimedPromotionsByUser()
        {
            try
            {
                var userId = _claimsService.GetCurrentUserId;
                var result = await _promotionService.GetClaimedPromotionsByUserAsync(userId);
                return Ok(ApiResult<IEnumerable<PromotionResponseDto>>.Success(result, "200", "Claimed promotions retrieved successfully."));
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