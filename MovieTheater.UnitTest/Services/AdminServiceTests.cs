using Microsoft.EntityFrameworkCore;
using MockQueryable;
using Moq;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.DTOs.AdminDTOs;
using MovieTheater.Domain.DTOs.EmailDTOs;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System.Linq.Expressions;

namespace MovieTheater.UnitTest.Services;

public class AdminServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILoggerService> _mockLoggerService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IClaimsService> _mockClaimsService;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<IRedisService> _mockRedisService;
    private readonly Mock<IGenericRepository<User>> _mockUserRepository;
    private readonly AdminService _adminService;
    private readonly Guid _currentAdminId = Guid.NewGuid();

    public AdminServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLoggerService = new Mock<ILoggerService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockClaimsService = new Mock<IClaimsService>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockRedisService = new Mock<IRedisService>();
        _mockUserRepository = new Mock<IGenericRepository<User>>();

        // Setup UnitOfWork to return user repository
        _mockUnitOfWork.Setup(uow => uow.Users).Returns(_mockUserRepository.Object);

        // Setup ClaimsService to return admin id
        _mockClaimsService.Setup(s => s.GetCurrentUserId).Returns(_currentAdminId);

        _adminService = new AdminService(
            _mockUnitOfWork.Object,
            _mockLoggerService.Object,
            _mockEmailService.Object,
            _mockClaimsService.Object,
            _mockAuditLogService.Object,
            _mockRedisService.Object
        );

    }


    [Fact]
    public async Task AddEmployeeAsync_WhenSaveChangesFails_ReturnsUserDto()
    {
        // Arrange
        var employeeDto = new AddEmployeeRequestDto { Email = "fail@example.com", FullName = "Fail" };
        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync((User)null!);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync(new User { Email = employeeDto.Email });
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(0);

        // Act
        var result = await _adminService.AddEmployeeAsync(employeeDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(employeeDto.Email, result.Email);
    }

    [Fact]
    public async Task AddEmployeeAsync_WhenSendEmailFails_ThrowsException()
    {
        // Arrange
        var employeeDto = new AddEmployeeRequestDto { Email = "failmail@example.com", FullName = "FailMail" };
        _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync((User)null!);
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync(new User { Email = employeeDto.Email });
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _mockEmailService.Setup(e => e.SendEmployeeCredentialsEmailAsync(It.IsAny<EmployeeCredentialsEmailDto>())).ThrowsAsync(new Exception("Mail error"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _adminService.AddEmployeeAsync(employeeDto));
        Assert.Contains("Mail error", ex.Message);
    }

    [Fact]
    public async Task AddEmployeeAsync_WithValidData_ReturnsUserDto()
    {
        // Arrange
        var employeeDto = new AddEmployeeRequestDto
        {
            Email = "employee@example.com",
            FullName = "Test Employee",
            DateOfBirth = new DateTime(1990, 1, 1),
            Sex = Gender.Male,
            CCCD = "012345678912",
            PhoneNumber = "0987654321",
            Address = "123 Test St"
        };

        // Setup user repository to return null (email doesn't exist)
        _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User)null!);

        // Setup user repository to return added user
        _mockUserRepository.Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _adminService.AddEmployeeAsync(employeeDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(employeeDto.Email, result.Email);
        Assert.Equal(employeeDto.FullName, result.FullName);
        Assert.Equal(RoleType.Employee, result.Role);

        _mockUserRepository.Verify(repo => repo.AddAsync(It.Is<User>(u =>
            u.Email == employeeDto.Email &&
            u.FullName == employeeDto.FullName &&
            u.UserStatus == UserStatus.Active &&
            u.Role == RoleType.Employee &&
            u.IsEmailVerified == true)), Times.Once);

        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        _mockEmailService.Verify(email => email.SendEmployeeCredentialsEmailAsync(
            It.Is<EmployeeCredentialsEmailDto>(dto => dto.To == employeeDto.Email)), Times.Once);
        _mockAuditLogService.Verify(log => log.LogAsync(
            It.IsAny<Guid>(), AuditActionType.Create, "Employee", It.IsAny<Guid>(),
            It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task AddEmployeeAsync_WithExistingEmail_ThrowsException()
    {
        // Arrange
        var employeeDto = new AddEmployeeRequestDto
        {
            Email = "existing@example.com",
            FullName = "Existing Employee"
        };

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = employeeDto.Email
        };

        _mockUserRepository.Setup(repo => repo.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _adminService.AddEmployeeAsync(employeeDto));

        Assert.Contains("Email has been used", exception.Message);
        _mockUserRepository.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task GetListEmployeeAsync_WhenNoEmployee_ReturnsEmptyPagination()
    {
        // Arrange
        _mockUserRepository.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<Expression<Func<User, object>>[]>())).ReturnsAsync(new List<User>());

        // Act
        var result = await _adminService.GetListEmployeeAsync(null, null, false, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetListUserAsync_ReturnsCachedResult_WhenCacheExists()
    {
        // Arrange
        var search = "test";
        var role = RoleType.Member;
        var sortBy = "CreatedAt";
        var isDescending = true;
        var page = 1;
        var pageSize = 10;

        var cachedResult = new Pagination<GetUserDto>(
            new List<GetUserDto>
            {
                new GetUserDto { Id = Guid.NewGuid(), Email = "test@example.com" }
            },
            1, page, pageSize);

        var cacheKey = $"admin:user:list:{search}:{role}:{sortBy}:{isDescending}:{page}:{pageSize}";

        _mockRedisService.Setup(redis => redis.GetAsync<Pagination<GetUserDto>>(cacheKey))
            .ReturnsAsync(cachedResult);

        // Act
        var result = await _adminService.GetListUserAsync(search, role, sortBy, isDescending, page, pageSize);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(cachedResult.Items.First().Email, result.Items.First().Email);

        _mockRedisService.Verify(redis => redis.GetAsync<Pagination<GetUserDto>>(cacheKey), Times.Once);
        _mockUserRepository.Verify(repo => repo.GetQueryable(), Times.Never);
    }

    [Fact]
    public async Task GetListEmployeeAsync_WithValidSearch_ReturnsPagination()
    {
        // Arrange
        var employees = new List<User>
        {
            new User {
                Id = Guid.NewGuid(),
                FullName = "Employee 1",
                Email = "emp1@example.com",
                Role = RoleType.Employee,
                IsDeleted = false
            },
            new User {
                Id = Guid.NewGuid(),
                FullName = "Employee 2",
                Email = "emp2@example.com",
                Role = RoleType.Employee,
                IsDeleted = false
            }
        };

        _mockUserRepository.Setup(repo => repo.GetAllAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<Expression<Func<User, object>>[]>())).ReturnsAsync(employees);

        // Act
        var result = await _adminService.GetListEmployeeAsync("emp", "fullname", false, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal("Employee 1", result.Items[0].FullName);
    }


    [Fact]
    public async Task EditEmployeeAsync_WithValidData_ReturnsUpdatedDto()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new User
        {
            Id = employeeId,
            FullName = "Original Name",
            Email = "employee@example.com",
            DateOfBirth = new DateTime(1990, 1, 1),
            Sex = Gender.Male,
            CCCD = "012345678912",
            PhoneNumber = "0987654321",
            Address = "Original Address"
        };

        var editDto = new EditEmployeeDto
        {
            FullName = "Updated Name",
            DateOfBirth = new DateTime(1992, 2, 2),
            Address = "Updated Address",
            Password = "NewPassword123"
        };

        _mockUserRepository.Setup(repo => repo.GetByIdAsync(employeeId))
            .ReturnsAsync(employee);

        // Act
        var result = await _adminService.EditEmployeeAsync(employeeId, editDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(editDto.FullName, result.FullName);
        Assert.Equal(editDto.DateOfBirth, result.DateOfBirth);
        Assert.Equal(editDto.Address, result.Address);

        _mockUserRepository.Verify(repo => repo.Update(It.Is<User>(u =>
            u.FullName == editDto.FullName &&
            u.Address == editDto.Address)), Times.Once);

        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        _mockEmailService.Verify(email => email.SendUpdateEmployeeCredentialsEmailAsync(It.IsAny<UpdateEmployeeCredentialsEmailDto>()), Times.Once);
        _mockAuditLogService.Verify(log => log.LogAsync(
            It.IsAny<Guid>(), AuditActionType.Update, "Employee", employeeId,
            It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task EditEmployeeAsync_WithNonExistentUser_ThrowsException()
    {
        // Arrange
        var employeeId = Guid.NewGuid();

        _mockUserRepository.Setup(repo => repo.GetByIdAsync(employeeId))
            .ReturnsAsync((User)null!);

        var editDto = new EditEmployeeDto { FullName = "Updated Name" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _adminService.EditEmployeeAsync(employeeId, editDto));

        Assert.Contains("User not found", exception.Message);
    }

    [Fact]
    public async Task EditEmployeeAsync_WithInvalidDateOfBirth_ThrowsException()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new User
        {
            Id = employeeId,
            FullName = "Original Name",
            Email = "employee@example.com"
        };

        var futureDate = DateTime.UtcNow.AddYears(1);
        var editDto = new EditEmployeeDto { DateOfBirth = futureDate };

        _mockUserRepository.Setup(repo => repo.GetByIdAsync(employeeId))
            .ReturnsAsync(employee);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _adminService.EditEmployeeAsync(employeeId, editDto));

        Assert.Contains("Date of birth cannot be in the future", exception.Message);
    }

    [Fact]
    public async Task DeleteEmployeeAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var employee = new User
        {
            Id = employeeId,
            Email = "employee@example.com",
            Role = RoleType.Employee,
            UserStatus = UserStatus.Active,
            IsDeleted = false
        };

        _mockUserRepository.Setup(repo => repo.GetByIdAsync(employeeId))
            .ReturnsAsync(employee);

        // Act
        var result = await _adminService.DeleteEmployeeAsync(employeeId, adminId);

        // Assert
        Assert.True(result);

        _mockUserRepository.Verify(repo => repo.Update(It.Is<User>(u =>
            u.UserStatus == UserStatus.Deleted &&
            u.IsDeleted == true &&
            u.DeletedBy == adminId)), Times.Once);

        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        _mockRedisService.Verify(redis => redis.RemoveByPatternAsync(It.IsAny<string>()), Times.Once);
        _mockRedisService.Verify(redis => redis.RemoveAsync(It.IsAny<string>()), Times.Once);
        _mockAuditLogService.Verify(log => log.LogAsync(
            It.IsAny<Guid>(), AuditActionType.Delete, "Employee", employeeId,
            It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task DeleteEmployeeAsync_WithNonExistentId_ReturnsFalse()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        _mockUserRepository.Setup(repo => repo.GetByIdAsync(employeeId))
            .ReturnsAsync((User)null!);

        // Act
        var result = await _adminService.DeleteEmployeeAsync(employeeId, adminId);

        // Assert
        Assert.False(result);
        _mockUserRepository.Verify(repo => repo.Update(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task BanUserAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "user@example.com",
            UserStatus = UserStatus.Active,
            IsDeleted = false
        };

        _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _adminService.BanUserAsync(userId, adminId);

        // Assert
        Assert.True(result);

        _mockUserRepository.Verify(repo => repo.Update(It.Is<User>(u =>
            u.UserStatus == UserStatus.Banned &&
            u.UpdatedBy == adminId)), Times.Once);

        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        _mockAuditLogService.Verify(log => log.LogAsync(
            It.IsAny<Guid>(), AuditActionType.Update, "User", userId,
            It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task BanUserAsync_WithAlreadyBannedUser_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "banned@example.com",
            UserStatus = UserStatus.Banned,
            IsDeleted = false
        };

        _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _adminService.BanUserAsync(userId, adminId);

        // Assert
        Assert.False(result);
        _mockUserRepository.Verify(repo => repo.Update(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UnbanUserAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "banned@example.com",
            UserStatus = UserStatus.Banned,
            IsDeleted = false
        };

        _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _adminService.UnbanUserAsync(userId, adminId);

        // Assert
        Assert.True(result);

        _mockUserRepository.Verify(repo => repo.Update(It.Is<User>(u =>
            u.UserStatus == UserStatus.Active &&
            u.UpdatedBy == adminId)), Times.Once);

        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        _mockAuditLogService.Verify(log => log.LogAsync(
            It.IsAny<Guid>(), AuditActionType.Update, "User", userId,
            It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UnbanUserAsync_WithNonBannedUser_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "active@example.com",
            UserStatus = UserStatus.Active,
            IsDeleted = false
        };

        _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _adminService.UnbanUserAsync(userId, adminId);

        // Assert
        Assert.False(result);
        _mockUserRepository.Verify(repo => repo.Update(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task GetUserDetailAsync_WithCachedResult_ReturnsUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cacheKey = $"admin:user:detail:{userId}";
        var cachedUser = new GetUserDto
        {
            Id = userId,
            Email = "cached@example.com",
            FullName = "Cached User"
        };

        _mockRedisService.Setup(redis => redis.GetAsync<GetUserDto>(cacheKey))
            .ReturnsAsync(cachedUser);

        // Act
        var result = await _adminService.GetUserDetailAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(cachedUser.Email, result.Email);
        Assert.Equal(cachedUser.FullName, result.FullName);

        _mockRedisService.Verify(redis => redis.GetAsync<GetUserDto>(cacheKey), Times.Once);
        _mockUserRepository.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetUserDetailAsync_WithoutCache_ReturnsFromDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "user@example.com",
            FullName = "Database User",
            UserStatus = UserStatus.Active,
            IsDeleted = false
        };

        _mockRedisService.Setup(redis => redis.GetAsync<GetUserDto>(It.IsAny<string>()))
            .ReturnsAsync((GetUserDto)null!);

        _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _adminService.GetUserDetailAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.FullName, result.FullName);

        _mockRedisService.Verify(redis => redis.GetAsync<GetUserDto>(It.IsAny<string>()), Times.Once);
        _mockRedisService.Verify(redis => redis.SetAsync(
            It.IsAny<string>(), It.IsAny<GetUserDto>(), It.IsAny<TimeSpan>()), Times.Once);
        _mockUserRepository.Verify(repo => repo.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserDetailAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockRedisService.Setup(redis => redis.GetAsync<GetUserDto>(It.IsAny<string>()))
            .ReturnsAsync((GetUserDto)null!);

        _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync((User)null!);

        // Act
        var result = await _adminService.GetUserDetailAsync(userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetListUserAsync_WhenCacheMissAndRoleFilter_Works()
    {
        // Arrange
        _mockRedisService.Setup(r => r.GetAsync<Pagination<GetUserDto>>(It.IsAny<string>())).ReturnsAsync((Pagination<GetUserDto>)null!);
        var users = new List<User>
    {
        new User { Id = Guid.NewGuid(), Email = "a@a.com", FullName = "A", Role = RoleType.Member, IsDeleted = false },
        new User { Id = Guid.NewGuid(), Email = "b@b.com", FullName = "B", Role = RoleType.Employee, IsDeleted = false }
    }.AsQueryable().BuildMock();
        _mockUserRepository.Setup(r => r.GetQueryable()).Returns(users);

        // Act
        var result = await _adminService.GetListUserAsync(null, RoleType.Member, null, false, 1, 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(RoleType.Member, result.Items[0].Role);
    }

    [Fact]
    public async Task GetListUserAsync_WhenSortByScoreBalanceDescending_Works()
    {
        // Arrange
        _mockRedisService.Setup(r => r.GetAsync<Pagination<GetUserDto>>(It.IsAny<string>()))
            .ReturnsAsync((Pagination<GetUserDto>)null!);

        var users = new List<User>
    {
        new User
        {
            Id = Guid.NewGuid(),
            Email = "lowscore@theater.com",
            FullName = "Low Score User",
            Role = RoleType.Member,
            IsDeleted = false,
            ScoreBalance = 10,
            UserStatus = UserStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        },
        new User
        {
            Id = Guid.NewGuid(),
            Email = "highscore@theater.com",
            FullName = "High Score User",
            Role = RoleType.Member,
            IsDeleted = false,
            ScoreBalance = 20,
            UserStatus = UserStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-15)
        }
    }.AsQueryable().BuildMock();

        _mockUserRepository.Setup(r => r.GetQueryable()).Returns(users);

        // Act - Sort by ScoreBalance descending
        var result = await _adminService.GetListUserAsync(null, RoleType.Member, "ScoreBalance", true, 1, 10);

        // Assert - First item should have highest score (20)
        Assert.Equal(20, result.Items[0].ScoreBalance);
        Assert.Equal("High Score User", result.Items[0].FullName);
        Assert.Equal("highscore@theater.com", result.Items[0].Email);
    }

    [Fact]
    public async Task GetListUserAsync_WhenException_Throws()
    {
        _mockRedisService.Setup(r => r.GetAsync<Pagination<GetUserDto>>(It.IsAny<string>())).ReturnsAsync((Pagination<GetUserDto>)null!);
        _mockUserRepository.Setup(r => r.GetQueryable()).Throws(new Exception("DB error"));

        await Assert.ThrowsAsync<Exception>(() => _adminService.GetListUserAsync(null, null, null, false, 1, 10));
    }

    [Fact]
    public async Task GetListEmployeeAsync_WithSortByDateOfBirthDescending_Works()
    {
        var employees = new List<User>
    {
        new User { Id = Guid.NewGuid(), FullName = "A", Role = RoleType.Employee, IsDeleted = false, DateOfBirth = new DateTime(2000,1,1) },
        new User { Id = Guid.NewGuid(), FullName = "B", Role = RoleType.Employee, IsDeleted = false, DateOfBirth = new DateTime(2010,1,1) }
    };
        _mockUserRepository.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<Expression<Func<User, object>>[]>())).ReturnsAsync(employees);

        var result = await _adminService.GetListEmployeeAsync(null, "dateofbirth", true, 1, 10);

        Assert.Equal("B", result.Items[0].FullName);
    }

    [Fact]
    public async Task GetListEmployeeAsync_WhenException_Throws()
    {
        _mockUserRepository.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<Expression<Func<User, object>>[]>())).Throws(new Exception("DB error"));

        await Assert.ThrowsAsync<Exception>(() => _adminService.GetListEmployeeAsync(null, null, false, 1, 10));
    }

    [Fact]
    public async Task EditEmployeeAsync_WithInvalidCCCD_Throws()
    {
        var employeeId = Guid.NewGuid();
        var employee = new User { Id = employeeId, FullName = "A", Email = "a@a.com" };
        _mockUserRepository.Setup(r => r.GetByIdAsync(employeeId)).ReturnsAsync(employee);

        var editDto = new EditEmployeeDto { CCCD = "123" }; // Không đủ 12 số

        await Assert.ThrowsAsync<ArgumentException>(() => _adminService.EditEmployeeAsync(employeeId, editDto));
    }

    [Fact]
    public async Task EditEmployeeAsync_WithNoChanges_ReturnsInput()
    {
        var employeeId = Guid.NewGuid();
        var employee = new User { Id = employeeId, FullName = "A", Email = "a@a.com" };
        _mockUserRepository.Setup(r => r.GetByIdAsync(employeeId)).ReturnsAsync(employee);

        var editDto = new EditEmployeeDto { FullName = "A" }; // Không đổi gì

        var result = await _adminService.EditEmployeeAsync(employeeId, editDto);

        Assert.Equal(editDto.FullName, result.FullName);
    }

    [Fact]
public async Task GetUserByPhoneNumberAsync_WithEmptyPhone_Throws()
{
    await Assert.ThrowsAsync<ArgumentException>(() => _adminService.GetUserByPhoneNumberAsync(""));
}

[Fact]
public async Task GetUserByPhoneNumberAsync_WithInvalidFormat_Throws()
{
    await Assert.ThrowsAsync<ArgumentException>(() => _adminService.GetUserByPhoneNumberAsync("abc"));
}

[Fact]
public async Task GetUserByPhoneNumberAsync_NotFound_ReturnsNull()
{
    _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync((User)null!);

    var result = await _adminService.GetUserByPhoneNumberAsync("0123456789");
    Assert.Null(result);
}

[Fact]
public async Task GetUserByPhoneNumberAsync_Found_ReturnsDto()
{
    var user = new User { Id = Guid.NewGuid(), PhoneNumber = "0123456789", Email = "a@a.com", FullName = "A" };
    _mockUserRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

    var result = await _adminService.GetUserByPhoneNumberAsync("0123456789");
    Assert.NotNull(result);
    Assert.Equal("0123456789", result.PhoneNumber);
}
}