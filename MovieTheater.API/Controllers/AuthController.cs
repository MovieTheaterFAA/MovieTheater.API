using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.AuthenDTOs;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Infrastructure.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

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

        /// <summary>
        /// Register a new user account.
        /// </summary>
        /// <param name="userDto">User registration data.</param>
        /// <returns>Registered user information.</returns>
        [HttpPost("register")]
        [SwaggerOperation(
            Summary = "Register a new user",
            Description = "Creates a new user account with the provided registration information."
        )]
        [ProducesResponseType(typeof(ApiResult<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<UserDto>), 400)]
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

        /// <summary>
        /// User login.
        /// </summary>
        /// <param name="dto">Login credentials.</param>
        /// <returns>JWT access and refresh tokens.</returns>
        [HttpPost("login")]
        [SwaggerOperation(
            Summary = "User login",
            Description = "Authenticate user and return JWT tokens."
        )]
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

        /// <summary>
        /// Logout the current user.
        /// </summary>
        /// <returns>Logout result.</returns>
        [HttpPost("logout")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Logout user",
            Description = "Logs out the currently authenticated user."
        )]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = _claimsService.GetCurrentUserId;
                var result = await _authService.LogoutAsync(userId);
                return Ok(ApiResult<object>.Success(result!, "200", "Loged out successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        /// <summary>
        /// Reset user password using OTP.
        /// </summary>
        /// <param name="dto">Reset password data.</param>
        /// <returns>Password reset result.</returns>
        [HttpPost("reset-password")]
        [SwaggerOperation(
            Summary = "Reset password",
            Description = "Reset user password using OTP sent to email."
        )]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            try
            {
                var reset = await _authService.ResetPasswordAsync(dto.Email, dto.Otp, dto.NewPassword);
                if (!reset) return BadRequest(ApiResult.Failure("400", "OTP is invalid, expired or data is invalid."));
                return Ok(ApiResult.Success("200", "Password was reset successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        /// <summary>
        /// Refresh JWT access token using a valid refresh token.
        /// </summary>
        /// <param name="requestToken">Refresh token data.</param>
        /// <returns>New JWT tokens.</returns>
        [HttpPost("refresh-token")]
        [SwaggerOperation(
            Summary = "Refresh JWT token",
            Description = "Refresh JWT access token using a valid refresh token."
        )]
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

        /// <summary>
        /// Resend OTP to user's email for registration or password reset.
        /// </summary>
        /// <param name="dto">Resend OTP request data.</param>
        /// <returns>OTP resend result.</returns>
        [HttpPost("resend-otp")]
        [SwaggerOperation(
            Summary = "Resend OTP",
            Description = "Resend OTP to user's email for registration or password reset."
        )]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequestDto dto)
        {
            try
            {
                var success = await _authService.ResendOtpAsync(dto.Email, dto.Purpose);
                if (!success)
                {
                    return BadRequest(ApiResult.Failure("400", "Failed to resend OTP. Possibly invalid state or email."));
                }

                return Ok(ApiResult.Success("200", "OTP resent successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        /// <summary>
        /// Verify OTP for email confirmation.
        /// </summary>
        /// <param name="dto">OTP verification data.</param>
        /// <returns>Verification result.</returns>
        [HttpPost("verify-otp")]
        [SwaggerOperation(
            Summary = "Verify OTP",
            Description = "Verify OTP for email confirmation and activate account."
        )]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            try
            {
                var verified = await _authService.VerifyEmailOtpAsync(dto.Email, dto.Otp);
                if (!verified)
                    return BadRequest(ApiResult.Failure("400", "OTP is invalid or expired."));
                return Ok(ApiResult.Success("200", "Verification successful. Account activated."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        /// <summary>
        /// Employee creates a new customer account.
        /// </summary>
        /// <param name="customer">Customer registration data.</param>
        /// <returns>Created customer information.</returns>
        [HttpPost("add-offline-customer")]
        [Authorize(Roles = "Employee")]
        [SwaggerOperation(
            Summary = "Employee creates a new customer",
            Description = "Allows an employee to create a new customer account."
        )]
        [ProducesResponseType(typeof(ApiResult<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<UserDto>), 400)]
        public async Task<IActionResult> EmployeeCreateCustomer([FromBody, SwaggerParameter("Customer registration data")] AddCustomerDto customer)
        {
            try
            {
                var employeeId = _claimsService.GetCurrentUserId;
                var result = await _authService.EmployeeCreateCustomerAsync(customer, employeeId);
                return Ok(ApiResult<UserDto>.Success(result, "200", "Customer account created successfully."));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<UserDto>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }
    }
}