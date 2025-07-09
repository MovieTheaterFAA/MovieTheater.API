using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain;
using MovieTheater.Domain.DTOs.AuditLogDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers;

[ApiController]
[Route("api/system-owner")]
public class SystemOwnerController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public SystemOwnerController(MovieTheaterDbContext context, ILoggerService logger, IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet("audit-logs")]
    [Authorize(Policy = "SystemOwnerPolicy")]
    [SwaggerOperation(Summary = "View audit logs", Description = "Get paginated list of audit logs with optional search and filters.")]
    [ProducesResponseType(typeof(ApiResult<Pagination<AuditLogDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> ViewAuditLogAsync(
          [FromQuery, SwaggerParameter(Description = "Search by entity type or admin name (optional)")] string? search,
          [FromQuery, SwaggerParameter(Description = "Filter by action type (optional)")] AuditActionType? actionType,
          [FromQuery, SwaggerParameter(Description = "Filter by entity type (optional)")] string? entityType,
          [FromQuery, SwaggerParameter(Description = "Sort descending by timestamp? Default: false")] bool isDescending = false,
          [FromQuery, SwaggerParameter(Description = "Page number, starts at 1")] int page = 1,
          [FromQuery, SwaggerParameter(Description = "Items per page")] int pageSize = 10)
    {
        try
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(ApiResult<object>.Failure("400", "Invalid pagination parameters"));

            var logs = await _auditLogService.ViewLogAsync(search, actionType, entityType, isDescending, page, pageSize);

            return Ok(ApiResult<Pagination<AuditLogDto>>.Success(logs, "200", "Retrieved audit logs successfully"));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}