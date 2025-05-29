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

        [HttpGet("GetAllUsers")]
        [ProducesResponseType(typeof(ApiResult<Pagination<UserForListDto>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetAllUserPagingAsyns(
            [FromQuery] string? search,
            [FromQuery] RoleType? role,
            [FromQuery] string? sortBy,
            [FromQuery] bool isDescending = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (page < 1 || pageSize < 1)
                    return BadRequest(ApiResult<object>.Failure("400 - Invalid pagination parameter"));

                _loggerService.Info("Received request to get user list.");

                var users = await _userService.GetListUsersAsyns(search, role, sortBy, isDescending, page, pageSize);

                _loggerService.Success($"Fetched {users.Count} users successfully.");

                return Ok(ApiResult<Pagination<UserForListDto>>.Success(users, "200", "Succesfully"));

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
