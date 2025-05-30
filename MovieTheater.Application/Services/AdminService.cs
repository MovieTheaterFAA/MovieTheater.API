using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.UserDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;
using System.Reactive;

namespace MovieTheater.Application.Services;

public class AdminService : IAdminService
{
    private readonly ILoggerService _loggerService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IClaimsService _claimsService;

    public AdminService(IUnitOfWork unitOfWork, ILoggerService loggerService, IEmailService emailService, IClaimsService claimsService)
    {
        _unitOfWork = unitOfWork;
        _loggerService = loggerService;
        _emailService = emailService;
        _claimsService = claimsService;
    }

    public async Task<UserDto?> AddEmployeeAsync(UserRequestDTO userRequestDTO)
    {
        _loggerService.Info($"[AddEmployeeAsync] Start registration employee for {userRequestDTO.Email}");

        // Kiểm tra email đã tồn tại chưa
        if (await UserExistsAsync(userRequestDTO.Email))
        {
            _loggerService.Warn($"[AddEmployeeAsync] Email {userRequestDTO.Email} already registered.");
            throw ErrorHelper.Conflict("Email has been used.");
        }

        // Tạo mật khẩu ngẫu nhiên
        string plainPassword = GenerateRandomPassword();

        // Mã hóa mật khẩu
        var hashedPassword = new PasswordHasher().HashPassword(plainPassword);

        // Tạo đối tượng User mới với role Employee
        var user = MapToUser(userRequestDTO);
        user.Password = hashedPassword;
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
        }
        catch (DbUpdateException dbEx)
        {
            _loggerService.Error($"DbUpdateException: {dbEx.InnerException?.Message ?? dbEx.Message}");
            throw;
        }

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



    public async Task<Pagination<GetUserDto>> GetListUsersAsync(string? search, RoleType? role, string? sortBy, bool isDescending, int page, int pageSize)
    {
        try
        {
            _loggerService.Info($"Fetching users - Page {page}, PageSize {pageSize}, Role: {role}, Search: {search}");

            var query = _unitOfWork.Users.GetQueryable();

            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(searchLower) || u.Email.ToLower().Contains(searchLower));
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
            else
            {
                query = query.OrderBy(u => u.Id);
            }

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!users.Any())
            {
                _loggerService.Warn($"No user found on page {page}");
                return new Pagination<GetUserDto>(new List<GetUserDto>(), 0, page, pageSize);
            }

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
                AvatarUrl = u.AvatarUrl,
            }).ToList();

            _loggerService.Success($"Retrieved {userDtos.Count} users on page {page}");

            return new Pagination<GetUserDto>(userDtos, totalUsers, page, pageSize);
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error while fetching users: {ex.Message}");
            throw new Exception("An error occurred while fetching users. Please try again later");
        }
    }


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
    // Map Entity To DTO
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

    // Map DTO to Enitty
    private User MapToUser(UserRequestDTO dto, User? user = null)
    {
        user ??= new User();

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        user.PhoneNumber = dto.PhoneNumber;

        // Chuyển DateOfBirth về UTC
        if (dto.DateOfBirth != default)
            user.DateOfBirth = DateTime.SpecifyKind(dto.DateOfBirth, DateTimeKind.Utc);
        user.CCCD = dto.IdentityCard;          // Nếu DTO có trường IdentityCard, map vào CCCD
        user.Address = dto.Address;
        user.Sex = dto.Sex;

        // Nếu bạn có trường AvatarUrl trong DTO (ví dụ Image), map thêm
        user.AvatarUrl = dto.Image;

        // Chú ý: Password được set riêng biệt ở AddEmployeeAsync (vì cần hash)

        return user;
    }


}