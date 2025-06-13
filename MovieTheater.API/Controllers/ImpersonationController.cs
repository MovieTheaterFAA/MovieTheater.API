using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.API.Controllers
{
    [Route("api/admin/impersonation")]
    [ApiController]
    public class ImpersonationController : ControllerBase
    {
        private readonly IImpersonationService _impersonationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimsService _claimsService;

        public ImpersonationController(
       IImpersonationService impersonationService,
       IUnitOfWork unitOfWork,
       IClaimsService claimsService)
        {
            _impersonationService = impersonationService;
            _unitOfWork = unitOfWork;
            _claimsService = claimsService;
        }
        [HttpPost("start")]
        public async Task<IActionResult> Start([FromQuery] Guid targetUserId, [FromQuery] string reason)
        {
            var currentUserId = _claimsService.GetCurrentUserId;
            var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId);

            if (currentUser == null || currentUser.Role != RoleType.Admin)
                return Forbid("Only admins can impersonate other users.");

            var result = await _impersonationService.StartImpersonationAsync(targetUserId, reason);
            return result ? Ok("Impersonation started.") : BadRequest("Failed to impersonate.");
        }

        [HttpPost("stop")]
        public async Task<IActionResult> Stop()
        {
            var result = await _impersonationService.StopImpersonationAsync();
            return result ? Ok("Impersonation stopped.") : BadRequest("Not impersonating.");
        }

        [HttpGet("status")]
        public IActionResult Status()
        {
            return Ok(new
            {
                isImpersonating = _impersonationService.IsImpersonating(),
                effectiveUserId = _impersonationService.GetEffectiveUserId(),
                impersonatedBy = _impersonationService.GetImpersonatedBy()
            });
        }
    }
}
