using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain;
using MovieTheater.Domain.DTOs.AuditLogDTOs;
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

    [HttpGet]
    [Authorize(Policy = "AdminPolicy")]
    [SwaggerOperation(Summary = "View all audit logs", Description = "Get all logs of admin action from the database.")]
    [ProducesResponseType(typeof(ApiResult<List<AuditLogDto>>), 200)]
    public async Task<IActionResult> ViewLogAsync()
    {
        var logs = await _auditLogService.ViewLogAsync();
        return Ok(ApiResult<List<AuditLogDto>>.Success(logs, "200", "Audit logs retrieved successfully."));
    }
}