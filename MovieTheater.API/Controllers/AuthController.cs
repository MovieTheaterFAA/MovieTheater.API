using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.AuthenDTOs;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IClaimsService _claimsService;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, IClaimsService claimsService, IConfiguration configuration)
        {
            _authService = authService;
            _claimsService = claimsService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResult<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<UserDto>), 409)]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto userDto)
        {
            try
            {
                var result = await _authService.RegisterUserAsync(userDto);
                return Ok(ApiResult<UserDto>.Success(result!, "200", "Registered successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<UserDto>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResult<LoginResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<LoginResponseDto>), 400)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            try
            {
                var result = await _authService.LoginAsync(dto, _configuration);
                return Ok(ApiResult<LoginResponseDto>.Success(result!, "200", "Login successful."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<LoginResponseDto>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = _claimsService.GetCurrentUserId;
                var result = await _authService.LogoutAsync(userId);
                return Ok(ApiResult<object>.Success(result!, "200", "Loged out successfully. "));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(ApiResult<LoginResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 401)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRefreshRequestDto requestToken)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(requestToken, _configuration);
                return Ok(ApiResult<object>.Success(result!, "200", "Refresh Token successfully"));
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
