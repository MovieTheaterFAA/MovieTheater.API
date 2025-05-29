using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Services;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;

namespace MovieTheater.API.Controllers;

[Route("api/admin")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("get-user")]
    [ProducesResponseType(typeof(ApiResult<Pagination<GetUserDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetAllUserAsync(
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
                return BadRequest(ApiResult<object>.Failure("400", "Invalid pagination parameter"));

            var users = await _adminService.GetListUsersAsync(search, role, sortBy, isDescending, page, pageSize);

            return Ok(ApiResult<Pagination<GetUserDto>>.Success(users, "200", "Get user succesfully"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}