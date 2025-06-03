using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.AdminDTOs;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers;

// test
[Route("api/admin")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IClaimsService _claimsService;

    public AdminController(IAdminService adminService, IClaimsService claimsService)
    {
        _adminService = adminService;
        _claimsService = claimsService;
    }

    [HttpGet("users")]
    [SwaggerOperation(Summary = "Get all users", Description = "Get paginated list of users with optional filters.")]
    [SwaggerResponse(200, "Users retrieved successfully", typeof(ApiResult<Pagination<GetUserDto>>))]
    [SwaggerResponse(400, "Bad request", typeof(ApiResult<object>))]
    [SwaggerResponse(500, "Internal server error", typeof(ApiResult<object>))]
    public async Task<IActionResult> GetAllUserAsync(
             [FromQuery, SwaggerParameter(Description = "Search by name or email (optional)")] string? search,
             [FromQuery, SwaggerParameter(Description = "Filter by user role (optional)")] RoleType? role,
             [FromQuery, SwaggerParameter(Description = "Sort by field: ScoreBalance, CreatedAt (optional)")] string? sortBy,
             [FromQuery, SwaggerParameter(Description = "Sort descending? Default: false")] bool isDescending = false,
             [FromQuery, SwaggerParameter(Description = "Page number, starts at 1")] int page = 1,
             [FromQuery, SwaggerParameter(Description = "Items per page")] int pageSize = 10)
    {
        try
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(ApiResult<object>.Failure("400", "Invalid pagination parameter"));

            var users = await _adminService.GetListUserAsync(search, role, sortBy, isDescending, page, pageSize);

            return Ok(ApiResult<Pagination<GetUserDto>>.Success(users, "200", "Get user succesfully"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpGet("employees")]
    [ProducesResponseType(typeof(ApiResult<Pagination<UserDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetAllEmployeeAsync(
         [FromQuery] string? search,
         [FromQuery] string? sortBy,
         [FromQuery] bool isDescending = false,
         [FromQuery] int page = 1,
         [FromQuery] int pageSize = 10)
    {
        try
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(ApiResult<object>.Failure("400", " Invalid pagination parameter"));

            var users = await _adminService.GetListEmployeeAsync(search, sortBy, isDescending, page, pageSize);

            return Ok(ApiResult<object>.Success(users, "200", "Get user succesfully"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpPost("employee")]
    [Authorize(Policy = "AdminPolicy")]
    [SwaggerOperation(Summary = "Add new employee")]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    public async Task<IActionResult> AddEmployeeAsync([FromBody] AddEmployeeRequestDto addEmployee)
    {
        try
        {
            var result = await _adminService.AddEmployeeAsync(addEmployee);
            return Ok(ApiResult<UserDto>.Success(result!, "200", "Added employee successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<UserDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpPut("employee/{id}")]
    [Authorize(Policy = "AdminPolicy")]
    [SwaggerOperation(Summary = "Update employee information")]
    [ProducesResponseType(typeof(ApiResult<EditEmployeeDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    public async Task<IActionResult> EditEmployeeAsync(Guid id, [FromBody] EditEmployeeDto dto)
    {
        if (id == Guid.Empty)
            return BadRequest(ApiResult<object>.Failure("400", "Invalid update request. User ID is required."));

        try
        {
            var adminId = _claimsService.GetCurrentUserId;

            var result = await _adminService.EditEmployeeAsync(id, dto);

            if (result == null)
                return BadRequest(ApiResult<object>.Failure("400", "Update failed. No user found with the provided ID."));

            return Ok(ApiResult<EditEmployeeDto>.Success(result, "200", "User updated successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<object>.Failure("400", ex.Message));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<EditEmployeeDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpDelete("employee/{id}")]
    [Authorize(Policy = "AdminPolicy")]
    [SwaggerOperation(Summary = "Delete employee")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        if (id == Guid.Empty)
            return BadRequest(ApiResult<object>.Failure("400", "Invalid delete request. User ID is required."));

        try
        {
            var adminId = _claimsService.GetCurrentUserId;
            var result = await _adminService.DeleteEmployeeAsync(id, adminId);

            if (!result)
                return BadRequest(ApiResult<object>.Failure("400", "Delete failed. No user found with the provided ID."));

            return Ok(ApiResult<object>.Success(result, "200", "User deleted successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<UserDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

}