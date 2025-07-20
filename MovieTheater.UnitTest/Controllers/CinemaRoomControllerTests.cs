using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.API.Controllers;
using MovieTheater.Application.Interfaces;
using MovieTheater.Domain.DTOs.CinemaRoomDTOs;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.UnitTest.Controllers
{
    public class CinemaRoomControllerTests
    {
        private readonly Mock<ICinemaRoomService> _serviceMock;
        private readonly Mock<IClaimsService> _claimsServiceMock;
        private readonly CinemaRoomController _controller;

        public CinemaRoomControllerTests()
        {
            _serviceMock = new Mock<ICinemaRoomService>();
            _claimsServiceMock = new Mock<IClaimsService>();
            _controller = new CinemaRoomController(_serviceMock.Object, _claimsServiceMock.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WhenSuccess()
        {
            var pagination = new Pagination<CinemaRoomDto>(new List<CinemaRoomDto>(), 0, 1, 10);
            _serviceMock.Setup(s => s.GetAllCinemaRoomAsync(null, null, false, 1, 10))
                .ReturnsAsync(pagination);

            var result = await _controller.GetAll(null, null, false, 1, 10);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode ?? 200);
        }

        [Fact]
        public async Task GetAll_ReturnsError_WhenExceptionThrown()
        {
            _serviceMock.Setup(s => s.GetAllCinemaRoomAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Test exception"));

            var result = await _controller.GetAll(null, null, false, 1, 10);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode ?? 500);
        }

        [Fact]
        public async Task GetById_ReturnsOkResult_WhenFound()
        {
            var room = new CinemaRoomDto { Id = Guid.NewGuid(), Name = "Room 1", Type = RoomType.TwoD };
            _serviceMock.Setup(s => s.GetCinemaRoomByIdAsync(room.Id)).ReturnsAsync(room);

            var result = await _controller.GetById(room.Id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode ?? 200);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenNotFound()
        {
            _serviceMock.Setup(s => s.GetCinemaRoomByIdAsync(It.IsAny<Guid>())).ReturnsAsync((CinemaRoomDto)null!);

            var result = await _controller.GetById(Guid.NewGuid());

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode ?? 404);
        }

        [Fact]
        public async Task GetById_ReturnsError_WhenExceptionThrown()
        {
            _serviceMock.Setup(s => s.GetCinemaRoomByIdAsync(It.IsAny<Guid>())).ThrowsAsync(new Exception("Test exception"));

            var result = await _controller.GetById(Guid.NewGuid());

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode ?? 500);
        }

        [Fact]
        public async Task Create_ReturnsOkResult_WhenSuccess()
        {
            var dto = new CreateCinemaRoomDto { Name = "Room 1", Type = RoomType.IMAX };
            var created = new CinemaRoomDto { Id = Guid.NewGuid(), Name = dto.Name, Type = dto.Type };
            _claimsServiceMock.Setup(c => c.GetCurrentUserId).Returns(Guid.NewGuid());
            _serviceMock.Setup(s => s.CreateCinemaRoomAsync(dto, It.IsAny<Guid>())).ReturnsAsync(created);

            var result = await _controller.Create(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode ?? 200);
        }

        [Fact]
        public async Task Create_ReturnsError_WhenExceptionThrown()
        {
            var dto = new CreateCinemaRoomDto { Name = "Room 1", Type = RoomType.IMAX };
            _claimsServiceMock.Setup(c => c.GetCurrentUserId).Returns(Guid.NewGuid());
            _serviceMock.Setup(s => s.CreateCinemaRoomAsync(dto, It.IsAny<Guid>())).ThrowsAsync(new Exception("Test exception"));

            var result = await _controller.Create(dto);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode ?? 500);
        }

        [Fact]
        public async Task Update_ReturnsOkResult_WhenSuccess()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateCinemaRoomDto { Name = "Updated", Type = RoomType.FourD };
            var updated = new CinemaRoomDto { Id = id, Name = dto.Name, Type = dto.Type };
            _claimsServiceMock.Setup(c => c.GetCurrentUserId).Returns(Guid.NewGuid());
            _serviceMock.Setup(s => s.UpdateCinemaRoomAsync(id, dto, It.IsAny<Guid>())).ReturnsAsync(updated);

            var result = await _controller.Update(id, dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode ?? 200);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenNotFound()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateCinemaRoomDto { Name = "Updated", Type = RoomType.FourD };
            _claimsServiceMock.Setup(c => c.GetCurrentUserId).Returns(Guid.NewGuid());
            _serviceMock.Setup(s => s.UpdateCinemaRoomAsync(id, dto, It.IsAny<Guid>())).ReturnsAsync((CinemaRoomDto)null!);

            var result = await _controller.Update(id, dto);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode ?? 404);
        }

        [Fact]
        public async Task Update_ReturnsError_WhenExceptionThrown()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateCinemaRoomDto { Name = "Updated", Type = RoomType.FourD };
            _claimsServiceMock.Setup(c => c.GetCurrentUserId).Returns(Guid.NewGuid());
            _serviceMock.Setup(s => s.UpdateCinemaRoomAsync(id, dto, It.IsAny<Guid>())).ThrowsAsync(new Exception("Test exception"));

            var result = await _controller.Update(id, dto);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode ?? 500);
        }

        [Fact]
        public async Task Delete_ReturnsOkResult_WhenSuccess()
        {
            var id = Guid.NewGuid();
            _claimsServiceMock.Setup(c => c.GetCurrentUserId).Returns(Guid.NewGuid());
            _serviceMock.Setup(s => s.SoftDeleteCinemaRoomAsync(id, It.IsAny<Guid>())).ReturnsAsync(true);

            var result = await _controller.Delete(id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode ?? 200);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenNotFound()
        {
            var id = Guid.NewGuid();
            _claimsServiceMock.Setup(c => c.GetCurrentUserId).Returns(Guid.NewGuid());
            _serviceMock.Setup(s => s.SoftDeleteCinemaRoomAsync(id, It.IsAny<Guid>())).ReturnsAsync(false);

            var result = await _controller.Delete(id);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode ?? 404);
        }

        [Fact]
        public async Task Delete_ReturnsError_WhenExceptionThrown()
        {
            var id = Guid.NewGuid();
            _claimsServiceMock.Setup(c => c.GetCurrentUserId).Returns(Guid.NewGuid());
            _serviceMock.Setup(s => s.SoftDeleteCinemaRoomAsync(id, It.IsAny<Guid>())).ThrowsAsync(new Exception("Test exception"));

            var result = await _controller.Delete(id);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode ?? 500);
        }
    }
}