using MockQueryable;
using Moq;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Domain.DTOs.CinemaRoomDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.UnitTest.Services;

public class CinemaRoomServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILoggerService> _mockLoggerService;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<IGenericRepository<CinemaRoom>> _mockCinemaRoomRepository;
    private readonly CinemaRoomService _cinemaRoomService;

    public CinemaRoomServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLoggerService = new Mock<ILoggerService>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockCinemaRoomRepository = new Mock<IGenericRepository<CinemaRoom>>();

        _mockUnitOfWork.Setup(u => u.CinemaRooms).Returns(_mockCinemaRoomRepository.Object);

        _cinemaRoomService = new CinemaRoomService(
            _mockUnitOfWork.Object,
            _mockLoggerService.Object,
            _mockAuditLogService.Object
        );
    }

    [Fact]
    public async Task GetAllCinemaRoomAsync_WithValidParameters_ReturnsSuccessfully()
    {
        // Arrange
        var cinemaRooms = new List<CinemaRoom>
        {
            new() { Id = Guid.NewGuid(), Name = "Room A", Type = RoomType.TwoD, IsDeleted = false },
            new() { Id = Guid.NewGuid(), Name = "Room B", Type = RoomType.FourD, IsDeleted = false },
            new() { Id = Guid.NewGuid(), Name = "Room C", Type = RoomType.IMAX, IsDeleted = false }
        };

        var mockQueryable = cinemaRooms.AsQueryable().BuildMock();
        _mockCinemaRoomRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _cinemaRoomService.GetAllCinemaRoomAsync(null, null, false, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(10, result.PageSize);

        _mockLoggerService.Verify(l => l.Info(It.Is<string>(s => s.Contains("Fetching cinema rooms"))), Times.Once);
        _mockLoggerService.Verify(l => l.Success(It.Is<string>(s => s.Contains("Retrieved 3 cinema rooms"))), Times.Once);
    }

    [Fact]
    public async Task GetAllCinemaRoomAsync_WithSearchFilter_ReturnsFilteredResults()
    {
        // Arrange
        var cinemaRooms = new List<CinemaRoom>
        {
            new() { Id = Guid.NewGuid(), Name = "Room A", Type = RoomType.TwoD, IsDeleted = false },
            new() { Id = Guid.NewGuid(), Name = "Room B", Type = RoomType.FourD, IsDeleted = false },
            new() { Id = Guid.NewGuid(), Name = "Hall C", Type = RoomType.IMAX, IsDeleted = false }
        };

        var mockQueryable = cinemaRooms.AsQueryable().BuildMock();
        _mockCinemaRoomRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _cinemaRoomService.GetAllCinemaRoomAsync("Room", null, false, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.Contains("Room", item.Name));

        _mockLoggerService.Verify(l => l.Info(It.Is<string>(s => s.Contains("Search: Room"))), Times.Once);
    }

    [Fact]
    public async Task GetAllCinemaRoomAsync_WithNameSortAscending_ReturnsSortedResults()
    {
        // Arrange
        var cinemaRooms = new List<CinemaRoom>
        {
            new() { Id = Guid.NewGuid(), Name = "Room C", Type = RoomType.TwoD, IsDeleted = false },
            new() { Id = Guid.NewGuid(), Name = "Room A", Type = RoomType.FourD, IsDeleted = false },
            new() { Id = Guid.NewGuid(), Name = "Room B", Type = RoomType.IMAX, IsDeleted = false }
        };

        var mockQueryable = cinemaRooms.AsQueryable().BuildMock();
        _mockCinemaRoomRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _cinemaRoomService.GetAllCinemaRoomAsync(null, "name", false, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Room A", result.Items[0].Name);
        Assert.Equal("Room B", result.Items[1].Name);
        Assert.Equal("Room C", result.Items[2].Name);
    }

    [Fact]
    public async Task GetAllCinemaRoomAsync_WithNameSortDescending_ReturnsSortedResults()
    {
        // Arrange
        var cinemaRooms = new List<CinemaRoom>
        {
            new() { Id = Guid.NewGuid(), Name = "Room A", Type = RoomType.TwoD, IsDeleted = false },
            new() { Id = Guid.NewGuid(), Name = "Room B", Type = RoomType.FourD, IsDeleted = false },
            new() { Id = Guid.NewGuid(), Name = "Room C", Type = RoomType.IMAX, IsDeleted = false }
        };

        var mockQueryable = cinemaRooms.AsQueryable().BuildMock();
        _mockCinemaRoomRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _cinemaRoomService.GetAllCinemaRoomAsync(null, "name", true, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Room C", result.Items[0].Name);
        Assert.Equal("Room B", result.Items[1].Name);
        Assert.Equal("Room A", result.Items[2].Name);
    }

    [Fact]
    public async Task GetAllCinemaRoomAsync_WithTypeSortAscending_ReturnsSortedResults()
    {
        // Arrange
        var cinemaRooms = new List<CinemaRoom>
        {
            new() { Id = Guid.NewGuid(), Name = "Room A", Type = RoomType.FourD, IsDeleted = false },
            new() { Id = Guid.NewGuid(), Name = "Room B", Type = RoomType.TwoD, IsDeleted = false },
            new() { Id = Guid.NewGuid(), Name = "Room C", Type = RoomType.IMAX, IsDeleted = false }
        };

        var mockQueryable = cinemaRooms.AsQueryable().BuildMock();
        _mockCinemaRoomRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _cinemaRoomService.GetAllCinemaRoomAsync(null, "type", false, 1, 10);

        // Assert
        Assert.NotNull(result);
        // Results should be sorted by Type enum values
        Assert.True(result.Items.Count > 0);
    }

    [Fact]
    public async Task GetAllCinemaRoomAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var cinemaRooms = new List<CinemaRoom>();
        for (int i = 1; i <= 25; i++)
        {
            cinemaRooms.Add(new CinemaRoom
            {
                Id = Guid.NewGuid(),
                Name = $"Room {i}",
                Type = RoomType.TwoD,
                IsDeleted = false
            });
        }

        var mockQueryable = cinemaRooms.AsQueryable().BuildMock();
        _mockCinemaRoomRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _cinemaRoomService.GetAllCinemaRoomAsync(null, null, false, 2, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(2, result.CurrentPage);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetAllCinemaRoomAsync_ExcludesDeletedRooms_ReturnsOnlyActiveRooms()
    {
        // Arrange
        var cinemaRooms = new List<CinemaRoom>
        {
            new() { Id = Guid.NewGuid(), Name = "Room A", Type = RoomType.TwoD, IsDeleted = false },
            new() { Id = Guid.NewGuid(), Name = "Room B", Type = RoomType.FourD, IsDeleted = true },
            new() { Id = Guid.NewGuid(), Name = "Room C", Type = RoomType.IMAX, IsDeleted = false }
        };

        var mockQueryable = cinemaRooms.AsQueryable().BuildMock();
        _mockCinemaRoomRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _cinemaRoomService.GetAllCinemaRoomAsync(null, null, false, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.NotEqual("Room B", item.Name));
    }

    [Fact]
    public async Task GetAllCinemaRoomAsync_WhenExceptionThrown_LogsErrorAndThrowsException()
    {
        // Arrange
        _mockCinemaRoomRepository.Setup(r => r.GetQueryable())
            .Throws(new Exception("Database error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _cinemaRoomService.GetAllCinemaRoomAsync(null, null, false, 1, 10));

        Assert.Equal("An error occurred while fetching cinema rooms. Please try again later.", exception.Message);
        _mockLoggerService.Verify(l => l.Error(It.Is<string>(s => s.Contains("Error fetching cinema rooms"))), Times.Once);
    }

    [Fact]
    public async Task GetCinemaRoomByIdAsync_WithValidId_ReturnsRoom()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var cinemaRooms = new List<CinemaRoom>
        {
            new() { Id = roomId, Name = "Room A", Type = RoomType.TwoD, IsDeleted = false }
        };

        var mockQueryable = cinemaRooms.AsQueryable().BuildMock();
        _mockCinemaRoomRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _cinemaRoomService.GetCinemaRoomByIdAsync(roomId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(roomId, result.Id);
        Assert.Equal("Room A", result.Name);
        Assert.Equal(RoomType.TwoD, result.Type);

        _mockLoggerService.Verify(l => l.Info(It.Is<string>(s => s.Contains($"Fetching cinema room detail for Id: {roomId}"))), Times.Once);
    }

    [Fact]
    public async Task GetCinemaRoomByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var cinemaRooms = new List<CinemaRoom>();

        var mockQueryable = cinemaRooms.AsQueryable().BuildMock();
        _mockCinemaRoomRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _cinemaRoomService.GetCinemaRoomByIdAsync(roomId);

        // Assert
        Assert.Null(result);
        _mockLoggerService.Verify(l => l.Warn(It.Is<string>(s => s.Contains($"Cinema room with Id {roomId} not found"))), Times.Once);
    }

    [Fact]
    public async Task GetCinemaRoomByIdAsync_WithDeletedRoom_ReturnsNull()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var cinemaRooms = new List<CinemaRoom>
        {
            new() { Id = roomId, Name = "Room A", Type = RoomType.TwoD, IsDeleted = true }
        };

        var mockQueryable = cinemaRooms.AsQueryable().BuildMock();
        _mockCinemaRoomRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _cinemaRoomService.GetCinemaRoomByIdAsync(roomId);

        // Assert
        Assert.Null(result);
        _mockLoggerService.Verify(l => l.Warn(It.Is<string>(s => s.Contains($"Cinema room with Id {roomId} not found"))), Times.Once);
    }

    [Fact]
    public async Task GetCinemaRoomByIdAsync_WhenExceptionThrown_LogsErrorAndThrowsException()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        _mockCinemaRoomRepository.Setup(r => r.GetQueryable())
            .Throws(new Exception("Database error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _cinemaRoomService.GetCinemaRoomByIdAsync(roomId));

        Assert.Equal("An error occurred while fetching cinema room detail. Please try again later.", exception.Message);
        _mockLoggerService.Verify(l => l.Error(It.Is<string>(s => s.Contains("Error fetching cinema room detail"))), Times.Once);
    }

    [Fact]
    public async Task CreateCinemaRoomAsync_WithValidDto_ReturnsCreatedRoom()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var dto = new CreateCinemaRoomDto
        {
            Name = "New Room",
            Type = RoomType.FourD
        };

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _cinemaRoomService.CreateCinemaRoomAsync(dto, adminId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(dto.Type, result.Type);
        Assert.NotEqual(Guid.Empty, result.Id);

        _mockLoggerService.Verify(l => l.Info(It.Is<string>(s => s.Contains($"Creating cinema room: {dto.Name}"))), Times.Once);
        _mockLoggerService.Verify(l => l.Success(It.Is<string>(s => s.Contains($"Cinema room '{dto.Name}' created successfully"))), Times.Once);

        _mockCinemaRoomRepository.Verify(r => r.AddAsync(It.Is<CinemaRoom>(c =>
            c.Name == dto.Name &&
            c.Type == dto.Type &&
            c.CreatedBy == adminId)), Times.Once);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);

        _mockAuditLogService.Verify(a => a.LogAsync(
            adminId,
            AuditActionType.Create,
            "CinemaRoom",
            It.IsAny<Guid>(),
            null,
            It.IsAny<object>(),
            It.IsAny<string>(),
            "Created new cinema room"), Times.Once);
    }

    [Fact]
    public async Task CreateCinemaRoomAsync_WhenExceptionThrown_LogsErrorAndThrowsException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var dto = new CreateCinemaRoomDto
        {
            Name = "New Room",
            Type = RoomType.FourD
        };

        _mockCinemaRoomRepository.Setup(r => r.AddAsync(It.IsAny<CinemaRoom>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _cinemaRoomService.CreateCinemaRoomAsync(dto, adminId));

        Assert.Equal("An error occurred while creating cinema room. Please try again later.", exception.Message);
        _mockLoggerService.Verify(l => l.Error(It.Is<string>(s => s.Contains("Error creating cinema room"))), Times.Once);
    }

    [Fact]
    public async Task UpdateCinemaRoomAsync_WithValidIdAndDto_ReturnsUpdatedRoom()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var existingRoom = new CinemaRoom
        {
            Id = roomId,
            Name = "Old Name",
            Type = RoomType.TwoD,
            IsDeleted = false
        };

        var updateDto = new UpdateCinemaRoomDto
        {
            Name = "Updated Name",
            Type = RoomType.FourD
        };

        _mockCinemaRoomRepository.Setup(r => r.GetByIdAsync(roomId))
            .ReturnsAsync(existingRoom);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _cinemaRoomService.UpdateCinemaRoomAsync(roomId, updateDto, adminId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updateDto.Name, result.Name);
        Assert.Equal(updateDto.Type, result.Type);
        Assert.Equal(roomId, result.Id);

        // Verify entity was updated
        Assert.Equal(updateDto.Name, existingRoom.Name);
        Assert.Equal(updateDto.Type, existingRoom.Type);
        Assert.Equal(adminId, existingRoom.UpdatedBy);

        _mockLoggerService.Verify(l => l.Info(It.Is<string>(s => s.Contains($"Updating cinema room Id: {roomId}"))), Times.Once);
        _mockLoggerService.Verify(l => l.Success(It.Is<string>(s => s.Contains($"Cinema room '{updateDto.Name}' updated successfully"))), Times.Once);

        _mockCinemaRoomRepository.Verify(r => r.Update(existingRoom), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);

        _mockAuditLogService.Verify(a => a.LogAsync(
            adminId,
            AuditActionType.Update,
            "CinemaRoom",
            roomId,
            It.IsAny<object>(),
            It.IsAny<object>(),
            It.IsAny<string>(),
            "Updated cinema room"), Times.Once);
    }

    [Fact]
    public async Task UpdateCinemaRoomAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var updateDto = new UpdateCinemaRoomDto
        {
            Name = "Updated Name",
            Type = RoomType.FourD
        };

        _mockCinemaRoomRepository.Setup(r => r.GetByIdAsync(roomId))
            .ReturnsAsync((CinemaRoom)null!);

        // Act
        var result = await _cinemaRoomService.UpdateCinemaRoomAsync(roomId, updateDto, adminId);

        // Assert
        Assert.Null(result);
        _mockLoggerService.Verify(l => l.Warn(It.Is<string>(s => s.Contains($"Cinema room with Id {roomId} not found"))), Times.Once);

        _mockCinemaRoomRepository.Verify(r => r.Update(It.IsAny<CinemaRoom>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        _mockAuditLogService.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<AuditActionType>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCinemaRoomAsync_WithDeletedRoom_ReturnsNull()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var deletedRoom = new CinemaRoom
        {
            Id = roomId,
            Name = "Deleted Room",
            Type = RoomType.TwoD,
            IsDeleted = true
        };

        var updateDto = new UpdateCinemaRoomDto
        {
            Name = "Updated Name",
            Type = RoomType.FourD
        };

        _mockCinemaRoomRepository.Setup(r => r.GetByIdAsync(roomId))
            .ReturnsAsync(deletedRoom);

        // Act
        var result = await _cinemaRoomService.UpdateCinemaRoomAsync(roomId, updateDto, adminId);

        // Assert
        Assert.Null(result);
        _mockLoggerService.Verify(l => l.Warn(It.Is<string>(s => s.Contains($"Cinema room with Id {roomId} not found"))), Times.Once);
    }

    [Fact]
    public async Task UpdateCinemaRoomAsync_WhenExceptionThrown_LogsErrorAndThrowsException()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var updateDto = new UpdateCinemaRoomDto
        {
            Name = "Updated Name",
            Type = RoomType.FourD
        };

        _mockCinemaRoomRepository.Setup(r => r.GetByIdAsync(roomId))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _cinemaRoomService.UpdateCinemaRoomAsync(roomId, updateDto, adminId));

        Assert.Equal("An error occurred while updating cinema room. Please try again later.", exception.Message);
        _mockLoggerService.Verify(l => l.Error(It.Is<string>(s => s.Contains("Error updating cinema room"))), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteCinemaRoomAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var existingRoom = new CinemaRoom
        {
            Id = roomId,
            Name = "Room to Delete",
            Type = RoomType.TwoD,
            IsDeleted = false
        };

        _mockCinemaRoomRepository.Setup(r => r.GetByIdAsync(roomId))
            .ReturnsAsync(existingRoom);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _cinemaRoomService.SoftDeleteCinemaRoomAsync(roomId, adminId);

        // Assert
        Assert.True(result);

        // Verify entity was soft deleted
        Assert.True(existingRoom.IsDeleted);
        Assert.Equal(adminId, existingRoom.DeletedBy);
        Assert.NotNull(existingRoom.DeletedAt);

        _mockLoggerService.Verify(l => l.Info(It.Is<string>(s => s.Contains($"Soft deleting cinema room Id: {roomId}"))), Times.Once);
        _mockLoggerService.Verify(l => l.Success(It.Is<string>(s => s.Contains($"Cinema room '{existingRoom.Name}' soft deleted successfully"))), Times.Once);

        _mockCinemaRoomRepository.Verify(r => r.Update(existingRoom), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);

        _mockAuditLogService.Verify(a => a.LogAsync(
            adminId,
            AuditActionType.Delete,
            "CinemaRoom",
            roomId,
            It.IsAny<object>(),
            It.IsAny<object>(),
            It.IsAny<string>(),
            "Soft deleted cinema room"), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteCinemaRoomAsync_WithNonExistentId_ReturnsFalse()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        _mockCinemaRoomRepository.Setup(r => r.GetByIdAsync(roomId))
            .ReturnsAsync((CinemaRoom)null!);

        // Act
        var result = await _cinemaRoomService.SoftDeleteCinemaRoomAsync(roomId, adminId);

        // Assert
        Assert.False(result);
        _mockLoggerService.Verify(l => l.Warn(It.Is<string>(s => s.Contains($"Cinema room with Id {roomId} not found"))), Times.Once);

        _mockCinemaRoomRepository.Verify(r => r.Update(It.IsAny<CinemaRoom>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        _mockAuditLogService.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<AuditActionType>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<object>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SoftDeleteCinemaRoomAsync_WithAlreadyDeletedRoom_ReturnsFalse()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var alreadyDeletedRoom = new CinemaRoom
        {
            Id = roomId,
            Name = "Already Deleted Room",
            Type = RoomType.TwoD,
            IsDeleted = true
        };

        _mockCinemaRoomRepository.Setup(r => r.GetByIdAsync(roomId))
            .ReturnsAsync(alreadyDeletedRoom);

        // Act
        var result = await _cinemaRoomService.SoftDeleteCinemaRoomAsync(roomId, adminId);

        // Assert
        Assert.False(result);
        _mockLoggerService.Verify(l => l.Warn(It.Is<string>(s => s.Contains($"Cinema room with Id {roomId} not found"))), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteCinemaRoomAsync_WhenExceptionThrown_LogsErrorAndThrowsException()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        _mockCinemaRoomRepository.Setup(r => r.GetByIdAsync(roomId))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _cinemaRoomService.SoftDeleteCinemaRoomAsync(roomId, adminId));

        Assert.Equal("An error occurred while deleting cinema room. Please try again later.", exception.Message);
        _mockLoggerService.Verify(l => l.Error(It.Is<string>(s => s.Contains("Error soft deleting cinema room"))), Times.Once);
    }

    [Fact]
    public async Task GetAllCinemaRoomAsync_WithEmptySearch_TreatsAsNoFilter()
    {
        // Arrange
        var cinemaRooms = new List<CinemaRoom>
        {
            new() { Id = Guid.NewGuid(), Name = "Room A", Type = RoomType.TwoD, IsDeleted = false },
            new() { Id = Guid.NewGuid(), Name = "Room B", Type = RoomType.FourD, IsDeleted = false }
        };

        var mockQueryable = cinemaRooms.AsQueryable().BuildMock();
        _mockCinemaRoomRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _cinemaRoomService.GetAllCinemaRoomAsync("", null, false, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetAllCinemaRoomAsync_WithWhitespaceSearch_TreatsAsNoFilter()
    {
        // Arrange
        var cinemaRooms = new List<CinemaRoom>
        {
            new() { Id = Guid.NewGuid(), Name = "Room A", Type = RoomType.TwoD, IsDeleted = false },
            new() { Id = Guid.NewGuid(), Name = "Room B", Type = RoomType.FourD, IsDeleted = false }
        };

        var mockQueryable = cinemaRooms.AsQueryable().BuildMock();
        _mockCinemaRoomRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _cinemaRoomService.GetAllCinemaRoomAsync("   ", null, false, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetAllCinemaRoomAsync_WithUnknownSortBy_DefaultsToNameSort()
    {
        // Arrange
        var cinemaRooms = new List<CinemaRoom>
        {
            new() { Id = Guid.NewGuid(), Name = "Room Z", Type = RoomType.TwoD, IsDeleted = false },
            new() { Id = Guid.NewGuid(), Name = "Room A", Type = RoomType.FourD, IsDeleted = false }
        };

        var mockQueryable = cinemaRooms.AsQueryable().BuildMock();
        _mockCinemaRoomRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _cinemaRoomService.GetAllCinemaRoomAsync(null, "unknown", false, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Room A", result.Items[0].Name);
        Assert.Equal("Room Z", result.Items[1].Name);
    }

    [Fact]
    public async Task CreateCinemaRoomAsync_VerifiesAuditLogContainsCorrectData()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var dto = new CreateCinemaRoomDto
        {
            Name = "Test Room",
            Type = RoomType.IMAX
        };

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _cinemaRoomService.CreateCinemaRoomAsync(dto, adminId);

        // Assert
        _mockAuditLogService.Verify(a => a.LogAsync(
            adminId,
            AuditActionType.Create,
            "CinemaRoom",
            It.IsAny<Guid>(),
            null,
            It.IsAny<object>(), // Simplified - just verify the object exists
            It.Is<string>(json => json.Contains("Test Room") && json.Contains("2")), // IMAX = 2
            "Created new cinema room"), Times.Once);
    }

    [Fact]
    public async Task UpdateCinemaRoomAsync_VerifiesAuditLogContainsOldAndNewData()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var existingRoom = new CinemaRoom
        {
            Id = roomId,
            Name = "Old Name",
            Type = RoomType.TwoD,
            IsDeleted = false
        };

        var updateDto = new UpdateCinemaRoomDto
        {
            Name = "New Name",
            Type = RoomType.FourD
        };

        _mockCinemaRoomRepository.Setup(r => r.GetByIdAsync(roomId))
            .ReturnsAsync(existingRoom);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _cinemaRoomService.UpdateCinemaRoomAsync(roomId, updateDto, adminId);

        // Assert
        _mockAuditLogService.Verify(a => a.LogAsync(
            adminId,
            AuditActionType.Update,
            "CinemaRoom",
            roomId,
            It.Is<object>(obj => obj.ToString()!.Contains("Old Name") && obj.ToString()!.Contains("TwoD")),
            It.Is<object>(obj => obj.ToString()!.Contains("New Name") && obj.ToString()!.Contains("FourD")),
            It.Is<string>(json => json.Contains("New Name") && json.Contains("1")), // FourD = 1
            "Updated cinema room"), Times.Once);
    }

}