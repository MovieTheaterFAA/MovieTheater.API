using Moq;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;


namespace MovieTheater.UnitTest.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILoggerService> _mockLoggerService;
        private readonly Mock<IRedisService> _mockRedisService;
        private readonly Mock<IGenericRepository<User>> _mockUserRepository;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLoggerService = new Mock<ILoggerService>();
            _mockRedisService = new Mock<IRedisService>();
            _mockUserRepository = new Mock<IGenericRepository<User>>();

            // Setup UnitOfWork to return user repository
            _mockUnitOfWork.Setup(uow => uow.Users).Returns(_mockUserRepository.Object);

            _userService = new UserService(
                _mockUnitOfWork.Object,
                _mockLoggerService.Object,
                _mockRedisService.Object
            );
        }

        #region GetUserDetails Tests

        [Fact]
        public async Task GetUserDetails_WithCachedData_ReturnsCachedUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var cachedUser = new CurrentUserDto
            {
                FullName = "Cached User",
                Email = "cached@example.com",
                Role = RoleType.Member
            };

            _mockRedisService.Setup(redis => redis.GetAsync<CurrentUserDto>($"user:detail:{userId}"))
                .ReturnsAsync(cachedUser);

            // Act
            var result = await _userService.GetUserDetails(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(cachedUser.FullName, result.FullName);
            Assert.Equal(cachedUser.Email, result.Email);

            // Verify Redis was called but database was not
            _mockRedisService.Verify(redis => redis.GetAsync<CurrentUserDto>($"user:detail:{userId}"), Times.Once);
            _mockUserRepository.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetUserDetails_WithoutCache_ReturnsFromDatabase()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = "Database User",
                Email = "database@example.com",
                Role = RoleType.Member,
                DateOfBirth = new DateTime(1990, 1, 1),
                Sex = Gender.Male,
                CCCD = "123456789012",
                PhoneNumber = "1234567890",
                Address = "123 Test St",
                ScoreBalance = 100,
                AvatarUrl = "avatar.jpg"
            };

            _mockRedisService.Setup(redis => redis.GetAsync<CurrentUserDto>($"user:detail:{userId}"))
                .ReturnsAsync((CurrentUserDto)null!);

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.GetUserDetails(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.FullName, result.FullName);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.ScoreBalance, result.ScoreBalance);

            _mockRedisService.Verify(redis => redis.GetAsync<CurrentUserDto>($"user:detail:{userId}"), Times.Once);
            _mockRedisService.Verify(redis => redis.SetAsync(
                $"user:detail:{userId}",
                It.IsAny<CurrentUserDto>(),
                TimeSpan.FromMinutes(10)), Times.Once);
            _mockUserRepository.Verify(repo => repo.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetUserDetails_WithEmptyGuid_ThrowsArgumentException()
        {
            // Arrange
            var userId = Guid.Empty;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _userService.GetUserDetails(userId));

            Assert.Contains("Invalid user ID", exception.Message);
            _mockLoggerService.Verify(logger => logger.Warn(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetUserDetails_WithNonExistentUser_ThrowsKeyNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockRedisService.Setup(redis => redis.GetAsync<CurrentUserDto>(It.IsAny<string>()))
                .ReturnsAsync((CurrentUserDto)null!);

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId))
                .ReturnsAsync((User)null!);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _userService.GetUserDetails(userId));

            Assert.Contains($"User with ID {userId} not found", exception.Message);
            _mockLoggerService.Verify(logger => logger.Warn(It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region UpdateUserInfo Tests

        [Fact]
        public async Task UpdateUserInfo_WithValidChanges_UpdatesAndReturnsDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = "Original Name",
                DateOfBirth = new DateTime(1990, 1, 1),
                Sex = Gender.Male,
                CCCD = "123456789012",
                PhoneNumber = "1234567890",
                Address = "Original Address"
            };

            var updateDto = new UserUpdateDto
            {
                FullName = "Updated Name",
                DateOfBirth = new DateTime(1992, 2, 2),
                Sex = Gender.Female,
                CCCD = "987654321012",
                PhoneNumber = "9876543210",
                Address = "Updated Address"
            };

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.UpdateUserInfo(userId, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateDto.FullName, result.FullName);
            Assert.Equal(updateDto.DateOfBirth, result.DateOfBirth);
            Assert.Equal(updateDto.Sex, result.Sex);
            Assert.Equal(updateDto.CCCD, result.CCCD);
            Assert.Equal(updateDto.PhoneNumber, result.PhoneNumber);
            Assert.Equal(updateDto.Address, result.Address);

            _mockUserRepository.Verify(repo => repo.Update(It.Is<User>(u =>
                u.FullName == updateDto.FullName &&
                u.DateOfBirth == updateDto.DateOfBirth &&
                u.Sex == updateDto.Sex &&
                u.CCCD == updateDto.CCCD &&
                u.PhoneNumber == updateDto.PhoneNumber &&
                u.Address == updateDto.Address)), Times.Once);

            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
            _mockRedisService.Verify(redis => redis.RemoveAsync($"user:detail:{userId}"), Times.Once);
            _mockRedisService.Verify(redis => redis.RemoveByPatternAsync("admin:user:list:"), Times.Once);
            _mockLoggerService.Verify(logger => logger.Success(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateUserInfo_WithNoChanges_ReturnsExistingData()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = "Original Name",
                DateOfBirth = new DateTime(1990, 1, 1),
                Sex = Gender.Male,
                CCCD = "123456789012",
                PhoneNumber = "1234567890",
                Address = "Original Address"
            };

            var updateDto = new UserUpdateDto
            {
                FullName = "Original Name",
                DateOfBirth = new DateTime(1990, 1, 1),
                Sex = Gender.Male,
                CCCD = "123456789012",
                PhoneNumber = "1234567890",
                Address = "Original Address"
            };

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.UpdateUserInfo(userId, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.FullName, result.FullName);

            _mockUserRepository.Verify(repo => repo.Update(It.IsAny<User>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Never);
            _mockLoggerService.Verify(logger => logger.Warn(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateUserInfo_WithInvalidCCCD_ThrowsArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = "Original Name",
                CCCD = "123456789012"
            };

            var updateDto = new UserUpdateDto
            {
                CCCD = "123456" // Invalid - not 12 digits
            };

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _userService.UpdateUserInfo(userId, updateDto));

            Assert.Contains("Citizen ID must consist of exactly 12 digits", exception.Message);
        }

        [Fact]
        public async Task UpdateUserInfo_WithInvalidPhoneNumber_ThrowsArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = "Original Name",
                PhoneNumber = "1234567890"
            };

            var updateDto = new UserUpdateDto
            {
                PhoneNumber = "123" // Invalid - too short
            };

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _userService.UpdateUserInfo(userId, updateDto));

            Assert.Contains("Invalid phone number format", exception.Message);
        }

        [Fact]
        public async Task UpdateUserInfo_WithFutureDateOfBirth_ThrowsArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = "Original Name",
                DateOfBirth = new DateTime(1990, 1, 1)
            };

            var updateDto = new UserUpdateDto
            {
                DateOfBirth = DateTime.UtcNow.AddDays(1) // Future date
            };

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _userService.UpdateUserInfo(userId, updateDto));

            Assert.Contains("Date of birth cannot be in the future", exception.Message);
        }

        [Fact]
        public async Task UpdateUserInfo_WithNonExistentUser_ThrowsKeyNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var updateDto = new UserUpdateDto { FullName = "New Name" };

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId))
                .ReturnsAsync((User)null!);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _userService.UpdateUserInfo(userId, updateDto));

            Assert.Contains("User not found", exception.Message);
            _mockLoggerService.Verify(logger => logger.Warn(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateUserInfo_WithPartialUpdate_UpdatesOnlyChangedFields()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = "Original Name",
                DateOfBirth = new DateTime(1990, 1, 1),
                Sex = Gender.Male,
                CCCD = "123456789012",
                PhoneNumber = "1234567890",
                Address = "Original Address"
            };

            var updateDto = new UserUpdateDto
            {
                FullName = "Updated Name",
                // Only changing name, other fields remain null/default
            };

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.UpdateUserInfo(userId, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateDto.FullName, result.FullName);
            Assert.Equal(user.DateOfBirth, result.DateOfBirth);
            Assert.Equal(user.Sex, result.Sex);

            _mockUserRepository.Verify(repo => repo.Update(It.Is<User>(u =>
                u.FullName == updateDto.FullName &&
                u.DateOfBirth == user.DateOfBirth)), Times.Once);

            _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        #endregion
    }
}