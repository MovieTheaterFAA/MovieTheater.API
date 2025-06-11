using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Utils;
using MovieTheater.Domain;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;

namespace MovieTheater.API.Controllers;

[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
    private readonly MovieTheaterDbContext _context;
    private readonly ILoggerService _logger;

    public SystemController(MovieTheaterDbContext context, ILoggerService logger, IAuditLogService auditLogService)
    {
        _context = context;
        _logger = logger;
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
            await SeedMovieAsync();
            //Seed cinema rooms and seats
            await SeedCinemaRoomAsync();

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

    private async Task SeedMovieAsync()
    {
        var movies = new List<Movie>
        {
    new Movie
    {
        Name = "Ne Zha 2",
        FromDate = new DateTime(2025, 1, 29, 0, 0, 0, DateTimeKind.Utc),
        ToDate = new DateTime(2025, 7, 31, 0, 0, 0, DateTimeKind.Utc),
        Actors = new List<string>{ "Lü Yanting", "Han Mo" },
        ActorsUrl = new List<string>
        { "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=movie-actor%2Fyanting-lu.jpg&version_id=null",
          "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=movie-actor%2Fmo-han.jpg&version_id=null",
        },
        Director = "Jiaozi",
        RunningTime = 144,
        TrailerUrl = "gsiAYjyiIBM",
        Genres = new List<string>{ "Animation", "Fantasy", "Action" },
        Rating = 8.5f,
        Description = "After a great catastrophe, the souls of Nezha and Aobing are saved, but their bodies face ruin. To give them new life, Taiyi Zhenren turns to the mystical seven-colored lotus in a daring bid to rebuild them and change their fate.",
        PosterImage = "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=poster-film%2Fnatra.jpg&version_id=null",
        BackgroundImage = "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=movie-background%2Fnatra.jpg&version_id=null",
        Status = MovieStatus.NowShowing
    },

    new Movie
    {
        Name = "A Minecraft Movie",
        FromDate = new DateTime(2025, 4, 4, 0, 0, 0, DateTimeKind.Utc),
        ToDate = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc),
        Actors = new List<string>{ "Jason Momoa", "Jack Black" },
        ActorsUrl = new List<string>{
            "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=movie-actor%2Fjason-momoa.jpg&version_id=null",
            "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=movie-actor%2Fjack-black.jpg&version_id=null"
        },
        Director = "Jared Hess",
        RunningTime = 100,
        TrailerUrl = "8B1EtVPBSMw",
        Rating = 7.2f,
        Genres = new List<string>{ "Adventure", "Fantasy" },
        Description = "Four misfits are suddenly pulled through a mysterious portal into a bizarre cubic wonderland that thrives on imagination. To get back home they'll have to master this world while embarking on a quest with an unexpected expert crafter.",
        PosterImage = "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=poster-film%2Fminecraft.jpg&version_id=null",
        BackgroundImage = "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=movie-background%2Fminecraft.jpg&version_id=null",
        Status = MovieStatus.NowShowing
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
        TrailerUrl = "",
        Rating = 7.2f,
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
        TrailerUrl = "",
        Rating = 7.2f,
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
        ActorsUrl = new List<string>{
            "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=movie-actor%2Ftom-cruise.jpg&version_id=null",
            "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=movie-actor%2Fhayley.jpg&version_id=null" },
        Director = "Christopher McQuarrie",
        RunningTime = 150,
        TrailerUrl = "fsQgc9pCyDU",
        Rating = 8,
        Genres = new List<string>{ "Action", "Thriller" },
        Description = "The eighth installment of M:I franchise.",
        PosterImage = "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=poster-film%2Fmission-impossible.jpg&version_id=null",
        BackgroundImage = "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=movie-background%2Fmission-impossible.jpg&version_id=null",
        Status = MovieStatus.NowShowing
    },

    new Movie
    {
        Name = "Captain America: Brave New World",
        FromDate = new DateTime(2025, 5, 2, 0, 0, 0, DateTimeKind.Utc),
        ToDate = new DateTime(2025, 7, 30, 0, 0, 0, DateTimeKind.Utc),
        Actors = new List<string>{ "Anthony Mackie", "Liv Tyler" },
        ActorsUrl = new List<string>{
            "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=movie-actor%2Fanthony-mackie.jpg&version_id=null",
            "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=movie-actor%2Fliv-tyler.jpg&version_id=null"
        },
        Director = "Julius Onah",
        RunningTime = 130,
        TrailerUrl = "1pHDWnXmK7Y",
        Rating = 8.5f,
        Genres = new List<string>{ "Superhero", "Action" },
        Description = "Marvel's Captain America continues with Sam Wilson.",
        PosterImage = "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=poster-film%2Fcaptain-america.jpg&version_id=null",
        BackgroundImage = "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=movie-background%2Fcaptain-america.jpg&version_id=null",
        Status = MovieStatus.NowShowing
    },

    new Movie
    {
        Name = "Thunderbolts",
        FromDate = new DateTime(2025, 4, 25, 0, 0, 0, DateTimeKind.Utc),
        ToDate = new DateTime(2025, 7, 20, 0, 0, 0, DateTimeKind.Utc),
        Actors = new List<string>{ "Sebastian Stan", "Florence Pugh" },
        ActorsUrl = new List<string>{
            "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=movie-actor%2Fsebastian-stan.jpg&version_id=null",
            "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=movie-actor%2Fflorence-pugh.jpg&version_id=null"
        },
        Director = "Jake Schreier",
        RunningTime = 120,
        TrailerUrl = "-sAOWhvheK8",
        Rating = 6.5f,
        Genres = new List<string>{ "Superhero", "Action" },
        Description = "Marvel anti‑hero team-up film.",
        PosterImage = "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=poster-film%2Fthunderbolts.jpg&version_id=null",
        BackgroundImage = "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=movie-background%2Fthunderbolts.jpg&version_id=null",
        Status = MovieStatus.NowShowing
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
        TrailerUrl = "",
        Rating = 7.8f,
        Genres = new List<string>{ "Horror", "Original" },
        Description = "Original vampire horror film by Ryan Coogler.",
        PosterImage = "", BackgroundImage = "", Status = MovieStatus.ComingSoon
    },

    new Movie
    {
        Name = "Final Destination Bloodlines",
        FromDate = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
        ToDate = new DateTime(2025, 7, 20, 0, 0, 0, DateTimeKind.Utc),
        Actors = new List<string>{ "Tony Todd", "Brec Bassinger" },
        ActorsUrl = new List<string>{
            "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=movie-actor%2Ftony-todd.jpg&version_id=null",
            "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=movie-actor%2Fbrec-bassinger.jpg&version_id=null"
        },
        Director = "Zach Lipovsky",
        RunningTime = 110,
        TrailerUrl = "UWMzKXsY9A4",
        Rating = 7.5f,
        Genres = new List<string>{ "Horror", "Thriller" },
        Description = "Reboot/sequel to the Final Destination franchise.",
        PosterImage = "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=poster-film%2Ffinal-destination.jpg&version_id=null",
        BackgroundImage = "https://minio.fpt-devteam.fun/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=movie-background%2Ffinal-destination.jpg&version_id=null",
        Status = MovieStatus.NowShowing
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
        TrailerUrl = "",
        Rating = 7.0f,
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

    private async Task SeedCinemaRoomAsync()
    {
        var rooms = new List<CinemaRoom>
    {
        new() { Name = "IMAX Room 1", Type = RoomType.IMAX },
        new() { Name = "IMAX Room 2", Type = RoomType.IMAX },
        new() { Name = "2D Room 1",   Type = RoomType.TwoD },
        new() { Name = "2D Room 2",   Type = RoomType.TwoD },
        new() { Name = "4D Room 1",   Type = RoomType.FourD }
    };

        var seatLayouts = new Dictionary<RoomType, int>
    {
        { RoomType.IMAX, 120 },
        { RoomType.TwoD, 80 },
        { RoomType.FourD, 60 }
    };

        var seats = new List<Seat>();

        foreach (var room in rooms)
        {
            int totalSeats = seatLayouts[room.Type];
            int rows = 6;
            int cols = totalSeats / rows;
            int seatCounter = 0;

            for (int r = 0; r < rows; r++)
            {
                char rowLabel = (char)('A' + r);
                SeatType seatType;

                if (r == 0) seatType = SeatType.Couple;                  // Row A
                else if (r >= 1 && r <= 3) seatType = SeatType.VIP;      // Rows B, C, D
                else seatType = SeatType.Normal;                         // Rows E, F

                for (int c = 1; c <= cols; c++)
                {
                    seats.Add(new Seat
                    {
                        CinemaRoom = room,
                        Row = rowLabel.ToString(),
                        Number = c,
                        Type = seatType,
                        Status = SeatStatus.Available
                    });
                    seatCounter++;
                }
            }

            room.SeatQuantity = seatCounter;
        }

        _logger.Info("Seeding cinema rooms and seats...");
        await _context.CinemaRooms.AddRangeAsync(rooms);
        await _context.Seats.AddRangeAsync(seats);
        await _context.SaveChangesAsync();
        _logger.Success("Cinema rooms and seats seeded successfully.");
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
                () => context.Movies.ExecuteDeleteAsync(),
                () => context.Seats.ExecuteDeleteAsync(),
                () => context.CinemaRooms.ExecuteDeleteAsync(),
                () => context.FoodAndDrinks.ExecuteDeleteAsync(),
                () => context.Events.ExecuteDeleteAsync(),
                () => context.Promotions.ExecuteDeleteAsync(),
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
}