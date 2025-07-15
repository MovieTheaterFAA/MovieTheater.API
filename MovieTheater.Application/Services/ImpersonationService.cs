using Microsoft.AspNetCore.Http;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure;
using MovieTheater.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Application.Services
{
    public class ImpersonationService : IImpersonationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _logger;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IClaimsService _claimsService;

        public ImpersonationService(
           IUnitOfWork unitOfWork,
           ILoggerService logger,
           IAuditLogService auditLogService,
           IHttpContextAccessor httpContext,
           IClaimsService claimsService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _auditLogService = auditLogService;
            _httpContext = httpContext;
            _claimsService = claimsService;
        }

        public Guid GetEffectiveUserId()
        {
            var session = _httpContext.HttpContext?.Session;
            var isImpersonating = session?.GetString("IsImpersonating") == "true";

            if (isImpersonating)
            {
                var idStr = session?.GetString("Id");
                if (Guid.TryParse(idStr, out var impersonatedId))
                    return impersonatedId;
            }

            return _claimsService.GetCurrentUserId;
        }

        public Guid? GetImpersonatedBy()
        {
            if (!IsImpersonating()) return null;

            var adminIdStr = _httpContext.HttpContext?.Session?.GetString("AdminIdOriginal");
            return Guid.TryParse(adminIdStr, out var adminId) ? adminId : null;
        }

        public bool IsImpersonating()
        {
            return _httpContext.HttpContext?.Session?.GetString("IsImpersonating") == "true";
        }

        public async Task<bool> StartImpersonationAsync(Guid targetUserId, string reason)
        {
            var adminId = _claimsService.GetCurrentUserId;
            var admin = await _unitOfWork.Users.GetByIdAsync(adminId);
            var targetUser = await _unitOfWork.Users.GetByIdAsync(targetUserId);

            if (admin == null || admin.Role != RoleType.Admin)
                throw ErrorHelper.Forbidden("Only admins can impersonate users.");

            if (targetUser == null)
                throw ErrorHelper.NotFound("Target user not found.");

            var session = _httpContext.HttpContext!.Session;
            session.SetString("IsImpersonating", "true");
            session.SetString("AdminIdOriginal", admin.Id.ToString());
            session.SetString("Id", targetUser.Id.ToString());

            await _auditLogService.LogAsync(admin.Id, AuditActionType.Impersonate, "User", targetUser.Id, null!, null!, "ImpersonationStarted", reason);
            _logger.Info($"Admin {admin.Email} is impersonating user {targetUser.Email}");

            return true;
        }

        public async Task<bool> StopImpersonationAsync()
        {
            var session = _httpContext.HttpContext?.Session;
            if (session?.GetString("IsImpersonating") != "true") return false;

            var originalAdminId = session.GetString("AdminIdOriginal");
            if (string.IsNullOrEmpty(originalAdminId)) return false;

            session.SetString("Id", originalAdminId);
            session.Remove("IsImpersonating");
            session.Remove("AdminIdOriginal");

            _logger.Info($"Stopped impersonation. Back to admin: {originalAdminId}");

            await Task.CompletedTask;
            return true;
        }
    }
}
