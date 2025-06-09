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

            //Seed user data
            await SeedUserAsync();
            //Seed movie data
            await SeedDataMovie();
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
    private async  Task SeedDataMovie()
    {
        var movies = new List<Movie>
        {
    new Movie
    {
        Name = "Ne Zha 2",
        FromDate = new DateTime(2025, 1, 29, 0, 0, 0, DateTimeKind.Utc),
        ToDate = new DateTime(2025, 7, 31, 0, 0, 0, DateTimeKind.Utc),
        Actors = new List<string>{ "Lü Yanting", "Han Mo" },
        ActorsUrl = new List<string>{ "", "" },
        Director = "Jiaozi",
        RunningTime = 144,
        Version = MovieVersion.FourD,
        TrailerUrl = "",
        Genres = new List<string>{ "Animation", "Fantasy", "Action" },
        Description = "Sequel to Ne Zha, huge Chinese mythological animated hit.",
        PosterImage = "", BackgroundImage = "", Status = MovieStatus.ComingSoon
    },

    new Movie
    {
        Name = "A Minecraft Movie",
        FromDate = new DateTime(2025, 4, 4, 0, 0, 0, DateTimeKind.Utc),
        ToDate = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc),
        Actors = new List<string>{ "Jason Momoa", "Emma Myers" },
        ActorsUrl = new List<string>{ "", "" },
        Director = "Jared Hess",
        RunningTime = 100,
        Version = MovieVersion.FourD,
        TrailerUrl = "",
        Genres = new List<string>{ "Adventure", "Fantasy" },
        Description = "Live‑action/CGI adaptation of Minecraft game world.",
        PosterImage = "", BackgroundImage = "", Status = MovieStatus.ComingSoon
    },

    new Movie
    {
        Name = "Lilo & Stitch",
        FromDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        ToDate = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc),
        Actors = new List<string>{ "Maia Kealoha", "Zach Galifianakis" },
        ActorsUrl = new List<string>{ "", "" },
        Director = "Dean Fleischer Camp",
        RunningTime = 95,
        Version = MovieVersion.TwoD,
        TrailerUrl = "",
        Genres = new List<string>{ "Family", "Adventure", "Comedy" },
        Description = "Live‑action remake of Disney classic.",
        PosterImage = "", BackgroundImage = "", Status = MovieStatus.ComingSoon
    },

    new Movie
    {
        Name = "Detective Chinatown 1900",
        FromDate = new DateTime(2025, 2, 14, 0, 0, 0, DateTimeKind.Utc),
        ToDate = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc),
        Actors = new List<string>{ "Wang Baoqiang", "Liu Haoran" },
        ActorsUrl = new List<string>{ "", "" },
        Director = "Chen Sicheng",
        RunningTime = 120,
        Version = MovieVersion.TwoD,
        TrailerUrl = "",
        Genres = new List<string>{ "Comedy", "Mystery" },
        Description = "Chinese detective comedy set in early 1900s.",
        PosterImage = "", BackgroundImage = "", Status = MovieStatus.ComingSoon
    },

    new Movie
    {
        Name = "Mission: Impossible – The Final Reckoning",
        FromDate = new DateTime(2025, 5, 23, 0, 0, 0, DateTimeKind.Utc),
        ToDate = new DateTime(2025, 7, 15, 0, 0, 0, DateTimeKind.Utc),
        Actors = new List<string>{ "Tom Cruise", "Hayley Atwell" },
        ActorsUrl = new List<string>{ "", "" },
        Director = "Christopher McQuarrie",
        RunningTime = 150,
        Version = MovieVersion.IMAX,
        TrailerUrl = "",
        Genres = new List<string>{ "Action", "Thriller" },
        Description = "The eighth installment of M:I franchise.",
        PosterImage = "", BackgroundImage = "", Status = MovieStatus.ComingSoon
    },

    new Movie
    {
        Name = "Captain America: Brave New World",
        FromDate = new DateTime(2025, 5, 2, 0, 0, 0, DateTimeKind.Utc),
        ToDate = new DateTime(2025, 7, 30, 0, 0, 0, DateTimeKind.Utc),
        Actors = new List<string>{ "Anthony Mackie", "Liv Tyler" },
        ActorsUrl = new List<string>{ "", "" },
        Director = "Julius Onah",
        RunningTime = 130,
        Version = MovieVersion.FourD,
        TrailerUrl = "",
        Genres = new List<string>{ "Superhero", "Action" },
        Description = "Marvel's Captain America continues with Sam Wilson.",
        PosterImage = "", BackgroundImage = "", Status = MovieStatus.ComingSoon
    },

    new Movie
    {
        Name = "Thunderbolts*",
        FromDate = new DateTime(2025, 4, 25, 0, 0, 0, DateTimeKind.Utc),
        ToDate = new DateTime(2025, 7, 20, 0, 0, 0, DateTimeKind.Utc),
        Actors = new List<string>{ "Sebastian Stan", "Florence Pugh" },
        ActorsUrl = new List<string>{ "", "" },
        Director = "Jake Schreier",
        RunningTime = 120,
        Version = MovieVersion.TwoD,
        TrailerUrl = "",
        Genres = new List<string>{ "Superhero", "Action" },
        Description = "Marvel anti‑hero team-up film.",
        PosterImage = "", BackgroundImage = "", Status = MovieStatus.ComingSoon
    },

    new Movie
    {
        Name = "Sinners",
        FromDate = new DateTime(2025, 3, 20, 0, 0, 0, DateTimeKind.Utc),
        ToDate = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc),
        Actors = new List<string>{ "Kiernan Shipka", "Jena Malone" },
        ActorsUrl = new List<string>{ "", "" },
        Director = "Ryan Coogler",
        RunningTime = 115,
        Version = MovieVersion.FourD,
        TrailerUrl = "",
        Genres = new List<string>{ "Horror", "Original" },
        Description = "Original vampire horror film by Ryan Coogler.",
        PosterImage = "", BackgroundImage = "", Status = MovieStatus.ComingSoon
    },

    new Movie
    {
        Name = "Final Destination Bloodlines",
        FromDate = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
        ToDate = new DateTime(2025, 7, 20, 0, 0, 0, DateTimeKind.Utc),
        Actors = new List<string>{ "Tony Todd", "New Cast" },
        ActorsUrl = new List<string>{ "", "" },
        Director = "Zach Lipovsky",
        RunningTime = 110,
        Version = MovieVersion.IMAX,
        TrailerUrl = "",
        Genres = new List<string>{ "Horror", "Thriller" },
        Description = "Reboot/sequel to the Final Destination franchise.",
        PosterImage = "", BackgroundImage = "", Status = MovieStatus.ComingSoon
    },

    new Movie
    {
        Name = "Snow White",
        FromDate = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
        ToDate = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        Actors = new List<string>{ "Rachel Zegler", "Gal Gadot" },
        ActorsUrl = new List<string>{ "", "" },
        Director = "Marc Webb",
        RunningTime = 110,
        Version = MovieVersion.TwoD,
        TrailerUrl = "",
        Genres = new List<string>{ "Fantasy", "Musical" },
        Description = "Disney’s new live‑action Snow White adaptation.",
        PosterImage = "", BackgroundImage = "", Status = MovieStatus.ComingSoon
    },

        };

        _logger.Info("Seeding movie...");
        await _context.Movies.AddRangeAsync(movies);
        await _context.SaveChangesAsync();
        _logger.Success("Movies seeded successfully.");
    }
    private async Task ClearDatabase(MovieTheaterDbContext context)
    {
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            _logger.Info("Start deleting data in database...");

            var tablesToDelete = new List<Func<Task>>
            {
                () => context.Users.ExecuteDeleteAsync(),
                () => context.Movies.ExecuteDeleteAsync()
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