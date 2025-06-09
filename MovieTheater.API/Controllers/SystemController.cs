using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain;
using MovieTheater.Domain.DTOs.AuditLogDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieTheater.API.Controllers;
[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
    private readonly MovieTheaterDbContext _context;
    private readonly ILoggerService _logger;
    private readonly IAuditLogService _auditLogService;

    public SystemController(MovieTheaterDbContext context, ILoggerService logger, IAuditLogService auditLogService)
    {
        _context = context;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    [HttpPost("seed-all-data")]
    public async Task<IActionResult> SeedData()
    {
        try
        {
            await ClearDatabase(_context);

            //Seed data
            await SeedUserAsync();

            return Ok(ApiResult<object>.Success(new
            {
                Message = "Data seeded successfully."
            }));
        }
        catch (DbUpdateException dbEx)
        {
            _logger.Error($"Database update error: {dbEx.Message}");
            return StatusCode(500, "Error seeding data: Database issue.");
        }
        catch (Exception ex)
        {
            _logger.Error($"General error: {ex.Message}");
            return StatusCode(500, "Error seeding data: General failure.");
        }
    }
    private async Task SeedUserAsync()
    {
        var passwordHasher = new PasswordHasher();

        //Seed User
        var users = new List<User>
        {
            new()
        {
            FullName = "Admin User",
            Email = "admin@gmail.com",
            Sex = Gender.Female,
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            PhoneNumber = "0944000000",
            Password = passwordHasher.HashPassword("1@"),
            Role = RoleType.Admin,
            ScoreBalance = 0,
            IsEmailVerified = true,
            UserStatus = UserStatus.Active,
            CCCD = "11000000000",
            AvatarUrl = "https://avatar.iran.liara.run/public"
        },
        // Employees
        new()
        {
            FullName = "Test Employee 1",
            Email = "employee1@gmail.com",
            Sex = Gender.Male,
            DateOfBirth = DateTime.UtcNow.AddYears(-28),
            PhoneNumber = "0944000001",
            Password = passwordHasher.HashPassword("1@"),
            Role = RoleType.Employee,
            ScoreBalance = 0,
            IsEmailVerified = true,
            UserStatus = UserStatus.Active,
            CCCD = "11000000001",
            AvatarUrl = "https://avatar.iran.liara.run/public"
        },
        new()
        {
            FullName = "Test Employee 2",
            Email = "employee2@gmail.com",
            Sex = Gender.Female,
            DateOfBirth = DateTime.UtcNow.AddYears(-27),
            PhoneNumber = "0944000002",
            Password = passwordHasher.HashPassword("1@"),
            Role = RoleType.Employee,
            ScoreBalance = 0,
            IsEmailVerified = true,
            UserStatus = UserStatus.Active,
            CCCD = "11000000002",
            AvatarUrl = "https://avatar.iran.liara.run/public"
        },
        new()
        {
            FullName = "Test Employee 3",
            Email = "employee3@gmail.com",
            Sex = Gender.Male,
            DateOfBirth = DateTime.UtcNow.AddYears(-26),
            PhoneNumber = "0944000003",
            Password = passwordHasher.HashPassword("1@"),
            Role = RoleType.Employee,
            ScoreBalance = 0,
            IsEmailVerified = true,
            UserStatus = UserStatus.Active,
            CCCD = "11000000003",
            AvatarUrl = "https://avatar.iran.liara.run/public"
        },
        // Members
        new()
        {
            FullName = "Test Member 1",
            Email = "member1@gmail.com",
            Sex = Gender.Female,
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            PhoneNumber = "0944000004",
            Password = passwordHasher.HashPassword("1@"),
            Role = RoleType.Member,
            ScoreBalance = 0,
            IsEmailVerified = true,
            UserStatus = UserStatus.Active,
            CCCD = "11000000004",
            AvatarUrl = "https://avatar.iran.liara.run/public"
        },
        new()
        {
            FullName = "Test Member 2",
            Email = "member2@gmail.com",
            Sex = Gender.Male,
            DateOfBirth = DateTime.UtcNow.AddYears(-24),
            PhoneNumber = "0944000005",
            Password = passwordHasher.HashPassword("1@"),
            Role = RoleType.Member,
            ScoreBalance = 0,
            IsEmailVerified = true,
            UserStatus = UserStatus.Active,
            CCCD = "11000000005",
            AvatarUrl = "https://avatar.iran.liara.run/public"
        },
        new()
        {
            FullName = "Test Member 3",
            Email = "member3@gmail.com",
            Sex = Gender.Female,
            DateOfBirth = DateTime.UtcNow.AddYears(-23),
            PhoneNumber = "0944000006",
            Password = passwordHasher.HashPassword("1@"),
            Role = RoleType.Member,
            ScoreBalance = 0,
            IsEmailVerified = true,
            UserStatus = UserStatus.Active,
            CCCD = "11000000006",
            AvatarUrl = "https://avatar.iran.liara.run/public"
        }
        };
        _logger.Info("Seeding users with roles...");
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();
        _logger.Success("Users seeded successfully.");
    }
    private async Task ClearDatabase(MovieTheaterDbContext context)
    {
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            _logger.Info("Start deleting data in database...");

            var tablesToDelete = new List<Func<Task>>
            {
                () => context.Users.ExecuteDeleteAsync()
            };

            foreach (var deleteFunc in tablesToDelete) await deleteFunc();

            await transaction.CommitAsync();
            _logger.Success("Deleted data in database successfully.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.Error($"Deleted data fail: {ex.Message}");
            throw;
        }
    }

    [HttpGet]
    [Authorize(Policy = "AdminPolicy")]
    [SwaggerOperation(Summary = "View all audit logs", Description = "Get all logs from the database.")]
    [ProducesResponseType(typeof(ApiResult<List<AuditLogDto>>), 200)]
    public async Task<IActionResult> ViewLogAsync()
    {
        var logs = await _auditLogService.ViewLogAsync();
        return Ok(ApiResult<List<AuditLogDto>>.Success(logs, "200", "Audit logs retrieved successfully."));
    }
}