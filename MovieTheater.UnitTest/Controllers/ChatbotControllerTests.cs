using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.API.Controllers;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.ChatbotDTOs;


namespace MovieTheater.UnitTest.Controllers
{
    public class ChatbotControllerTests
    {
        private readonly Mock<IChatbotService> _chatbotServiceMock;
        private readonly ChatbotController _controller;

        public ChatbotControllerTests()
        {
            _chatbotServiceMock = new Mock<IChatbotService>();
            _controller = new ChatbotController(_chatbotServiceMock.Object);
        }

        [Fact]
        public async Task AskMember_ReturnsOk_WhenPromptIsValid()
        {
            // Arrange
            var request = new AskMemberRequestDto { Prompt = "Hello", GroupId = null };
            var response = "Hi there!";
            _chatbotServiceMock.Setup(s => s.FreestyleAskAsync(request.Prompt, request.GroupId))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.AskMember(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<string>>(okResult.Value);
            Assert.True(apiResult.IsSuccess);
            Assert.Equal(response, apiResult.Value.Data);
            Assert.Equal("200", apiResult.Value.Code);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task AskMember_ReturnsBadRequest_WhenPromptIsNullOrWhiteSpace(string prompt)
        {
            // Arrange
            var request = new AskMemberRequestDto { Prompt = prompt };

            // Act
            var result = await _controller.AskMember(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("400", apiResult.Error.Code);
            Assert.Equal("Prompt is required.", apiResult.Error.Message);
        }

        [Fact]
        public async Task AskMember_ReturnsBadRequest_WhenRequestIsNull()
        {
            // Act
            var result = await _controller.AskMember(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResult = Assert.IsType<ApiResult<object>>(badRequestResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("400", apiResult.Error.Code);
            Assert.Equal("Prompt is required.", apiResult.Error.Message);
        }

        [Fact]
        public async Task AskMember_Exception_ReturnsErrorResponse()
        {
            // Arrange
            var request = new AskMemberRequestDto { Prompt = "Hello" };
            _chatbotServiceMock.Setup(s => s.FreestyleAskAsync(request.Prompt, request.GroupId))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.AskMember(request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            var apiResult = Assert.IsType<ApiResult<object>>(objectResult.Value);
            Assert.False(apiResult.IsSuccess);
            Assert.Equal("500", apiResult.Error.Code);
        }
    }
}