using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.API.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IClaimsService _claimsService;
        private readonly ILoggerService _loggerService;
        public UserController(IUserService userService, IClaimsService claimsService, ILoggerService loggerService)
        {
            _userService = userService;
            _claimsService = claimsService;
            _loggerService = loggerService;
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
                var currentUserId = _claimsService.GetCurrentUserId;
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

                var currentUserId = _claimsService.GetCurrentUserId;
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
