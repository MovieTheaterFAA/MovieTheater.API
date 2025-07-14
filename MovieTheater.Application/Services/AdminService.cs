using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.AdminDTOs;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;
using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MovieTheater.Application.Services;

public class AdminService : IAdminService
{
    private readonly ILoggerService _loggerService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IClaimsService _claimsService;
    private readonly IAuditLogService _auditLogService;
    private readonly IRedisService _redisService;

    public AdminService(IUnitOfWork unitOfWork, ILoggerService loggerService, IEmailService emailService, IClaimsService claimsService, IAuditLogService auditLogService, IRedisService redisService)
    {
        _unitOfWork = unitOfWork;
        _loggerService = loggerService;
        _emailService = emailService;
        _claimsService = claimsService;
        _auditLogService = auditLogService;
        _redisService = redisService;
    }

    public async Task<UserDto?> AddEmployeeAsync(AddEmployeeRequestDto dto)
    {
        _loggerService.Info($"[AddEmployeeAsync] Start registration employee for {dto.Email}");

        // Kiểm tra email đã tồn tại chưa
        if (await UserExistsAsync(dto.Email))
        {
            _loggerService.Warn($"[AddEmployeeAsync] Email {dto.Email} already registered.");
            throw ErrorHelper.Conflict("Email has been used.");
        }

        // Tạo mật khẩu ngẫu nhiên
        string plainPassword = GenerateRandomPassword();

        // Mã hóa mật khẩu
        var hashedPassword = new PasswordHasher().HashPassword(plainPassword);

        // Tạo đối tượng User mới với role Employee
        var user = ToAddEmployeeDto(dto);

        // Chắc chắn pass được hash trước khi gán
        user.Password = hashedPassword ?? throw new InvalidOperationException("Password hashing failed.");

        user.UserStatus = UserStatus.Active;
        user.Role = RoleType.Employee;
        user.IsEmailVerified = true;

        // Thêm các trường audit bắt buộc nếu có
        user.CreatedAt = DateTime.UtcNow;
        user.CreatedBy = _claimsService.GetCurrentUserId; // hoặc Guid.Empty nếu không có

        // Thêm User vào database
        await _unitOfWork.Users.AddAsync(user);
        try
        {
            await _unitOfWork.SaveChangesAsync();
            await _redisService.RemoveByPatternAsync($"admin:user:list:");
        }
        catch (DbUpdateException dbEx)
        {
            _loggerService.Error($"DbUpdateException: {dbEx.InnerException?.Message ?? dbEx.Message}");
            throw;
        }
        var adminId = _claimsService.GetCurrentUserId;

        var newData = new
        {
            user.FullName,
            user.DateOfBirth,
            user.Sex,
            user.CCCD,
            user.Email,
            user.PhoneNumber,
            user.Address,
            user.Role,
            user.UserStatus,
            user.IsEmailVerified
        };

        var changedFields = JsonSerializer.Serialize(new
        {
            user.FullName,
            user.DateOfBirth,
            user.Sex,
            user.CCCD,
            user.Email,
            user.PhoneNumber,
            user.Address,
            user.Role,
            user.UserStatus,
            user.IsEmailVerified
        });

        await _auditLogService.LogAsync
                (
                adminId,
                AuditActionType.Create,
                "Employee",
                user.Id,
                null,
                newData,
                changedFields,
                "Admin created new employee"
                );

        _loggerService.Success($"[AddEmployeeAsync] Employee {user.Email} created successfully.");

        // Gửi email thông tin đăng nhập cho nhân viên
        await _emailService.SendEmployeeCredentialsEmailAsync(new Domain.DTOs.EmailDTOs.EmployeeCredentialsEmailDto
        {
            To = user.Email,
            UserName = user.Email,
            Password = plainPassword // gửi mật khẩu gốc đã tạo
        });

        _loggerService.Info($"[AddEmployeeAsync] Login information sent to {user.Email} for verification.");

        // Trả về UserDto ( có phương thức chuyển đổi)
        return ToUserDto(user);
    }

    public async Task<Pagination<GetUserDto>> GetListUserAsync(string? search, RoleType? role, string? sortBy, bool isDescending, int page, int pageSize)
    {
        try
        {
            var cacheKey = $"admin:user:list:{search}:{role}:{sortBy}:{isDescending}:{page}:{pageSize}";
            var cached = await _redisService.GetAsync<Pagination<GetUserDto>>(cacheKey);
            if (cached != null)
            {
                _loggerService.Info($"[CACHE HIT] {cacheKey}");
                return cached;
            }

            _loggerService.Info($"[CACHE MISS] {cacheKey} — Fetching from DB");

            var query = _unitOfWork.Users.GetQueryable().Where(u => !u.IsDeleted);

            if (role.HasValue)
                query = query.Where(u => u.Role == role.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(searchLower) ||
                    u.Email.ToLower().Contains(searchLower));
            }

            var totalUsers = await query.CountAsync();

            if (!string.IsNullOrEmpty(sortBy))
            {
                query = sortBy switch
                {
                    "ScoreBalance" => isDescending ? query.OrderByDescending(u => u.ScoreBalance) : query.OrderBy(u => u.ScoreBalance),
                    "CreatedAt" => isDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
                    _ => query.OrderBy(u => u.Id)
                };
            }
            else query = query.OrderBy(u => u.Id);

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userDtos = users.Select(u => new GetUserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Sex = u.Sex,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                CCCD = u.CCCD,
                Address = u.Address,
                Role = u.Role,
                ScoreBalance = u.ScoreBalance,
                CreatedAt = u.CreatedAt,
                AvatarUrl = u.AvatarUrl ?? string.Empty,
                IsDeleted = u.IsDeleted,
                Status = u.UserStatus
            }).ToList();

            var result = new Pagination<GetUserDto>(userDtos, totalUsers, page, pageSize);
            await _redisService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            return result;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error while fetching users: {ex.Message}");
            throw new Exception("An error occurred while fetching users. Please try again later");
        }
    }


    public async Task<Pagination<UserDto>> GetListEmployeeAsync(string? search, string? sortBy, bool isDescending, int page, int pageSize)
    {
        try
        {
            _loggerService.Info($"Fetching employees - Page {page}, PageSize {pageSize}, Search: {search}");

            var listUsers = await _unitOfWork.Users.GetAllAsync();

            var employeeUsers = listUsers.Where(u => u.Role == RoleType.Employee && !u.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                employeeUsers = employeeUsers.Where(u =>
                    (!string.IsNullOrEmpty(u.FullName) && u.FullName.ToLower().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(u.Email) && u.Email.ToLower().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(u.PhoneNumber) && u.PhoneNumber.ToLower().Contains(searchLower))
                );
            }

            var totalEmployees = await employeeUsers.CountAsync();

            employeeUsers = sortBy?.ToLower() switch
            {
                "fullname" => isDescending ? employeeUsers.OrderByDescending(u => u.FullName) : employeeUsers.OrderBy(u => u.FullName),
                "dateofbirth" => isDescending ? employeeUsers.OrderByDescending(u => u.DateOfBirth) : employeeUsers.OrderBy(u => u.DateOfBirth),
                _ => employeeUsers.OrderBy(u => u.Id)
            };

            var pagedEmployees = await employeeUsers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = pagedEmployees.Select(user => new UserDto
            {
                UserId = user.Id,
                AvatarUrl = user.AvatarUrl,
                FullName = user.FullName,
                CCCD = user.CCCD,
                DateOfBirth = user.DateOfBirth,
                Sex = user.Sex,
                Email = user.Email,
                Role = user.Role,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address
            }).ToList();

            _loggerService.Success($"Retrieved {result.Count} employees on page {page} successfully.");

            return new Pagination<UserDto>(result, totalEmployees, page, pageSize);
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Failed to retrieve employees. Exception: {ex.Message}");
            throw new Exception("An error occurred while retrieving employees. Please try again later.");
        }
    }

    public async Task<EditEmployeeDto> EditEmployeeAsync(Guid userId, EditEmployeeDto editEmployeeDto)
    {
        try
        {
            _loggerService.Info($"[Admin] Starting employee info update for UserID: {userId}");

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                _loggerService.Warn($"User with ID {userId} not found.");
                throw new KeyNotFoundException("User not found.");
            }

            var oldData = new
            {
                user.FullName,
                user.DateOfBirth,
                user.Sex,
                user.CCCD,
                user.PhoneNumber,
                user.Address
            };

            bool isUpdated = false;

            // 1. Full Name
            if (!string.IsNullOrEmpty(editEmployeeDto.FullName) && user.FullName != editEmployeeDto.FullName)
            {
                user.FullName = editEmployeeDto.FullName;
                isUpdated = true;
            }

            // 2. Date of Birth
            if (editEmployeeDto.DateOfBirth.HasValue && user.DateOfBirth != editEmployeeDto.DateOfBirth)
            {
                if (editEmployeeDto.DateOfBirth.Value > DateTime.UtcNow)
                    throw new ArgumentException("Date of birth cannot be in the future.");

                user.DateOfBirth = editEmployeeDto.DateOfBirth.Value;
                isUpdated = true;
            }

            // 3. Gender
            if (editEmployeeDto.Sex.HasValue && user.Sex != editEmployeeDto.Sex)
            {
                user.Sex = editEmployeeDto.Sex.Value;
                isUpdated = true;
            }

            // 4. CCCD
            if (!string.IsNullOrEmpty(editEmployeeDto.CCCD) && user.CCCD != editEmployeeDto.CCCD)
            {
                if (!Regex.IsMatch(editEmployeeDto.CCCD, @"^\d{12}$"))
                    throw new ArgumentException("Citizen ID must consist of exactly 12 digits.");

                user.CCCD = editEmployeeDto.CCCD;
                isUpdated = true;
            }

            // 5. Phone Number
            if (!string.IsNullOrEmpty(editEmployeeDto.PhoneNumber) && user.PhoneNumber != editEmployeeDto.PhoneNumber)
            {
                if (!Regex.IsMatch(editEmployeeDto.PhoneNumber, @"^\d{10,15}$"))
                    throw new ArgumentException("Invalid phone number format.");

                user.PhoneNumber = editEmployeeDto.PhoneNumber;
                isUpdated = true;
            }

            // 6. Address
            if (!string.IsNullOrEmpty(editEmployeeDto.Address) && user.Address != editEmployeeDto.Address)
            {
                user.Address = editEmployeeDto.Address;
                isUpdated = true;
            }

            // 7. Password (admin sets a new one)
            if (!string.IsNullOrWhiteSpace(editEmployeeDto.Password))
            {
                if (editEmployeeDto.Password.Length <= 6)
                    throw new ArgumentException("Password must be longer than 6 characters.");

                user.Password = new PasswordHasher().HashPassword(editEmployeeDto.Password);
                isUpdated = true;
            }

            if (!isUpdated)
            {
                _loggerService.Warn($"No changes detected for EmployeeId: {userId}");
                return editEmployeeDto;
            }

            await _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
            await _redisService.RemoveByPatternAsync($"admin:user:list:");
            await _redisService.RemoveAsync($"admin:user:detail:{userId}");

            // Notify employee about the update
            await _emailService.SendUpdateEmployeeCredentialsEmailAsync(new Domain.DTOs.EmailDTOs.UpdateEmployeeCredentialsEmailDto
            {
                To = user.Email,
                UserName = user.Email,
                Password = !string.IsNullOrWhiteSpace(editEmployeeDto.Password) ? editEmployeeDto.Password : "Your password was not changed",
                FullName = user.FullName,
                DateOfBirth = user.DateOfBirth,
                Sex = user.Sex,
                CCCD = user.CCCD,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address
            });

            var newData = new
            {
                user.FullName,
                user.DateOfBirth,
                user.Sex,
                user.CCCD,
                user.PhoneNumber,
                user.Address
            };

            var changedFields = JsonSerializer.Serialize(new
            {
                editEmployeeDto.FullName,
                editEmployeeDto.DateOfBirth,
                editEmployeeDto.Sex,
                editEmployeeDto.CCCD,
                editEmployeeDto.PhoneNumber,
                editEmployeeDto.Address
            });

            var adminId = _claimsService.GetCurrentUserId;

            await _auditLogService.LogAsync
                (
                adminId,
                AuditActionType.Update,
                "Employee",
                userId,
                oldData,
                newData,
                changedFields,
                "Admin updated employee information"
                );

            _loggerService.Success($"[Admin] Employee info updated successfully for UserId: {userId}");

            return new EditEmployeeDto
            {
                FullName = user.FullName,
                Password = "",
                DateOfBirth = user.DateOfBirth,
                Sex = user.Sex,
                CCCD = user.CCCD,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address
            };
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error updating employee info for UserId: {userId}. Exception: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteEmployeeAsync(Guid employeeId, Guid adminId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(employeeId);
        if (user == null || user.IsDeleted ||
            !(user.Role == RoleType.Employee || user.Role == RoleType.Admin))
        {
            return false;
        }

        var oldValue = new
        {
            user.UserStatus,
        };

        user.UserStatus = UserStatus.Deleted;
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = adminId;

        var newValue = new
        {
            user.UserStatus,
        };

        var changedFields = JsonSerializer.Serialize(new
        {
            UserStatus = UserStatus.Deleted,
        });

        await _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        await _redisService.RemoveByPatternAsync($"admin:user:list:");
        await _redisService.RemoveAsync($"admin:user:detail:{employeeId}");

        await _auditLogService.LogAsync
               (
               adminId,
               AuditActionType.Delete,
               "Employee",
               employeeId,
               oldValue,
               newValue,
               changedFields,
               "Deleted employee account"
               );

        return true;
    }

    public async Task<bool> BanUserAsync(Guid userId, Guid adminId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
        {
            _loggerService.Warn($"[BanUserAsync] User with ID {userId} not found or already deleted.");
            return false;
        }

        if (user.UserStatus == UserStatus.Banned)
        {
            _loggerService.Warn($"[BanUserAsync] User with ID {userId} is already banned.");
            return false;
        }

        var oldValue = new
        {
            user.UserStatus,
        };

        user.UserStatus = UserStatus.Banned;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = adminId;

        var newValue = new
        {
            user.UserStatus,
        };

        var changedFields = System.Text.Json.JsonSerializer.Serialize(new
        {
            UserStatus = UserStatus.Banned,
        });

        await _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        await _redisService.RemoveByPatternAsync($"admin:user:list:");
        await _redisService.RemoveAsync($"admin:user:detail:{userId}");

        await _auditLogService.LogAsync(
            adminId,
            AuditActionType.Update,
            "User",
            userId,
            oldValue,
            newValue,
            changedFields,
            "User was banned by admin"
        );

        _loggerService.Success($"[BanUserAsync] User with ID {userId} has been banned.");
        return true;
    }

    public async Task<bool> UnbanUserAsync(Guid userId, Guid adminId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
        {
            _loggerService.Warn($"[UnbanUserAsync] User with ID {userId} not found or already deleted.");
            return false;
        }

        if (user.UserStatus != UserStatus.Banned)
        {
            _loggerService.Warn($"[UnbanUserAsync] User with ID {userId} is not banned.");
            return false;
        }

        var oldValue = new
        {
            user.UserStatus,
        };

        user.UserStatus = UserStatus.Active;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = adminId;

        var newValue = new
        {
            user.UserStatus,
        };

        var changedFields = System.Text.Json.JsonSerializer.Serialize(new
        {
            UserStatus = UserStatus.Active,
        });

        await _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        await _redisService.RemoveByPatternAsync($"admin:user:list:");
        await _redisService.RemoveAsync($"admin:user:detail:{userId}");

        await _auditLogService.LogAsync(
            adminId,
            AuditActionType.Update,
            "User",
            userId,
            oldValue,
            newValue,
            changedFields,
            "User was unbanned by admin"
        );

        _loggerService.Success($"[UnbanUserAsync] User with ID {userId} has been unbanned.");
        return true;
    }

    public async Task<GetUserDto?> GetUserDetailAsync(Guid userId)
    {
        var cacheKey = $"admin:user:detail:{userId}";
        var cached = await _redisService.GetAsync<GetUserDto>(cacheKey);
        if (cached != null)
        {
            _loggerService.Info($"[CACHE HIT] {cacheKey}");
            return cached;
        }

        _loggerService.Info($"[CACHE MISS] {cacheKey} — Fetching from DB");

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return null;

        var dto = new GetUserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Sex = user.Sex,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            CCCD = user.CCCD,
            Address = user.Address,
            Role = user.Role,
            ScoreBalance = user.ScoreBalance,
            CreatedAt = user.CreatedAt,
            AvatarUrl = user.AvatarUrl ?? string.Empty,
            IsDeleted = user.IsDeleted,
            Status = user.UserStatus
        };

        await _redisService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10));
        return dto;
    }

    public async Task<GetUserDto?> GetUserByPhoneNumberAsync(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));

        // Validate phone number format (10-15 digits, adjust regex as needed)
        if (!Regex.IsMatch(phoneNumber, @"^\d{10,15}$"))
            throw new ArgumentException("Invalid phone number format.", nameof(phoneNumber));

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && !u.IsDeleted);
        if (user == null)
            return null;

        return new GetUserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Sex = user.Sex,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            CCCD = user.CCCD,
            Address = user.Address,
            Role = user.Role,
            ScoreBalance = user.ScoreBalance,
            CreatedAt = user.CreatedAt,
            AvatarUrl = user.AvatarUrl ?? string.Empty,
            IsDeleted = user.IsDeleted,
            Status = user.UserStatus
        };
    }


    //========================= PRIVATE HELPER METHODS ============================

    /// <summary>
    ///     Check những employee đã tồn tại trong hệ thống hay chưa.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    private async Task<bool> UserExistsAsync(string email)
    {
        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
        return existingUser != null;
    }

    /// <summary>
    ///     Hàm tự động tạo password cho employee
    /// </summary>
    /// <param name="length"></param>
    /// <returns></returns>
    private string GenerateRandomPassword(int length = 12)
    {
        const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-";
        var random = new Random();
        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = validChars[random.Next(validChars.Length)];
        }
        return new string(chars);
    }

    //========================= MAPPER ============================
    private UserDto ToUserDto(User user)
    {
        return new UserDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            DateOfBirth = user.DateOfBirth,
            Sex = user.Sex,
            Email = user.Email,
            CCCD = user.CCCD,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Role = user.Role,
            ScoreBalance = user.ScoreBalance,
            CreatedAt = user.CreatedAt
        };
    }

    private User ToAddEmployeeDto(AddEmployeeRequestDto dto, User? user = null)
    {
        user ??= new User();

        user.FullName = dto.FullName;
        user.DateOfBirth = DateTime.SpecifyKind(dto.DateOfBirth, DateTimeKind.Utc);
        user.Sex = dto.Sex;
        user.CCCD = dto.CCCD;
        user.Email = dto.Email;
        user.PhoneNumber = dto.PhoneNumber;
        user.Address = dto.Address;
        user.CreatedAt = dto.CreateAt != default ? DateTime.SpecifyKind(dto.CreateAt, DateTimeKind.Utc) : DateTime.UtcNow;

        // Note: Password, UserStatus, Role, IsEmailVerified, CreatedBy are set above
        return user;
    }
}