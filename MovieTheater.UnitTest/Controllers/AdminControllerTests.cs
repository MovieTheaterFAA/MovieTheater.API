using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.API.Controllers;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.AdminDTOs;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.UnitTest.Controllers
{
    public class AdminControllerTests
    {
        private readonly Mock<IAdminService> _mockAdminService;
        private readonly Mock<IClaimsService> _mockClaimsService;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            _mockAdminService = new Mock<IAdminService>();
            _mockClaimsService = new Mock<IClaimsService>();
            _controller = new AdminController(
                _mockAdminService.Object,
                _mockClaimsService.Object
            );
        }

        [Fact]
        public async Task GetAllUserAsync_ValidParameters_ReturnsOkResult()
        {
            // Arrange
            var userList = new Pagination<GetUserDto> { Items = new List<GetUserDto>() };
            _mockAdminService.Setup(s => s.GetListUserAsync(It.IsAny<string>(), It.IsAny<RoleType?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(userList);

            // Act
            var result = await _controller.GetAllUserAsync("name", RoleType.Member, "ScoreBalance", true, 1, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<Pagination<GetUserDto>>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(userList, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetAllUserAsync_InvalidPagination_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetAllUserAsync(null, null, null, false, 0, 0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetAllUserAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            _mockAdminService.Setup(s => s.GetListUserAsync(It.IsAny<string>(), It.IsAny<RoleType?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetAllUserAsync("name", null, null, false, 1, 10);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Theory]
        [InlineData("ScoreBalance", false)]
        [InlineData("CreatedAt", true)]
        [InlineData("", false)]
        public async Task GetAllUserAsync_DifferentSortParameters_ReturnsOkResult(string sortBy, bool isDescending)
        {
            // Arrange
            var userList = new Pagination<GetUserDto> { Items = new List<GetUserDto>() };
            _mockAdminService.Setup(s => s.GetListUserAsync(It.IsAny<string>(), It.IsAny<RoleType?>(), sortBy, isDescending, It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(userList);

            // Act
            var result = await _controller.GetAllUserAsync(null, null, sortBy, isDescending, 1, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<ApiResult<Pagination<GetUserDto>>>(okResult.Value);
        }

        [Fact]
        public async Task GetAllEmployeeAsync_ValidParameters_ReturnsOkResult()
        {
            // Arrange
            var employeeList = new Pagination<UserDto> { Items = new List<UserDto>() };
            _mockAdminService.Setup(s => s.GetListEmployeeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(employeeList);

            // Act
            var result = await _controller.GetAllEmployeeAsync("name", "FullName", true, 1, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(employeeList, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetAllEmployeeAsync_InvalidPagination_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetAllEmployeeAsync(null, null, false, 0, 0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetAllEmployeeAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            _mockAdminService.Setup(s => s.GetListEmployeeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetAllEmployeeAsync("name", "FullName", false, 1, 10);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Theory]
        [InlineData("FullName", false)]
        [InlineData("DateOfBirth", true)]
        [InlineData("", false)]
        public async Task GetAllEmployeeAsync_DifferentSortParameters_ReturnsOkResult(string sortBy, bool isDescending)
        {
            // Arrange
            var employeeList = new Pagination<UserDto> { Items = new List<UserDto>() };
            _mockAdminService.Setup(s => s.GetListEmployeeAsync(It.IsAny<string>(), sortBy, isDescending, It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(employeeList);

            // Act
            var result = await _controller.GetAllEmployeeAsync(null, sortBy, isDescending, 1, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<ApiResult<object>>(okResult.Value);
        }

        [Fact]
        public async Task GetUserDetailAsync_UserExists_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userDto = new GetUserDto { Id = userId };
            _mockAdminService.Setup(s => s.GetUserDetailAsync(userId)).ReturnsAsync(userDto);

            // Act
            var result = await _controller.GetUserDetailAsync(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<GetUserDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(userDto, apiResult.Value.Data);
        }

        [Fact]
        public async Task GetUserDetailAsync_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockAdminService.Setup(s => s.GetUserDetailAsync(userId)).ReturnsAsync((GetUserDto)null!);

            // Act
            var result = await _controller.GetUserDetailAsync(userId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(notFoundResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task GetUserDetailAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockAdminService.Setup(s => s.GetUserDetailAsync(userId)).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetUserDetailAsync(userId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task AddEmployeeAsync_ValidData_ReturnsOkResult()
        {
            // Arrange
            var addEmployeeDto = new AddEmployeeRequestDto
            {
                FullName = "Test Employee",
                Email = "test@example.com",
                PhoneNumber = "0123456789",
                DateOfBirth = DateTime.Now.AddYears(-25),
                Sex = Gender.Male,
                CCCD = "0123456789"
            };
            var userDto = new UserDto
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                FullName = "Test Employee"
            };
            _mockAdminService.Setup(s => s.AddEmployeeAsync(addEmployeeDto)).ReturnsAsync(userDto);

            // Act
            var result = await _controller.AddEmployeeAsync(addEmployeeDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<UserDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(userDto, apiResult.Value.Data);
        }

        [Fact]
        public async Task AddEmployeeAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var addEmployeeDto = new AddEmployeeRequestDto { Email = "test@example.com" };
            _mockAdminService.Setup(s => s.AddEmployeeAsync(addEmployeeDto)).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.AddEmployeeAsync(addEmployeeDto);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<UserDto>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task EditEmployeeAsync_ValidData_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var editEmployeeDto = new EditEmployeeDto { FullName = "Test Name" };

            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(currentUserId);
            _mockAdminService.Setup(s => s.EditEmployeeAsync(userId, editEmployeeDto)).ReturnsAsync(editEmployeeDto);

            // Act
            var result = await _controller.EditEmployeeAsync(userId, editEmployeeDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<EditEmployeeDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(editEmployeeDto, apiResult.Value.Data);
        }

        [Fact]
        public async Task EditEmployeeAsync_EmptyUserId_ReturnsBadRequest()
        {
            // Arrange
            var editEmployeeDto = new EditEmployeeDto { FullName = "Test Name" };

            // Act
            var result = await _controller.EditEmployeeAsync(Guid.Empty, editEmployeeDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task EditEmployeeAsync_NullDto_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.EditEmployeeAsync(Guid.NewGuid(), null!);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task EditEmployeeAsync_ModelValidationFails_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _controller.ModelState.AddModelError("FullName", "FullName is required");

            // Act
            var result = await _controller.EditEmployeeAsync(userId, new EditEmployeeDto());

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task EditEmployeeAsync_UserNotFound_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var editEmployeeDto = new EditEmployeeDto { FullName = "Test Name" };

            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(currentUserId);
            _mockAdminService.Setup(s => s.EditEmployeeAsync(userId, editEmployeeDto)).ReturnsAsync((EditEmployeeDto)null!);

            // Act
            var result = await _controller.EditEmployeeAsync(userId, editEmployeeDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task EditEmployeeAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var editEmployeeDto = new EditEmployeeDto { FullName = "Test Name" };
            _mockAdminService.Setup(s => s.EditEmployeeAsync(userId, editEmployeeDto)).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.EditEmployeeAsync(userId, editEmployeeDto);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<EditEmployeeDto>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task DeleteUser_ValidId_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(currentUserId);
            _mockAdminService.Setup(s => s.DeleteEmployeeAsync(userId, currentUserId)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteUser(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
        }

        [Fact]
        public async Task DeleteUser_EmptyId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.DeleteUser(Guid.Empty);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task DeleteUser_DeletionFailed_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(currentUserId);
            _mockAdminService.Setup(s => s.DeleteEmployeeAsync(userId, currentUserId)).ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteUser(userId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task DeleteUser_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(currentUserId);
            _mockAdminService.Setup(s => s.DeleteEmployeeAsync(userId, currentUserId))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.DeleteUser(userId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<UserDto>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task BanUser_ValidId_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(currentUserId);
            _mockAdminService.Setup(s => s.BanUserAsync(userId, currentUserId)).ReturnsAsync(true);

            // Act
            var result = await _controller.BanUser(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
        }

        [Fact]
        public async Task BanUser_EmptyId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.BanUser(Guid.Empty);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task BanUser_BanFailed_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(currentUserId);
            _mockAdminService.Setup(s => s.BanUserAsync(userId, currentUserId)).ReturnsAsync(false);

            // Act
            var result = await _controller.BanUser(userId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task BanUser_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(currentUserId);
            _mockAdminService.Setup(s => s.BanUserAsync(userId, currentUserId))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.BanUser(userId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task UnbanUser_ValidId_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(currentUserId);
            _mockAdminService.Setup(s => s.UnbanUserAsync(userId, currentUserId)).ReturnsAsync(true);

            // Act
            var result = await _controller.UnbanUser(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
        }

        [Fact]
        public async Task UnbanUser_EmptyId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.UnbanUser(Guid.Empty);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task UnbanUser_UnbanFailed_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(currentUserId);
            _mockAdminService.Setup(s => s.UnbanUserAsync(userId, currentUserId)).ReturnsAsync(false);

            // Act
            var result = await _controller.UnbanUser(userId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
        }

        [Fact]
        public async Task UnbanUser_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            _mockClaimsService.SetupGet(s => s.GetCurrentUserId).Returns(currentUserId);
            _mockAdminService.Setup(s => s.UnbanUserAsync(userId, currentUserId))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.UnbanUser(userId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
        }
        [Fact]
        public async Task GetUserByPhoneNumberAsync_PhoneNumberIsNullOrWhiteSpace_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetUserByPhoneNumberAsync(null!);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("Phone number is required.", apiResult.Error.Message);
        }

        [Fact]
        public async Task GetUserByPhoneNumberAsync_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockAdminService.Setup(s => s.GetUserByPhoneNumberAsync(It.IsAny<string>()))
                .ReturnsAsync((GetUserDto)null!);

            // Act
            var result = await _controller.GetUserByPhoneNumberAsync("0123456789");

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(notFoundResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("User not found.", apiResult.Error.Message);
        }

        [Fact]
        public async Task GetUserByPhoneNumberAsync_UserFound_ReturnsOk()
        {
            // Arrange
            var userDto = new GetUserDto { Id = Guid.NewGuid(), PhoneNumber = "0123456789" };
            _mockAdminService.Setup(s => s.GetUserByPhoneNumberAsync("0123456789"))
                .ReturnsAsync(userDto);

            // Act
            var result = await _controller.GetUserByPhoneNumberAsync("0123456789");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<GetUserDto>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(userDto, apiResult.Value.Data);
            Assert.Equal("Get user by phone number successfully.", apiResult.Value.Message);
        }

        [Fact]
        public async Task GetUserByPhoneNumberAsync_ArgumentException_ReturnsBadRequest()
        {
            // Arrange
            _mockAdminService.Setup(s => s.GetUserByPhoneNumberAsync(It.IsAny<string>()))
                .ThrowsAsync(new ArgumentException("Invalid phone number format."));

            // Act
            var result = await _controller.GetUserByPhoneNumberAsync("invalid");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("Invalid phone number format.", apiResult.Error.Message);
        }

        [Fact]
        public async Task GetUserByPhoneNumberAsync_ServiceThrowsException_ReturnsErrorResponse()
        {
            // Arrange
            _mockAdminService.Setup(s => s.GetUserByPhoneNumberAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetUserByPhoneNumberAsync("0123456789");

            // Assert
            Assert.IsType<ObjectResult>(result);
        }

        [Fact]
        public async Task EditEmployeeAsync_ArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var editEmployeeDto = new EditEmployeeDto { FullName = "Test Name" };
            _mockAdminService
                .Setup(s => s.EditEmployeeAsync(userId, editEmployeeDto))
                .ThrowsAsync(new ArgumentException("Invalid employee data"));

            // Act
            var result = await _controller.EditEmployeeAsync(userId, editEmployeeDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("Invalid employee data", apiResult.Error.Message);
        }

    }
}