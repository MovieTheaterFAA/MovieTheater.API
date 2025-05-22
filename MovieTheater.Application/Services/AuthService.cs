using BlindTreasure.Application.Utils;
using Microsoft.Extensions.Configuration;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.AuthenDTOs;
using MovieTheater.Domain.DTOs.EmailDTOs;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Application.Services
{
    /// <summary>
    ///     Service for authentication and authorization operations, including login, logout, and user registration, OTP, and refresh token.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IEmailService _emailService;
        private readonly ILoggerService _loggerService;
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(IUnitOfWork unitOfWork, IEmailService emailService, ILoggerService loggerService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _loggerService = loggerService;
        }

        public async Task<UserDto?> RegisterUserAsync(UserRegistrationDto registrationDto)
        {
            _loggerService.Info($"[RegisterUserAsync] Start registration for {registrationDto.Email}");

            if (await UserExistsAsync(registrationDto.Email))
            {
                _loggerService.Warn($"[RegisterUserAsync] Email {registrationDto.Email} already registered.");
                throw ErrorHelper.Conflict("Email đã được sử dụng.");
            }

            var hashedPassword = new PasswordHasher().HashPassword(registrationDto.Password);

            var user = new User
            {
                Email = registrationDto.Email,
                Password = hashedPassword,
                FullName = registrationDto.FullName,
                PhoneNumber = registrationDto.PhoneNumber,
                DateOfBirth = registrationDto.DateOfBirth,
                UserStatus = UserStatus.Pending,
                Role = RoleType.Member,
                IsEmailVerified = false
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.Success($"[RegisterUserAsync] User {user.Email} created successfully.");

            await GenerateAndSendOtpAsync(user, OtpPurpose.Register, "register-otp");

            _loggerService.Info($"[RegisterUserAsync] OTP sent to {user.Email} for verification.");

            return ToUserDto(user);
        }

        public Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginDto, IConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        public Task<bool> LogoutAsync(Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<LoginResponseDto?> RefreshTokenAsync(TokenRefreshRequestDto refreshTokenDto, IConfiguration configuration)
        {
            throw new NotImplementedException();
        }



        //========================= PRIVATE HELPER METHODS ============================

        /// <summary>
        ///     Check những user đã tồn tại trong hệ thống hay chưa.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        private async Task<bool> UserExistsAsync(string email)
        {
            var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
            return existingUser != null;
        }


        private async Task GenerateAndSendOtpAsync(User user, OtpPurpose purpose, string otpCachePrefix)
        {
            var otpToken = OtpGenerator.GenerateToken(6, TimeSpan.FromMinutes(10));
            var otp = new OtpVerification
            {
                Target = user.Email,
                OtpCode = otpToken.Code,
                ExpiredAt = otpToken.ExpiresAtUtc,
                IsUsed = false,
                Purpose = purpose
            };

            await _unitOfWork.OtpVerifications.AddAsync(otp);
            await _unitOfWork.SaveChangesAsync();
            await _cacheService.SetAsync($"{otpCachePrefix}:{user.Email}", otpToken.Code, TimeSpan.FromMinutes(10));

            // Send the correct email based on OTP purpose
            if (purpose == OtpPurpose.Register)
            {
                await _emailService.SendOtpVerificationEmailAsync(new EmailRequestDto
                {
                    To = user.Email,
                    Otp = otpToken.Code,
                    UserName = user.FullName
                });
                _logger.Info($"[GenerateAndSendOtpAsync] Registration OTP sent to {user.Email}");
            }
            else if (purpose == OtpPurpose.ForgotPassword)
            {
                await _emailService.SendForgotPasswordOtpEmailAsync(new EmailRequestDto
                {
                    To = user.Email,
                    Otp = otpToken.Code,
                    UserName = user.FullName
                });
                _logger.Info($"[GenerateAndSendOtpAsync] Forgot password OTP sent to {user.Email}");
            }
        }
    }
}
