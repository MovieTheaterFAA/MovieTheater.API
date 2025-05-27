using Microsoft.Extensions.Configuration;
using MovieTheater.Domain.DTOs.AuthenDTOs;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Domain.Enums;

namespace MovieTheater.Application.Interfaces;

public interface IAuthService
{
    Task<UserDto?> RegisterUserAsync(UserRegistrationDto registrationDto);
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginDto, IConfiguration configuration);
    Task<bool> LogoutAsync(Guid userId);
    Task<LoginResponseDto?> RefreshTokenAsync(TokenRefreshRequestDto refreshTokenDto, IConfiguration configuration);

    //OTP & emails
    Task<bool> ResendOtpAsync(string email, OtpPurpose otpPurpose);
    Task<bool> VerifyEmailOtpAsync(string email, string otp);
}