using Microsoft.Extensions.Configuration;
using Moq;
using MovieTheater.Application.Services;
using MovieTheater.Domain.DTOs.EmailDTOs;
using MovieTheater.Domain.Enums;
using Resend;

namespace MovieTheater.UnitTest.Services
{
    public class EmailServiceTests
    {
        private readonly Mock<IResend> _resendMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly EmailService _emailService;

        public EmailServiceTests()
        {
            _resendMock = new Mock<IResend>();
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(x => x["RESEND_FROM"]).Returns("test@example.com");
            _emailService = new EmailService(_resendMock.Object, _configurationMock.Object);
        }

        [Fact]
        public async Task SendRegistrationSuccessEmailAsync_ShouldSendEmailWithCorrectParameters()
        {
            // Arrange
            var request = new EmailRequestDto
            {
                To = "user@example.com",
                UserName = "TestUser"
            };

            // Act
            await _emailService.SendRegistrationSuccessEmailAsync(request);

            // Assert
            _resendMock.Verify(x => x.EmailSendAsync(It.Is<EmailMessage>(m =>
                m.From.Email == "test@example.com" &&
                m.To.Contains("user@example.com") &&
                m.Subject == "Signed" &&
                m.HtmlBody!.Contains("TestUser")
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendOtpVerificationEmailAsync_ShouldSendEmailWithCorrectParameters()
        {
            // Arrange
            var request = new EmailRequestDto
            {
                To = "user@example.com",
                Otp = "123456"
            };

            // Act
            await _emailService.SendOtpVerificationEmailAsync(request);

            // Assert
            _resendMock.Verify(x => x.EmailSendAsync(It.Is<EmailMessage>(m =>
                m.From.Email == "test@example.com" &&
                m.To.Contains("user@example.com") &&
                m.Subject == "OTP authentication at MovieTheater" &&
                m.HtmlBody!.Contains("123456")
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendForgotPasswordOtpEmailAsync_ShouldSendEmailWithCorrectParameters()
        {
            // Arrange
            var request = new EmailRequestDto
            {
                To = "user@example.com",
                Otp = "123456"
            };

            // Act
            await _emailService.SendForgotPasswordOtpEmailAsync(request);

            // Assert
            _resendMock.Verify(x => x.EmailSendAsync(It.Is<EmailMessage>(m =>
                m.From.Email == "test@example.com" &&
                m.To.Contains("user@example.com") &&
                m.Subject == "OTP password recovery at MovieTheater" &&
                m.HtmlBody!.Contains("123456")
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendPasswordChangeSuccessAsync_ShouldSendEmailWithCorrectParameters()
        {
            // Arrange
            var request = new EmailRequestDto
            {
                To = "user@example.com",
                UserName = "TestUser"
            };

            // Act
            await _emailService.SendPasswordChangeSuccessAsync(request);

            // Assert
            _resendMock.Verify(x => x.EmailSendAsync(It.Is<EmailMessage>(m =>
                m.From.Email == "test@example.com" &&
                m.To.Contains("user@example.com") &&
                m.Subject == "Password has been changed at MovieTheater" &&
                m.HtmlBody!.Contains("Hello TestUser")
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendEmployeeCredentialsEmailAsync_ShouldSendEmailWithCorrectParameters()
        {
            // Arrange
            var request = new EmployeeCredentialsEmailDto
            {
                To = "employee@example.com",
                UserName = "EmployeeUser",
                Password = "Password123"
            };

            // Act
            await _emailService.SendEmployeeCredentialsEmailAsync(request);

            // Assert
            _resendMock.Verify(x => x.EmailSendAsync(It.Is<EmailMessage>(m =>
                m.From.Email == "test@example.com" &&
                m.To.Contains("employee@example.com") &&
                m.Subject == "Your Account Credentials" &&
                m.HtmlBody!.Contains("Welcome EmployeeUser") &&
                m.HtmlBody!.Contains("Password123")
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendUpdateEmployeeCredentialsEmailAsync_ShouldSendEmailWithCorrectParameters()
        {
            // Arrange
            var request = new UpdateEmployeeCredentialsEmailDto
            {
                To = "employee@example.com",
                UserName = "EmployeeUser",
                Password = "Password123",
                FullName = "John Doe",
                DateOfBirth = new DateTime(1990, 1, 1),
                Sex = Gender.Male,
                CCCD = "123456789",
                PhoneNumber = "0123456789",
                Address = "123 Main St"
            };

            // Act
            await _emailService.SendUpdateEmployeeCredentialsEmailAsync(request);

            // Assert
            _resendMock.Verify(x => x.EmailSendAsync(It.Is<EmailMessage>(m =>
                m.From.Email == "test@example.com" &&
                m.To.Contains("employee@example.com") &&
                m.Subject == "Your Account Has Been Update Credentials" &&
                m.HtmlBody!.Contains("John Doe") &&
                m.HtmlBody.Contains("Password123") &&
                m.HtmlBody.Contains("123456789") &&
                m.HtmlBody.Contains("0123456789") &&
                m.HtmlBody.Contains("123 Main St")
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Constructor_WithNoResendFromConfig_ShouldUseDefaultEmail()
        {
            // Arrange
            var resendMock = new Mock<IResend>();
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(x => x["RESEND_FROM"]).Returns((string)null!);

            // Act
            var emailService = new EmailService(resendMock.Object, configMock.Object);

            // No direct way to test this, but we can verify behavior when sending an email
            var request = new EmailRequestDto { To = "user@example.com", UserName = "TestUser" };
            await emailService.SendRegistrationSuccessEmailAsync(request);

            // Assert
            resendMock.Verify(x => x.EmailSendAsync(It.Is<EmailMessage>(m =>
                m.From.Email == "noreply@movie-theater.com"
            ), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}