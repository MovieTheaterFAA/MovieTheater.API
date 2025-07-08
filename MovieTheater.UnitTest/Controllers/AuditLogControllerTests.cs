using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.API.Controllers;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain;
using MovieTheater.Domain.DTOs.AuditLogDTOs;
using MovieTheater.Domain.Enums;

namespace MovieTheater.UnitTest.Controllers
{
    public class AuditLogControllerTests
    {
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<ILoggerService> _mockLoggerService;
        private readonly Mock<MovieTheaterDbContext> _mockDbContext;
        private readonly SystemOwnerController _controller;

        public AuditLogControllerTests()
        {
            _mockAuditLogService = new Mock<IAuditLogService>();
            _mockLoggerService = new Mock<ILoggerService>();
            _mockDbContext = new Mock<MovieTheaterDbContext>();

            _controller = new SystemOwnerController(
                _mockDbContext.Object,
                _mockLoggerService.Object,
                _mockAuditLogService.Object
            );
        }

        [Fact]
        public async Task ViewAuditLogAsync_ValidParameters_ReturnsOkResult()
        {
            // Arrange
            string search = "test";
            AuditActionType? actionType = AuditActionType.Create;
            string entityType = "User";
            bool isDescending = true;
            int page = 1;
            int pageSize = 10;

            var auditLogs = new Pagination<AuditLogDto>
            {
                Items = new List<AuditLogDto>
                {
                    new AuditLogDto
                    {
                        Id = Guid.NewGuid(),
                        AdminId = Guid.NewGuid(),
                        ActionType = AuditActionType.Create,
                        EntityType = "User",
                        EntityId = Guid.NewGuid(),
                        ChangedFields = "Name,Email",
                        OldValue = "{}",
                        NewValue = "{\"Name\":\"Test\",\"Email\":\"test@example.com\"}",
                        Reason = "Initial creation",
                        Timestamp = DateTime.UtcNow
                    }
                },
                CurrentPage = 1,
                TotalPages = 1,
                PageSize = 10,
                TotalCount = 1
            };

            _mockAuditLogService.Setup(s => s.ViewLogAsync(
                search, actionType, entityType, isDescending, page, pageSize))
                .ReturnsAsync(auditLogs);

            // Act
            var result = await _controller.ViewAuditLogAsync(search, actionType, entityType, isDescending, page, pageSize);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<Pagination<AuditLogDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal("Retrieved audit logs successfully", apiResult.Value.Message);
            Assert.Equal(auditLogs, apiResult.Value.Data);
        }

        [Fact]
        public async Task ViewAuditLogAsync_DefaultParameters_ReturnsOkResult()
        {
            // Arrange
            var auditLogs = new Pagination<AuditLogDto>
            {
                Items = new List<AuditLogDto>(),
                CurrentPage = 1,
                TotalPages = 0,
                PageSize = 10,
                TotalCount = 0
            };

            _mockAuditLogService.Setup(s => s.ViewLogAsync(
                null, null, null, false, 1, 10))
                .ReturnsAsync(auditLogs);

            // Act
            var result = await _controller.ViewAuditLogAsync(null, null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<Pagination<AuditLogDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(auditLogs, apiResult.Value.Data);
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(1, 0)]
        [InlineData(-1, 5)]
        [InlineData(5, -1)]
        public async Task ViewAuditLogAsync_InvalidPagination_ReturnsBadRequest(int page, int pageSize)
        {
            // Act
            var result = await _controller.ViewAuditLogAsync(null, null, null, false, page, pageSize);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("Invalid pagination parameters", apiResult.Error.Message);
        }

        [Fact]
        public async Task ViewAuditLogAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            _mockAuditLogService.Setup(s => s.ViewLogAsync(
                It.IsAny<string>(), It.IsAny<AuditActionType?>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.ViewAuditLogAsync("test", null, null, false, 1, 10);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(statusCodeResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task ViewAuditLogAsync_FilterByActionType_ReturnsOkResult()
        {
            // Arrange
            AuditActionType actionType = AuditActionType.Update;
            var auditLogs = new Pagination<AuditLogDto>
            {
                Items = new List<AuditLogDto>
                {
                    new AuditLogDto
                    {
                        ActionType = AuditActionType.Update
                    }
                }
            };

            _mockAuditLogService.Setup(s => s.ViewLogAsync(
                null, actionType, null, false, 1, 10))
                .ReturnsAsync(auditLogs);

            // Act
            var result = await _controller.ViewAuditLogAsync(null, actionType, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<Pagination<AuditLogDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(auditLogs, apiResult.Value.Data);
        }

        [Fact]
        public async Task ViewAuditLogAsync_FilterByEntityType_ReturnsOkResult()
        {
            // Arrange
            string entityType = "Movie";
            var auditLogs = new Pagination<AuditLogDto>
            {
                Items = new List<AuditLogDto>
                {
                    new AuditLogDto
                    {
                        EntityType = "Movie"
                    }
                }
            };

            _mockAuditLogService.Setup(s => s.ViewLogAsync(
                null, null, entityType, false, 1, 10))
                .ReturnsAsync(auditLogs);

            // Act
            var result = await _controller.ViewAuditLogAsync(null, null, entityType);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<Pagination<AuditLogDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(auditLogs, apiResult.Value.Data);
        }

        [Fact]
        public async Task ViewAuditLogAsync_SearchParameter_ReturnsOkResult()
        {
            // Arrange
            string search = "admin";
            var auditLogs = new Pagination<AuditLogDto>
            {
                Items = new List<AuditLogDto>
                {
                    new AuditLogDto
                    {
                        EntityType = "User",
                        NewValue = "{\"Name\":\"Admin User\"}"
                    }
                }
            };

            _mockAuditLogService.Setup(s => s.ViewLogAsync(
                search, null, null, false, 1, 10))
                .ReturnsAsync(auditLogs);

            // Act
            var result = await _controller.ViewAuditLogAsync(search, null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<Pagination<AuditLogDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(auditLogs, apiResult.Value.Data);
        }

        [Fact]
        public async Task ViewAuditLogAsync_DescendingSorting_ReturnsOkResult()
        {
            // Arrange
            bool isDescending = true;
            var auditLogs = new Pagination<AuditLogDto>
            {
                Items = new List<AuditLogDto>
                {
                    new AuditLogDto { Timestamp = DateTime.Now.AddDays(-1) },
                    new AuditLogDto { Timestamp = DateTime.Now.AddDays(-2) }
                }
            };

            _mockAuditLogService.Setup(s => s.ViewLogAsync(
                null, null, null, isDescending, 1, 10))
                .ReturnsAsync(auditLogs);

            // Act
            var result = await _controller.ViewAuditLogAsync(null, null, null, isDescending);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<Pagination<AuditLogDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(auditLogs, apiResult.Value.Data);
        }
    }
}