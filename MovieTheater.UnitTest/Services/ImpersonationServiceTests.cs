using Microsoft.AspNetCore.Http;
using Moq;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MovieTheater.UnitTest.Services
{
    public class ImpersonationServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILoggerService> _mockLogger;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<IHttpContextAccessor> _mockHttpContext;
        private readonly Mock<IClaimsService> _mockClaimsService;
        private readonly Mock<HttpContext> _mockContext;
        private readonly Mock<ISession> _mockSession;
        private readonly ImpersonationService _service;

        public ImpersonationServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILoggerService>();
            _mockAuditLogService = new Mock<IAuditLogService>();
            _mockHttpContext = new Mock<IHttpContextAccessor>();
            _mockClaimsService = new Mock<IClaimsService>();
            _mockContext = new Mock<HttpContext>();
            _mockSession = new Mock<ISession>();

            _mockHttpContext.Setup(h => h.HttpContext).Returns(_mockContext.Object);
            _mockContext.Setup(c => c.Session).Returns(_mockSession.Object);

            _service = new ImpersonationService(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockAuditLogService.Object,
                _mockHttpContext.Object,
                _mockClaimsService.Object);
        }

        [Fact]
        public void GetEffectiveUserId_WhenNotImpersonating_ReturnsCurrentUserId()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            SetupSessionTryGetValue("IsImpersonating", null!);
            _mockClaimsService.Setup(c => c.GetCurrentUserId).Returns(currentUserId);

            // Act
            var result = _service.GetEffectiveUserId();

            // Assert
            Assert.Equal(currentUserId, result);
        }

        [Fact]
        public void GetEffectiveUserId_WhenImpersonating_ReturnsImpersonatedId()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var impersonatedId = Guid.NewGuid();
            SetupSessionTryGetValue("IsImpersonating", "true");
            SetupSessionTryGetValue("Id", impersonatedId.ToString());
            _mockClaimsService.Setup(c => c.GetCurrentUserId).Returns(currentUserId);

            // Act
            var result = _service.GetEffectiveUserId();

            // Assert
            Assert.Equal(impersonatedId, result);
        }

        [Fact]
        public void GetImpersonatedBy_WhenNotImpersonating_ReturnsNull()
        {
            // Arrange
            SetupSessionTryGetValue("IsImpersonating", null!);

            // Act
            var result = _service.GetImpersonatedBy();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetImpersonatedBy_WhenImpersonating_ReturnsAdminId()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            SetupSessionTryGetValue("IsImpersonating", "true");
            SetupSessionTryGetValue("AdminIdOriginal", adminId.ToString());

            // Act
            var result = _service.GetImpersonatedBy();

            // Assert
            Assert.Equal(adminId, result);
        }

        [Fact]
        public void IsImpersonating_WhenImpersonating_ReturnsTrue()
        {
            // Arrange
            SetupSessionTryGetValue("IsImpersonating", "true");

            // Act
            var result = _service.IsImpersonating();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsImpersonating_WhenNotImpersonating_ReturnsFalse()
        {
            // Arrange
            SetupSessionTryGetValue("IsImpersonating", null!);

            // Act
            var result = _service.IsImpersonating();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task StartImpersonationAsync_WhenUserIsAdmin_ReturnsTrue()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var reason = "Testing purposes";

            var admin = new User { Id = adminId, Role = RoleType.Admin, Email = "admin@test.com" };
            var targetUser = new User { Id = targetUserId, Email = "user@test.com" };

            _mockClaimsService.Setup(c => c.GetCurrentUserId).Returns(adminId);
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(adminId, It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
                .ReturnsAsync(admin);
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(targetUserId, It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
                .ReturnsAsync(targetUser);

            // Mock session methods
            _mockSession.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Callback<string, byte[]>((key, value) => { });

            // Act
            var result = await _service.StartImpersonationAsync(targetUserId, reason);

            // Assert
            Assert.True(result);
            _mockSession.Verify(s => s.Set("IsImpersonating", It.IsAny<byte[]>()), Times.Once);
            _mockSession.Verify(s => s.Set("AdminIdOriginal", It.IsAny<byte[]>()), Times.Once);
            _mockSession.Verify(s => s.Set("Id", It.IsAny<byte[]>()), Times.Once);
            _mockAuditLogService.Verify(a => a.LogAsync(
                adminId, AuditActionType.Impersonate, "User", targetUserId,
                null!, null!, "ImpersonationStarted", reason), Times.Once);
        }

        [Fact]
        public async Task StartImpersonationAsync_WhenUserIsNotAdmin_ThrowsForbiddenException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var user = new User { Id = userId, Role = RoleType.Employee };

            _mockClaimsService.Setup(c => c.GetCurrentUserId).Returns(userId);
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(userId, It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
                .ReturnsAsync(user);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _service.StartImpersonationAsync(targetUserId, "Testing"));
        }

        [Fact]
        public async Task StartImpersonationAsync_WhenTargetUserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var admin = new User { Id = adminId, Role = RoleType.Admin };

            _mockClaimsService.Setup(c => c.GetCurrentUserId).Returns(adminId);
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(adminId, It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
                .ReturnsAsync(admin);
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(targetUserId, It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
                .ReturnsAsync((User)null!);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _service.StartImpersonationAsync(targetUserId, "Testing"));
        }

        [Fact]
        public async Task StopImpersonationAsync_WhenImpersonating_ReturnsTrue()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            SetupSessionTryGetValue("IsImpersonating", "true");
            SetupSessionTryGetValue("AdminIdOriginal", adminId.ToString());

            // Mock session methods
            _mockSession.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Callback<string, byte[]>((key, value) => { });
            _mockSession.Setup(s => s.Remove(It.IsAny<string>()))
                .Callback<string>((key) => { });

            // Act
            var result = await _service.StopImpersonationAsync();

            // Assert
            Assert.True(result);
            _mockSession.Verify(s => s.Set("Id", It.IsAny<byte[]>()), Times.Once);
            _mockSession.Verify(s => s.Remove("IsImpersonating"), Times.Once);
            _mockSession.Verify(s => s.Remove("AdminIdOriginal"), Times.Once);
        }

        [Fact]
        public async Task StopImpersonationAsync_WhenNotImpersonating_ReturnsFalse()
        {
            // Arrange
            SetupSessionTryGetValue("IsImpersonating", null!);

            // Act
            var result = await _service.StopImpersonationAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetEffectiveUserId_WhenImpersonatingButIdInvalid_ReturnsCurrentUserId()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            SetupSessionTryGetValue("IsImpersonating", "true");
            SetupSessionTryGetValue("Id", "not-a-guid");
            _mockClaimsService.Setup(c => c.GetCurrentUserId).Returns(currentUserId);

            // Act
            var result = _service.GetEffectiveUserId();

            // Assert
            Assert.Equal(currentUserId, result);
        }

        [Fact]
        public void GetImpersonatedBy_WhenImpersonatingButAdminIdInvalid_ReturnsNull()
        {
            // Arrange
            SetupSessionTryGetValue("IsImpersonating", "true");
            SetupSessionTryGetValue("AdminIdOriginal", "not-a-guid");

            // Act
            var result = _service.GetImpersonatedBy();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void IsImpersonating_WhenSessionValueIsNotTrue_ReturnsFalse()
        {
            // Arrange
            SetupSessionTryGetValue("IsImpersonating", "false");

            // Act
            var result = _service.IsImpersonating();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task StopImpersonationAsync_WhenImpersonatingButAdminIdOriginalMissing_ReturnsFalse()
        {
            // Arrange
            SetupSessionTryGetValue("IsImpersonating", "true");
            SetupSessionTryGetValue("AdminIdOriginal", null!);

            // Act
            var result = await _service.StopImpersonationAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task StopImpersonationAsync_WhenImpersonatingButAdminIdOriginalEmpty_ReturnsFalse()
        {
            // Arrange
            SetupSessionTryGetValue("IsImpersonating", "true");
            SetupSessionTryGetValue("AdminIdOriginal", "");

            // Act
            var result = await _service.StopImpersonationAsync();

            // Assert
            Assert.False(result);
        }

        private void SetupSessionTryGetValue(string key, string value)
        {
            if (value == null)
            {
                _mockSession.Setup(s => s.TryGetValue(key, out It.Ref<byte[]>.IsAny!))
                    .Returns(false);
            }
            else
            {
                _mockSession.Setup(s => s.TryGetValue(key, out It.Ref<byte[]>.IsAny!))
                    .Callback(new TryGetValueCallback((string k, out byte[] v) =>
                    {
                        v = Encoding.UTF8.GetBytes(value);
                    }))
                    .Returns(true);
            }
        }

        [Fact]
        public void GetEffectiveUserId_WhenSessionIsNull_ReturnsCurrentUserId()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            _mockContext.Setup(c => c.Session).Returns((ISession)null!);
            _mockClaimsService.Setup(c => c.GetCurrentUserId).Returns(currentUserId);

            // Act
            var result = _service.GetEffectiveUserId();

            // Assert
            Assert.Equal(currentUserId, result);
        }

        [Fact]
        public void GetEffectiveUserId_WhenImpersonatingButIdIsNull_ReturnsCurrentUserId()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            SetupSessionTryGetValue("IsImpersonating", "true");
            SetupSessionTryGetValue("Id", null!);
            _mockClaimsService.Setup(c => c.GetCurrentUserId).Returns(currentUserId);

            // Act
            var result = _service.GetEffectiveUserId();

            // Assert
            Assert.Equal(currentUserId, result);
        }

        [Fact]
        public void GetEffectiveUserId_WhenImpersonatingButIdIsEmpty_ReturnsCurrentUserId()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            SetupSessionTryGetValue("IsImpersonating", "true");
            SetupSessionTryGetValue("Id", "");
            _mockClaimsService.Setup(c => c.GetCurrentUserId).Returns(currentUserId);

            // Act
            var result = _service.GetEffectiveUserId();

            // Assert
            Assert.Equal(currentUserId, result);
        }

        [Fact]
        public void GetImpersonatedBy_WhenSessionIsNull_ReturnsNull()
        {
            // Arrange
            _mockContext.Setup(c => c.Session).Returns((ISession)null!);

            // Act
            var result = _service.GetImpersonatedBy();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void IsImpersonating_WhenSessionIsNull_ReturnsFalse()
        {
            // Arrange
            _mockContext.Setup(c => c.Session).Returns((ISession)null!);

            // Act
            var result = _service.IsImpersonating();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task StartImpersonationAsync_WhenAdminIsNull_ThrowsForbiddenException()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            _mockClaimsService.Setup(c => c.GetCurrentUserId).Returns(adminId);
            _mockUnitOfWork.Setup(u => u.Users.GetByIdAsync(adminId, It.IsAny<System.Linq.Expressions.Expression<Func<User, object>>[]>()))
                .ReturnsAsync((User)null!);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _service.StartImpersonationAsync(targetUserId, "Testing"));
        }

        [Fact]
        public async Task StopImpersonationAsync_WhenSessionIsNull_ReturnsFalse()
        {
            // Arrange
            _mockContext.Setup(c => c.Session).Returns((ISession)null!);

            // Act
            var result = await _service.StopImpersonationAsync();

            // Assert
            Assert.False(result);
        }

        private delegate void TryGetValueCallback(string key, out byte[] value);
    }
}