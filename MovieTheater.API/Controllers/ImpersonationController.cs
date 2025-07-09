using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Infrastructure.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers
{
    [Route("api/admin/impersonation")]
    [ApiController]
    public class ImpersonationController : ControllerBase
    {
        private readonly IImpersonationService _impersonationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimsService _claimsService;

        public ImpersonationController(
            IImpersonationService impersonationService,
            IUnitOfWork unitOfWork,
            IClaimsService claimsService)
        {
            _impersonationService = impersonationService;
            _unitOfWork = unitOfWork;
            _claimsService = claimsService;
        }

        [HttpPost("start")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
            Summary = "Start impersonating a user", 
            Description = "Allows an admin to impersonate a target user by their ID and specify a reason.")]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> Start(
            [FromQuery, SwaggerParameter("ID of the user to impersonate", Required = true)] Guid targetUserId,
            [FromQuery, SwaggerParameter("Reason for impersonation", Required = true)] string reason)
        {
            try
            {
                var currentUserId = _claimsService.GetCurrentUserId;
                var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId);
                var result = await _impersonationService.StartImpersonationAsync(targetUserId, reason);
                if (result)
                    return Ok(ApiResult<object>.Success(null!, "200", "Impersonation started."));
                else
                    return BadRequest(ApiResult<object>.Failure("400", "Failed to impersonate."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPost("stop")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
            Summary = "Stop impersonation", 
            Description = "Stops the current impersonation session if one is active.")]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> Stop()
        {
            try
            {
                var result = await _impersonationService.StopImpersonationAsync();
                if (result)
                    return Ok(ApiResult<object>.Success(null, "200", "Impersonation stopped."));
                else
                    return BadRequest(ApiResult<object>.Failure("400", "Not impersonating."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpGet("status")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(
            Summary = "Get impersonation status", 
            Description = "Returns whether the admin is currently impersonating another user.")]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public IActionResult Status()
        {
            try
            {
                var status = new
                {
                    isImpersonating = _impersonationService.IsImpersonating(),
                    effectiveUserId = _impersonationService.GetEffectiveUserId(),
                    impersonatedBy = _impersonationService.GetImpersonatedBy()
                };
                return Ok(ApiResult<object>.Success(status, "200", "Impersonation status retrieved."));
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