using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.UserDTOs;

namespace MovieTheater.API.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IImpersonationService _impersonationService;
        public UserController(IUserService userService, IImpersonationService impersonationService)
        {
            _userService = userService;
            _impersonationService = impersonationService;
        }
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetUserProfile()
        {
            try
            {
                var currentUserId = _impersonationService.GetEffectiveUserId();
                var currentUser = await _userService.GetUserDetails(currentUserId);

                var result = ApiResult<object>.Success(currentUser, "200", "User profile retrieved successfully.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPut("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UserUpdateDto userUpdateDto)
        {
            try
            {
                if (userUpdateDto == null)
                {
                    return BadRequest(ApiResult<object>.Failure("400", "User update data is required."));
                }

                var currentUserId = _impersonationService.GetEffectiveUserId();
                if (currentUserId == Guid.Empty)
                {
                    return BadRequest(ApiResult<object>.Failure("400", "Invalid or missing user ID."));
                }

                var updatedUser = await _userService.UpdateUserInfo(currentUserId, userUpdateDto);

                return Ok(ApiResult<UserUpdateDto>.Success(updatedUser, "200", "User profile updated successfully."));

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
