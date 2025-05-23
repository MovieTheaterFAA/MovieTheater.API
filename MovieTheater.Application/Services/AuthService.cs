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

        /// <summary>
        ///     Register a new user.
        /// </summary>
        /// <param name="registrationDto"></param>
        /// <returns></returns>
        public async Task<UserDto?> RegisterUserAsync(UserRegistrationDto registrationDto)
        {
            _loggerService.Info($"[RegisterUserAsync] Start registration for {registrationDto.Email}");

            if (await UserExistsAsync(registrationDto.Email))
            {
                _loggerService.Warn($"[RegisterUserAsync] Email {registrationDto.Email} already registered.");
                throw ErrorHelper.Conflict("Email have been used.");
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

            await GenerateAndSendOtpAsync(user, OtpPurpose.Register);

            _loggerService.Info($"[RegisterUserAsync] OTP sent to {user.Email} for verification.");

            return ToUserDto(user);
        }

        /// <summary>
        ///     Login a user and return JWT access and refresh token.
        /// </summary>
        /// <param name="loginDto"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginDto, IConfiguration configuration)
        {
            _loggerService.Info($"[LoginAsync] Login attempt for {loginDto.Email}");

            // Get user from DB
            var user = await GetUserByEmailAsync(loginDto.Email!);
            if (user == null)
                throw ErrorHelper.NotFound("Account does not exist.");

            if (!new PasswordHasher().VerifyPassword(loginDto.Password!, user.Password))
                throw ErrorHelper.Unauthorized("Password is incorrect.");

            if (user.UserStatus != UserStatus.Active)
                throw ErrorHelper.Forbidden("Account have not verified yet.");

            _loggerService.Success($"[LoginAsync] User {loginDto.Email} authenticated successfully.");

            // Generate JWT token and refresh token
            var accessToken = JwtUtils.GenerateJwtToken(
                user.Id,
                user.Email,
                user.Role.ToString(),
                configuration,
                TimeSpan.FromMinutes(30)
            );

            var refreshToken = Guid.NewGuid().ToString();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.Info($"[LoginAsync] Tokens generated and user cache updated for {user.Email}");

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = ToUserDto(user)
            };
        }

        /// <summary>
        ///     Logout a user by removing their refresh token from the database.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<bool> LogoutAsync(Guid userId)
        {
            _loggerService.Info($"[LogoutAsync] Logout process initiated for user ID: {userId}");

            var user = await GetUserById(userId);
            if (user == null)
                throw ErrorHelper.NotFound("Account does not exist.");

            if (user.IsDeleted || user.UserStatus == UserStatus.Banned || user.UserStatus == UserStatus.Deleted)
                throw ErrorHelper.Forbidden("Account has been disabled or banned.");

            // Đã logout rồi thì không cần xóa token nữa
            if (string.IsNullOrEmpty(user.RefreshToken))
                throw ErrorHelper.BadRequest("User previously logged out.");

            // Xóa token trong DB
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.Info($"[LogoutAsync] Logout successful for user ID: {userId}.");
            return true;
        }

        /// <summary>
        ///     Refresh the access token using the refresh token. 🐧
        /// </summary>
        /// <param name="refreshTokenDto"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public async Task<LoginResponseDto?> RefreshTokenAsync(TokenRefreshRequestDto refreshTokenDto, IConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(refreshTokenDto.RefreshToken))
                throw ErrorHelper.BadRequest("Missing tokens");

            var user = await GetUserByRefreshToken(refreshTokenDto.RefreshToken);

            if (user == null)
                throw ErrorHelper.NotFound("Account does not exist.");

            if (string.IsNullOrEmpty(user.RefreshToken))
                throw ErrorHelper.BadRequest("User previously logged out.");

            // Kiểm tra Refresh Token có còn hiệu lực hay không
            if (user.RefreshTokenExpiryTime < DateTime.UtcNow)
                throw ErrorHelper.Conflict("Refresh token has expired.");

            var roleName = user.Role.ToString();

            // Tạo mới access và refresh token
            var newAccessToken = JwtUtils.GenerateJwtToken(
                user.Id,
                user.Email,
                roleName,
                configuration,
                TimeSpan.FromHours(1)
            );

            var newRefreshToken = Guid.NewGuid().ToString();
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();


            return new LoginResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }

        /// <summary>
        ///     Verify OTP mà user truyền vào
        /// </summary>
        /// <param name="email"></param>
        /// <param name="otp"></param>
        /// <returns></returns>
        public async Task<bool> VerifyEmailOtpAsync(string email, string otp)
        {
            _loggerService.Info($"[VerifyEmailOtpAsync] Verifying OTP for {email}");

            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) throw ErrorHelper.NotFound("Account does not exist.");

            if (user.IsEmailVerified) return false;
            if (!await VerifyOtpAsync(email, otp, OtpPurpose.Register))
                return false;

            // Activate user account
            user.IsEmailVerified = true;
            user.UserStatus = UserStatus.Active;
            await _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            await _emailService.SendRegistrationSuccessEmailAsync(new EmailRequestDto
            {
                To = user.Email,
                UserName = user.FullName
            });

            _loggerService.Success($"[VerifyEmailOtpAsync] User {email} verified and activated.");
            return true;
        }


        /// <summary>
        ///     Check resend lại OTP là gì và gọi đúng hàm resend OTP
        /// </summary>
        /// <param name="email"></param>
        /// <param name="otpPurpose"></param>
        /// <returns></returns>
        public async Task<bool> ResendOtpAsync(string email, OtpPurpose otpPurpose)
        {
            return otpPurpose switch
            {
                OtpPurpose.Register => await ResendRegisterOtpAsync(email),
                _ => throw ErrorHelper.BadRequest("Loại OTP không hợp lệ.")
            };
        }

        /// <summary>
        ///     Gửi lại OTP cho user đã đăng ký nhưng chưa xác thực email.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        private async Task<bool> ResendRegisterOtpAsync(string email)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw ErrorHelper.NotFound("Email does not exist in the system.");

            if (user.IsDeleted || user.UserStatus == UserStatus.Banned)
                throw ErrorHelper.Forbidden("Account has been disabled or banned.");

            if (user.IsEmailVerified)
                throw ErrorHelper.Conflict("Verified account, no need to resend OTP.");

            await GenerateAndSendOtpAsync(user, OtpPurpose.Register);

            return true;
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
        private async Task GenerateAndSendOtpAsync(User user, OtpPurpose purpose)
        {
            var otpToken = OtpGenerator.GenerateToken(6, TimeSpan.FromMinutes(10));
            var otp = new OtpStorage
            {
                Target = user.Email,
                OtpCode = otpToken.Code,
                ExpiredAt = otpToken.ExpiresAtUtc,
                IsUsed = false,
                Purpose = purpose
            };

            await _unitOfWork.OtpStorages.AddAsync(otp);
            await _unitOfWork.SaveChangesAsync();

            // Send the correct email based on OTP purpose
            if (purpose == OtpPurpose.Register)
            {
                await _emailService.SendOtpVerificationEmailAsync(new EmailRequestDto
                {
                    To = user.Email,
                    Otp = otpToken.Code,
                    UserName = user.FullName
                });
                _loggerService.Info($"[GenerateAndSendOtpAsync] Registration OTP sent to {user.Email}");
            }
            //else if (purpose == OtpPurpose.ForgotPassword)
            //{
            //    await _emailService.SendForgotPasswordOtpEmailAsync(new EmailRequestDto
            //    {
            //        To = user.Email,
            //        Otp = otpToken.Code,
            //        UserName = user.FullName
            //    });
            //    _loggerService.Info($"[GenerateAndSendOtpAsync] Forgot password OTP sent to {user.Email}");
            //}
        }

        private async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
        private async Task<User?> GetUserById(Guid id)
        {
            return await _unitOfWork.Users.GetByIdAsync(id);
        }
        private async Task<User?> GetUserByRefreshToken(string refreshToken)
        {
            return await _unitOfWork.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
        }

        private async Task<bool> VerifyOtpAsync(string email, string otp, OtpPurpose purpose)
        {

            // Check trong db có tồn tại OTP chưa
            var otpRecord = await _unitOfWork.OtpStorages.FirstOrDefaultAsync(o =>
                o.Target == email && o.OtpCode == otp && o.Purpose == purpose && !o.IsUsed);

            // Nếu ko có OTP hoặc expired thì trả log
            if (otpRecord == null || otpRecord.ExpiredAt < DateTime.UtcNow)
            {
                _loggerService.Warn($"[VerifyOtpAsync] OTP not found or expired for {email} (purpose: {purpose})");
                return false;
            }

            otpRecord.IsUsed = true;
            await _unitOfWork.OtpStorages.Update(otpRecord);
            await _unitOfWork.SaveChangesAsync();
            _loggerService.Info($"[VerifyOtpAsync] OTP for {email} (purpose: {purpose}) verified and marked as used in DB.");
            return true;
        }


        //========================= MAPPER ============================
        private UserDto ToUserDto(User user)
        {
            return new UserDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
            };
        }


    }
}
