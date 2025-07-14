using Microsoft.VisualBasic;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Infrastructure.Interfaces;
using System.Text.RegularExpressions;

namespace MovieTheater.Application.Services
{
    public class UserService : IUserService
    {

        private readonly ILoggerService _loggerService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRedisService _redisService;
        public UserService(IUnitOfWork unitOfWork, ILoggerService loggerService, IRedisService redisService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
            _redisService = redisService;
        }

        public async Task<CurrentUserDto> GetUserDetails(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                _loggerService.Warn("Attempted to fetch user with an empty GUID.");
                throw new ArgumentException("Invalid user ID.");
            }

            try
            {
                string cacheKey = $"user:detail:{userId}";
                var cached = await _redisService.GetAsync<CurrentUserDto>(cacheKey);
                if (cached != null) return cached;

                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    _loggerService.Warn($"No user found with ID: {userId}");
                    throw new KeyNotFoundException($"User with ID {userId} not found.");
                }

                var result = new CurrentUserDto
                {
                    FullName = user.FullName,
                    DateOfBirth = user.DateOfBirth,
                    Sex = user.Sex,
                    Email = user.Email,
                    CCCD = user.CCCD,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    Role = user.Role,
                    ScoreBalance = user.ScoreBalance,
                    AvatarUrl = user.AvatarUrl,
                };

                await _redisService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
                return result;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"An unexpected error occurred while fetching user details for ID {userId}: {ex.Message}");
                throw;
            }
        }


        public async Task<UserUpdateDto> UpdateUserInfo(Guid userId, UserUpdateDto userUpdateDto)
        {
            try
            {
                _loggerService.Info($"Starting user info update for UserID: {userId}");

                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    _loggerService.Warn($"User with ID {userId} not found.");
                    throw new KeyNotFoundException("User not found.");
                }

                var isUpdated = false;

                if (!string.IsNullOrEmpty(userUpdateDto.FullName) && user.FullName != userUpdateDto.FullName)
                {
                    user.FullName = userUpdateDto.FullName;
                    isUpdated = true;
                }

                if (userUpdateDto.Sex.HasValue && user.Sex != userUpdateDto.Sex)
                {
                    user.Sex = userUpdateDto.Sex.Value;
                    isUpdated = true;
                }

                if (!string.IsNullOrEmpty(userUpdateDto.CCCD) && user.CCCD != userUpdateDto.CCCD)
                {
                    if (!Regex.IsMatch(userUpdateDto.CCCD, @"^\d{12}$"))
                        throw new ArgumentException("Citizen ID must consist of exactly 12 digits.");

                    user.CCCD = userUpdateDto.CCCD;
                    isUpdated = true;
                }


                if (userUpdateDto.DateOfBirth.HasValue && user.DateOfBirth != userUpdateDto.DateOfBirth)
                {
                    if (userUpdateDto.DateOfBirth.Value > DateTime.UtcNow)
                        throw new ArgumentException("Date of birth cannot be in the future.");

                    user.DateOfBirth = userUpdateDto.DateOfBirth;
                    isUpdated = true;
                }

                if (!string.IsNullOrEmpty(userUpdateDto.Address) && user.Address != userUpdateDto.Address)
                {
                    user.Address = userUpdateDto.Address;
                    isUpdated = true;
                }

                if (!string.IsNullOrEmpty(userUpdateDto.PhoneNumber) && user.PhoneNumber != userUpdateDto.PhoneNumber)
                {
                    if (!Regex.IsMatch(userUpdateDto.PhoneNumber, @"^\d{10,15}$"))
                        throw new ArgumentException("Invalid phone number format.");

                    List<Ticket>? tickets = _unitOfWork.Tickets.GetQueryable().Where(t => t.GuestPhoneNumber == user.PhoneNumber).ToList();
                    if (tickets.Any())
                    {
                        foreach (var ticket in tickets)
                        {
                            ticket.GuestPhoneNumber = userUpdateDto.PhoneNumber;
                            await _unitOfWork.Tickets.Update(ticket);
                        }
                    }

                    user.PhoneNumber = userUpdateDto.PhoneNumber;
                    isUpdated = true;
                }

                if (!isUpdated)
                {
                    _loggerService.Warn($"No changes detected for UserId: {userId}");
                    return new UserUpdateDto
                    {
                        FullName = user.FullName,
                        Sex = user.Sex,
                        CCCD = user.CCCD,
                        DateOfBirth = user.DateOfBirth,
                        Address = user.Address,
                        PhoneNumber = user.PhoneNumber
                    };
                }

                await _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();
                await _redisService.RemoveAsync($"user:detail:{userId}");
                await _redisService.RemoveByPatternAsync("admin:user:list:"); // xóa cache cho cả list admin

                _loggerService.Success($"User info updated successfully for UserId: {userId}");

                return new UserUpdateDto
                {
                    FullName = user.FullName,
                    Sex = user.Sex,
                    CCCD = user.CCCD,
                    DateOfBirth = user.DateOfBirth,
                    Address = user.Address,
                    PhoneNumber = user.PhoneNumber
                };
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error updating user info for UserId: {userId}. Exception: {ex.Message}");
                throw;
            }
        }

    }
}
