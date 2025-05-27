using MovieTheater.Domain.DTOs.EmailDTOs;

namespace MovieTheater.Application.Interfaces;

public interface IEmailService
{
    Task SendRegistrationSuccessEmailAsync(EmailRequestDto request);
    Task SendOtpVerificationEmailAsync(EmailRequestDto request);
}