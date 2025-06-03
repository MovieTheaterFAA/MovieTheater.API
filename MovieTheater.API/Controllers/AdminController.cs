using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Services;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.AdminDTOs;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.API.Controllers;

[Route("api/admin")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IClaimsService _claimsService;

    public AdminController(IAdminService adminService,IClaimsService claimsService)
    {
        _adminService = adminService;
        _claimsService = claimsService;
    }

    [HttpGet("users")]
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
    [ProducesResponseType(typeof(ApiResult<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 400)]
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
    [ProducesResponseType(typeof(ApiResult<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 400)]
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
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        if (id == Guid.Empty)
            return BadRequest(ApiResult<object>.Failure("400", "Invalid delete request. User ID is required."));

        try
        {
            var adminId =  _claimsService.GetCurrentUserId;
            var result = await _adminService.DeleteEmployeeAsync(id,adminId);

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