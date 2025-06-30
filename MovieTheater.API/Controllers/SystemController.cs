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

            await SeedUserAsync();
            await SeedMovieAsync();
            await SeedCinemaRoomAsync();
            await SeedShowTimeForAllRoomsAndMoviesAsync();
            await SeedFoodAndDrinkAsync();
            await SeedEventAndPromotionAsync();
            await SeedSeatsForAllCinemaRoomsAsync();
            //await SeedShowTimeSeatsWithRandomStatusAsync();

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
            new()
        {
            FullName = "System Owner User",
            Email = "system@gmail.com",
            Sex = Gender.Female,
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            PhoneNumber = "0944000000",
            Password = passwordHasher.HashPassword("1@"),
            Role = RoleType.SystemOwner,
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

        // Remove any existing rooms with the same names to avoid duplicates
        var existingNames = rooms.Select(r => r.Name).ToList();
        var existingRooms = await _context.CinemaRooms
            .Where(r => existingNames.Contains(r.Name))
            .ToListAsync();

        if (existingRooms.Any())
        {
            _context.CinemaRooms.RemoveRange(existingRooms);
            await _context.SaveChangesAsync();
        }

        _logger.Info("Seeding cinema rooms...");
        await _context.CinemaRooms.AddRangeAsync(rooms);
        await _context.SaveChangesAsync();
        _logger.Success("Cinema rooms seeded successfully.");
    }
    private async Task SeedShowTimeForAllRoomsAndMoviesAsync()
    {
        var rooms = await _context.CinemaRooms.ToListAsync();
        var movies = await _context.Movies.Where(m => m.Status == MovieStatus.NowShowing).ToListAsync();
        var showtimes = new List<ShowTime>();
        var random = new Random();

        var today = DateTime.UtcNow.Date;
        var endOfJuly = new DateTime(today.Year, 7, 31);

        foreach (var movie in movies)
        {
            if (!movie.RunningTime.HasValue) continue;

            // Pick 2 random, distinct cinema rooms for this movie
            var selectedRooms = rooms.OrderBy(_ => random.Next()).Take(2).ToList();

            foreach (var room in selectedRooms)
            {
                var currentDay = today;
                while (currentDay <= endOfJuly)
                {
                    // For each day, schedule multiple showtimes from 8AM to midnight
                    var scheduledWindows = new List<(DateTime Start, DateTime End)>();
                    var startHour = 8;
                    var endHour = 24;
                    var runningTime = movie.RunningTime.Value;
                    var restTime = 15; // 15 minutes rest after each show

                    var hour = startHour;
                    while (hour < endHour)
                    {
                        // Randomize minute (0, 15, 30, 45) for some variety
                        var minute = random.Next(0, 4) * 15;
                        var start = currentDay.AddHours(hour).AddMinutes(minute);

                        // Calculate end time (movie duration + rest)
                        var duration = TimeSpan.FromMinutes(runningTime);
                        var totalDuration = duration.Add(TimeSpan.FromMinutes(restTime));
                        var end = start.Add(totalDuration);

                        // Check for overlap with already scheduled showtimes for this day/room/movie
                        bool overlap = scheduledWindows.Any(w => w.Start < end && start < w.End);
                        if (!overlap && end.Hour <= endHour)
                        {
                            showtimes.Add(new ShowTime
                            {
                                Id = Guid.NewGuid(),
                                CinemaRoomId = room.Id,
                                MovieId = movie.Id,
                                ShowDate = start,
                                Duration = duration,
                                CreatedAt = DateTime.UtcNow,
                                CreatedBy = Guid.Empty // System seed
                            });
                            scheduledWindows.Add((start, end));
                            // Move hour forward to after this showtime (plus rest)
                            hour = end.Hour;
                            // If the end minute is not 0, move to next quarter
                            if (end.Minute > 0)
                            {
                                hour = end.Hour;
                            }
                        }
                        else
                        {
                            // If overlap or out of range, try next 15-min slot
                            hour += 1;
                        }
                    }
                    currentDay = currentDay.AddDays(1);
                }
            }
        }

        await _context.Showtimes.AddRangeAsync(showtimes);
        await _context.SaveChangesAsync();

        _logger.Success($"Seeded {showtimes.Count} non-overlapping showtimes for all movies in random 2 cinema rooms each, from today to end of July, with multiple showtimes per day.");
    }
    private async Task SeedSeatsForAllCinemaRoomsAsync()
    {
        var rooms = await _context.CinemaRooms.ToListAsync();
        var seatList = new List<Seat>();

        foreach (var room in rooms)
        {
            // Only seed if no seats exist for this room
            var existing = await _context.Seats.AnyAsync(s => s.CinemaRoomId == room.Id);
            if (existing) continue;

            if (room.Type == RoomType.TwoD)
            {
                // 2D: 10 rows, 13 columns (A-M)
                int rowCount = 10;
                int colCount = 13;
                for (int rowIdx = 1; rowIdx <= rowCount; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < colCount; colIdx++)
                    {
                        string colLabel = ((char)('A' + colIdx)).ToString();
                        SeatType seatType;
                        if (colIdx == 12) // M (last column)
                            seatType = SeatType.Couple;
                        else if (colIdx >= 5 && colIdx <= 11) // F-L
                            seatType = SeatType.VIP;
                        else
                            seatType = SeatType.Normal;

                        seatList.Add(new Seat
                        {
                            Id = Guid.NewGuid(),
                            CinemaRoomId = room.Id,
                            Row = colLabel,
                            Number = rowIdx,
                            Type = seatType,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = Guid.Empty // System seed
                        });
                    }
                }
                _logger.Info($"Seeded {rowCount * colCount} seats for 2D room {room.Name}.");
            }
            else if (room.Type == RoomType.IMAX)
            {
                // IMAX: 12 rows, variable columns
                int rowCount = 12;
                for (int rowIdx = 1; rowIdx <= rowCount; rowIdx++)
                {
                    int colStart = 0;
                    int colEnd = 0;
                    if (rowIdx >= 1 && rowIdx <= 4)
                    {
                        colStart = 0; colEnd = 11; // A-L (12 seats)
                    }
                    else if (rowIdx >= 5 && rowIdx <= 8)
                    {
                        colStart = 0; colEnd = 14; // A-O (15 seats)
                    }
                    else // 9-12
                    {
                        colStart = 0; colEnd = 15; // A-P (16 seats)
                    }

                    for (int colIdx = colStart; colIdx <= colEnd; colIdx++)
                    {
                        string colLabel = ((char)('A' + colIdx)).ToString();
                        SeatType seatType;
                        if (colIdx == colEnd) // Last column (L, O, or P)
                            seatType = SeatType.Couple;
                        else if (colIdx >= 5 && colIdx <= colEnd - 1) // F to one before last
                            seatType = SeatType.VIP;
                        else
                            seatType = SeatType.Normal;

                        seatList.Add(new Seat
                        {
                            Id = Guid.NewGuid(),
                            CinemaRoomId = room.Id,
                            Row = colLabel,
                            Number = rowIdx,
                            Type = seatType,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = Guid.Empty // System seed
                        });
                    }
                }
                _logger.Info($"Seeded seats for IMAX room {room.Name}.");
            }
            else if (room.Type == RoomType.FourD)
            {
                // 4DX: 6 rows, 10 columns (A-J), 3-4-3 split, all VIP
                int rowCount = 6;
                for (int rowIdx = 1; rowIdx <= rowCount; rowIdx++)
                {
                    // Left section: A-C
                    for (int colIdx = 0; colIdx <= 2; colIdx++)
                    {
                        string colLabel = ((char)('A' + colIdx)).ToString();
                        seatList.Add(new Seat
                        {
                            Id = Guid.NewGuid(),
                            CinemaRoomId = room.Id,
                            Row = colLabel,
                            Number = rowIdx,
                            Type = SeatType.VIP,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = Guid.Empty // System seed
                        });
                    }
                    // Center section: D-G
                    for (int colIdx = 3; colIdx <= 6; colIdx++)
                    {
                        string colLabel = ((char)('A' + colIdx)).ToString();
                        seatList.Add(new Seat
                        {
                            Id = Guid.NewGuid(),
                            CinemaRoomId = room.Id,
                            Row = colLabel,
                            Number = rowIdx,
                            Type = SeatType.VIP,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = Guid.Empty // System seed
                        });
                    }
                    // Right section: H-J
                    for (int colIdx = 7; colIdx <= 9; colIdx++)
                    {
                        string colLabel = ((char)('A' + colIdx)).ToString();
                        seatList.Add(new Seat
                        {
                            Id = Guid.NewGuid(),
                            CinemaRoomId = room.Id,
                            Row = colLabel,
                            Number = rowIdx,
                            Type = SeatType.VIP,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = Guid.Empty // System seed
                        });
                    }
                }
                _logger.Info($"Seeded seats for 4DX room {room.Name}.");
            }
        }

        if (seatList.Count > 0)
        {
            await _context.Seats.AddRangeAsync(seatList);
            await _context.SaveChangesAsync();
            _logger.Success($"Seeded {seatList.Count} seats for all cinema rooms.");
        }
        else
        {
            _logger.Info("No new seats to seed.");
        }
    }

    private async Task SeedShowTimeSeatsWithRandomStatusAsync()
    {
        var showtimes = await _context.Showtimes.ToListAsync();
        var allSeats = await _context.Seats.ToListAsync();
        var random = new Random();
        var seatStatusValues = Enum.GetValues(typeof(SeatStatus)).Cast<SeatStatus>().ToArray();
        var showTimeSeats = new List<ShowTimeSeat>();

        foreach (var showtime in showtimes)
        {
            // Get seats for the cinema room of this showtime
            var seatsInRoom = allSeats.Where(s => s.CinemaRoomId == showtime.CinemaRoomId).ToList();

            foreach (var seat in seatsInRoom)
            {
                showTimeSeats.Add(new ShowTimeSeat
                {
                    Id = Guid.NewGuid(),
                    ShowTimeId = showtime.Id,
                    SeatId = seat.Id,
                    Status = seatStatusValues[random.Next(seatStatusValues.Length)],
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = Guid.Empty // System seed
                });
            }
        }

        if (showTimeSeats.Count > 0)
        {
            await _context.ShowTimeSeats.AddRangeAsync(showTimeSeats);
            await _context.SaveChangesAsync();
            _logger.Success($"Seeded {showTimeSeats.Count} ShowTimeSeats with random status.");
        }
        else
        {
            _logger.Info("No ShowTimeSeats to seed.");
        }
    }
    private async Task SeedEventAndPromotionAsync()
    {
        var events = new List<Event>
    {
        new Event
        {
            Name = "Summer Blockbuster Festival",
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(30),
            Detail = "Enjoy the hottest movies and exclusive deals all summer long!",
            Image = "https://iguov8nhvyobj.vcdn.cloud/media/wysiwyg/2024/112024/Happy_Day_Oct_28_N_O_350x495.jpg",
            Promotions = new List<Promotion>
            {
                new Promotion { Title = "Buy 1 Get 1 Free", DiscountValue = 0.5m, Detail = "Buy one ticket, get one free for select movies." },
                new Promotion { Title = "Free Popcorn Combo", DiscountValue = 0.2m, Detail = "Get a free popcorn combo with every 2 tickets." },
                new Promotion { Title = "Student Discount", DiscountValue = 0.15m, Detail = "Students enjoy 15% off all showtimes." },
                new Promotion { Title = "Family Pack", DiscountValue = 0.25m, Detail = "Family pack: 4 tickets + snacks at 25% off." },
                new Promotion { Title = "Early Bird", DiscountValue = 0.1m, Detail = "10% off for tickets booked before 10am." }
            }
        },
        new Event
        {
            Name = "Mid-Autumn Movie Night",
            StartTime = DateTime.UtcNow.AddDays(10),
            EndTime = DateTime.UtcNow.AddDays(40),
            Detail = "Celebrate Mid-Autumn with special screenings and mooncake treats.",
            Image = "https://iguov8nhvyobj.vcdn.cloud/media/wysiwyg/2025/042025/full_01.jpg",
            Promotions = new List<Promotion>
            {
                new Promotion { Title = "Mooncake Gift", DiscountValue = 0.05m, Detail = "Free mooncake with every ticket." },
                new Promotion { Title = "Couple Night", DiscountValue = 0.2m, Detail = "20% off for couples booking together." },
                new Promotion { Title = "Kids Free", DiscountValue = 0.5m, Detail = "Kids under 12 get 50% off." },
                new Promotion { Title = "Snack Combo", DiscountValue = 0.3m, Detail = "30% off on all snack combos." },
                new Promotion { Title = "Late Night Show", DiscountValue = 0.12m, Detail = "12% off for shows after 9pm." }
            }
        },
        new Event
        {
            Name = "New Year Movie Marathon",
            StartTime = DateTime.UtcNow.AddDays(20),
            EndTime = DateTime.UtcNow.AddDays(50),
            Detail = "Ring in the New Year with a marathon of blockbuster hits.",
            Image = "https://iguov8nhvyobj.vcdn.cloud/media/wysiwyg/2025/052025/350x495_4_.jpg",
            Promotions = new List<Promotion>
            {
                new Promotion { Title = "Marathon Pass", DiscountValue = 0.4m, Detail = "40% off for all-day marathon passes." },
                new Promotion { Title = "Free Drink", DiscountValue = 0.1m, Detail = "Free drink with every ticket." },
                new Promotion { Title = "Group Discount", DiscountValue = 0.2m, Detail = "20% off for groups of 5 or more." },
                new Promotion { Title = "Lucky Draw", DiscountValue = 0.05m, Detail = "Enter lucky draw with every purchase." },
                new Promotion { Title = "VIP Upgrade", DiscountValue = 0.3m, Detail = "Upgrade to VIP seats at 30% off." }
            }
        },
        new Event
        { 
            Name = "Happy Birthday - Special Gift",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddYears(10),
            Detail = "Celebrate your birthday with MovieTheater! Enjoy a free ticket or 50% off during your birthday week.",
            Image = "https://m.media-amazon.com/images/I/91x6DRQyTkL._UF894,1000_QL80_.jpg",
            Promotions = new List<Promotion>
            {
                new Promotion { Title = "Birthday Week Discount", DiscountValue = 0.2m, Detail = "Enjoy 20% off for up to 1 tickets during your birthday week." }
            }
        }
    };

        _logger.Info("Seeding events and promotions...");
        await _context.Events.AddRangeAsync(events);
        await _context.SaveChangesAsync();
        _logger.Success("Events and promotions seeded successfully.");
    }
    private async Task SeedFoodAndDrinkAsync()
    {
        var foodanddrinks = new List<FoodAndDrink>
        {
    new()
    {
        Name = "Butter Popcorn",
        Description = "Aromatic, crispy popcorn with a savory butter flavor.",
        Price = 45000m, // 45,000₫
        Type = FoodType.Food,
        ImageUrl = null,
        IsAvailable = true
    },
    new()
    {
        Name = "Pepsi Can",
        Description = "Carbonated soft drink, perfect with popcorn.",
        Price = 25000m, // 25,000₫
        Type = FoodType.Drink,
        ImageUrl = null,
        IsAvailable = true
    },
    new()
    {
        Name = "Combo 1: Popcorn + Pepsi",
        Description = "Value combo including a serving of butter popcorn and one Pepsi can.",
        Price = 65000m, // 65,000₫
        Type = FoodType.Combo,
        ImageUrl = null,
        IsAvailable = true
    },
    new()
    {
        Name = "Cheese Sausage",
        Description = "Hot sausage filled with melted cheese, delicious and satisfying.",
        Price = 30000m, // 30,000₫
        Type = FoodType.Food,
        ImageUrl = null,
        IsAvailable = true
    },
    new()
    {
        Name = "Peach Citrus Lemongrass Tea",
        Description = "Refreshing drink with the aroma of peach, citrus, and lemongrass.",
        Price = 35000m, // 35,000₫
        Type = FoodType.Drink,
        ImageUrl = null,
        IsAvailable = true
    },
    new()
    {
        Name = "French Fries",
        Description = "Crispy potato fries, served with ketchup or cheese sauce.",
        Price = 30000m, // 30,000₫
        Type = FoodType.Food,
        ImageUrl = null,
        IsAvailable = true
    },
    new()
    {
        Name = "Aquafina Water",
        Description = "500ml purified bottled water, convenient and refreshing.",
        Price = 15000m, // 15,000₫
        Type = FoodType.Drink,
        ImageUrl = null,
        IsAvailable = true
    },
    new()
    {
        Name = "Combo 2: Large Popcorn + 2 Pepsi",
        Description = "Perfect for two: 1 large popcorn and 2 Pepsi cans.",
        Price = 90000m, // 90,000₫
        Type = FoodType.Combo,
        ImageUrl = null,
        IsAvailable = true
    },
    new()
    {
        Name = "Hotdog Sausage",
        Description = "Soft bun with hot sausage, mayonnaise, and ketchup.",
        Price = 35000m, // 35,000₫
        Type = FoodType.Food,
        ImageUrl = null,
        IsAvailable = true
    },
    new()
    {
        Name = "Coca-Cola Bottle",
        Description = "Classic carbonated drink, 390ml plastic bottle.",
        Price = 27000m, // 27,000₫
        Type = FoodType.Drink,
        ImageUrl = null,
        IsAvailable = true
        }

    };

        _logger.Info("Seeding food and drinks...");

        await _context.FoodAndDrinks.AddRangeAsync(foodanddrinks);
        await _context.SaveChangesAsync();
        _logger.Success("Food and Drink seeded successfully.");
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
                () => context.ShowTimeSeats.ExecuteDeleteAsync(),
                () => context.Showtimes.ExecuteDeleteAsync(),
                () => context.AuditLogs.ExecuteDeleteAsync(),
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