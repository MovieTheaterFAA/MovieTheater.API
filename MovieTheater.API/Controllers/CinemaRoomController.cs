using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.CinemaRoomDTOs;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers
{
    [Route("api/cinema-room")]
    [ApiController]
    public class CinemaRoomController : ControllerBase
    {
        private readonly ICinemaRoomService _service;
        private readonly IClaimsService _claimsService;

        public CinemaRoomController(ICinemaRoomService service, IClaimsService claimsService)
        {
            _service = service;
            _claimsService = claimsService;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "List all cinema rooms")]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? sortBy, [FromQuery] bool isDescending = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _service.GetAllCinemaRoomAsync(search, sortBy, isDescending, page, pageSize);
                return Ok(ApiResult<Pagination<CinemaRoomDto>>.Success(result, "200", "Fetched cinema rooms successfully"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get cinema room details")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var room = await _service.GetCinemaRoomByIdAsync(id);
                if (room == null)
                    return NotFound(ApiResult<object>.Failure("404", "Cinema room not found"));
                return Ok(ApiResult<CinemaRoomDto>.Success(room, "200", "Fetched cinema room successfully"));
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
        [SwaggerOperation(Summary = "Create new cinema room")]
        public async Task<IActionResult> Create([FromBody] CreateCinemaRoomDto dto)
        {
            try
            {
                var adminId = _claimsService.GetCurrentUserId;
                var result = await _service.CreateCinemaRoomAsync(dto, adminId);
                return Ok(ApiResult<CinemaRoomDto>.Success(result, "200", "Created cinema room successfully"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(Summary = "Update cinema room")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCinemaRoomDto dto)
        {
            try
            {
                var adminId = _claimsService.GetCurrentUserId;
                var result = await _service.UpdateCinemaRoomAsync(id, dto, adminId);
                if (result == null)
                    return NotFound(ApiResult<object>.Failure("404", "Cinema room not found"));
                return Ok(ApiResult<CinemaRoomDto>.Success(result, "200", "Updated cinema room successfully"));
            }
            catch (Exception ex)
            {
                var statusCode = ExceptionUtils.ExtractStatusCode(ex);
                var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
                return StatusCode(statusCode, errorResponse);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminPolicy")]
        [SwaggerOperation(Summary = "Soft delete cinema room")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var adminId = _claimsService.GetCurrentUserId;
                var success = await _service.SoftDeleteCinemaRoomAsync(id, adminId);
                if (!success)
                    return NotFound(ApiResult<object>.Failure("404", "Cinema room not found"));
                return Ok(ApiResult<object>.Success(null!, "200", "Deleted cinema room successfully"));
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