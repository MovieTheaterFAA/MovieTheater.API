using Microsoft.AspNetCore.Http;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.BlobDTOs;
using MovieTheater.Domain.DTOs.MovieDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Domain.Enums;
using MovieTheater.Infrastructure.Interfaces;
using System.Text.Json;

namespace MovieTheater.Application.Services
{
    public class MovieService : IMovieService
    {
        private readonly ILoggerService _loggerService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimsService _claimsService;
        private readonly IAuditLogService _auditLogService;
        private readonly IRedisService _redisService;
        private readonly IBlobService _blobService;

        public MovieService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService, IAuditLogService auditLogService, IRedisService redisService, IBlobService blobService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
            _claimsService = claimsService;
            _auditLogService = auditLogService;
            _redisService = redisService;
            _blobService = blobService;
        }

        public async Task<Pagination<MovieResponseDto>> GetAllMoviesAsync(
            string? search,
            string? sortBy,
            bool isDescending,
            int page,
            int pageSize,
            List<string>? genres = null,
            MovieStatus? status = null)
        {
            try
            {
                string genreKey = genres != null ? string.Join(",", genres.OrderBy(g => g)) : "";
                string cacheKey = $"movie:list:{search}:{sortBy}:{isDescending}:{page}:{pageSize}:{genreKey}:{status}";

                var cached = await _redisService.GetAsync<Pagination<MovieResponseDto>>(cacheKey);
                if (cached != null)
                {
                    _loggerService.Info($"[CACHE HIT] {cacheKey}");
                    return cached;
                }

                _loggerService.Info($"[CACHE MISS] {cacheKey} — Fetching from DB");

                var movies = await _unitOfWork.Movies.GetAllAsync();
                var query = movies.AsQueryable().Where(m => !m.IsDeleted);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowerSearch = search.ToLower();
                    query = query.Where(m =>
                        (!string.IsNullOrEmpty(m.Name) && m.Name.ToLower().Contains(lowerSearch)) ||
                        (!string.IsNullOrEmpty(m.Director) && m.Director.ToLower().Contains(lowerSearch)) ||
                        (m.Actors != null && m.Actors.Any(a => a.ToLower().Contains(lowerSearch)))
                    );
                }

                if (genres != null && genres.Any())
                {
                    query = query.Where(m => m.Genres != null && m.Genres.Any(g => genres.Contains(g)));
                }

                if (status.HasValue)
                {
                    query = query.Where(m => m.Status == status.Value);
                }

                var totalMovies = query.Count();

                query = sortBy?.ToLower() switch
                {
                    "name" => isDescending ? query.OrderByDescending(m => m.Name) : query.OrderBy(m => m.Name),
                    "fromdate" => isDescending ? query.OrderByDescending(m => m.FromDate) : query.OrderBy(m => m.FromDate),
                    "todate" => isDescending ? query.OrderByDescending(m => m.ToDate) : query.OrderBy(m => m.ToDate),
                    _ => query.OrderBy(m => m.Id)
                };

                var pagedMovies = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var result = pagedMovies.Select(m => new MovieResponseDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    FromDate = m.FromDate,
                    ToDate = m.ToDate,
                    Actors = m.Actors,
                    Director = m.Director,
                    RunningTime = m.RunningTime,
                    TrailerUrl = m.TrailerUrl,
                    Genres = m.Genres,
                    Description = m.Description,
                    PosterImage = m.PosterImage,
                    BackgroundImage = m.BackgroundImage,
                    Rating = m.Rating,
                    Status = m.Status
                }).ToList();

                var response = new Pagination<MovieResponseDto>(result, totalMovies, page, pageSize);

                await _redisService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));

                return response;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Failed to retrieve movies. Exception: {ex.Message}");
                throw new Exception("An error occurred while retrieving movies. Please try again later.");
            }
        }
        public async Task<MovieResponseDto> GetMovieDetailAsync(Guid movieId)
        {
            try
            {
                var cacheKey = $"movie:detail:{movieId}";

                var cached = await _redisService.GetAsync<MovieResponseDto>(cacheKey);
                if (cached != null)
                {
                    _loggerService.Info($"[CACHE HIT] {cacheKey}");
                    return cached;
                }

                _loggerService.Info($"[CACHE MISS] {cacheKey} — Fetching from DB");

                var movie = await _unitOfWork.Movies.GetByIdAsync(movieId);
                if (movie == null || movie.IsDeleted)
                {
                    _loggerService.Warn($"[GetMovieDetailAsync] Movie with ID {movieId} not found.");
                    throw new KeyNotFoundException($"Movie with ID {movieId} not found.");
                }

                var responseDto = new MovieResponseDto
                {
                    Id = movie.Id,
                    Name = movie.Name,
                    FromDate = movie.FromDate,
                    ToDate = movie.ToDate,
                    Actors = movie.Actors,
                    ActorsUrl = movie.ActorsUrl,
                    Director = movie.Director,
                    RunningTime = movie.RunningTime,
                    TrailerUrl = movie.TrailerUrl,
                    Genres = movie.Genres,
                    Description = movie.Description,
                    PosterImage = movie.PosterImage,
                    BackgroundImage = movie.BackgroundImage,
                    Status = movie.Status,
                    Rating = movie.Rating,
                };

                await _redisService.SetAsync(cacheKey, responseDto, TimeSpan.FromMinutes(10));

                return responseDto;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[GetMovieDetailAsync] Error fetching movie details for MovieId: {movieId}. Exception: {ex.Message}");
                throw new Exception("An error occurred while fetching movie details. Please try again later.");
            }
        }
        public async Task<List<MovieResponseDto>> GetMovieByNameAsync(string? Name)
        {
            try
            {
                var movies = await _unitOfWork.Movies.GetAllAsync();
                var movieQuery = movies.AsQueryable();
                movieQuery = movieQuery.Where(m => !m.IsDeleted);

                if (!string.IsNullOrWhiteSpace(Name))
                {
                    var name = Name.ToLower();
                    movieQuery = movieQuery.Where(m => !string.IsNullOrEmpty(m.Name) && m.Name.ToLower().Contains(name));
                }

                // Sort A-Z by movie name
                movieQuery = movieQuery.OrderBy(m => m.Name);

                var movieList = movieQuery.Select(m => new MovieResponseDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    FromDate = m.FromDate,
                    ToDate = m.ToDate,
                    Actors = m.Actors,
                    Director = m.Director,
                    RunningTime = m.RunningTime,
                    TrailerUrl = m.TrailerUrl,
                    Genres = m.Genres,
                    Description = m.Description,
                    PosterImage = m.PosterImage,
                    Status = m.Status,
                    Rating = m.Rating,
                    BackgroundImage = m.BackgroundImage,
                }).ToList();

                return movieList;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"SearchMoviesByNameAsync failed: {ex.Message}");
                throw new Exception("An error occurred while searching for movies. Please try again later.");
            }
        }
        public async Task<MovieResponseDto> AddMovieAsync(MovieRequestDto movieRequestDto)
        {
            _loggerService.Info($"[AddMovieAsync] Start adding movie {movieRequestDto.Name}");
            try
            {
                // Kiểm tra xem ngày `ToDate` có nhỏ hơn `FromDate` không
                if (movieRequestDto.FromDate > movieRequestDto.ToDate)
                {
                    _loggerService.Warn($"[AddMovieAsync] ToDate cannot be earlier than FromDate for movie {movieRequestDto.Name}");
                    throw new InvalidOperationException("ToDate cannot be earlier than FromDate.");
                }

                // Tạo mới đối tượng Movie từ MovieRequestDTO
                var movie = new Movie
                {
                    Name = movieRequestDto.Name,
                    FromDate = movieRequestDto.FromDate,
                    ToDate = movieRequestDto.ToDate,
                    Actors = new List<string>(),
                    ActorsUrl = new List<string>(),
                    Director = movieRequestDto.Director,
                    RunningTime = movieRequestDto.RunningTime,
                    TrailerUrl = movieRequestDto.TrailerUrl,
                    Genres = movieRequestDto.Genres,
                    Description = movieRequestDto.Description,
                    Rating = movieRequestDto.Rating,
                    Status = Domain.Enums.MovieStatus.ComingSoon,
                };

                var adminId = _claimsService.GetCurrentUserId;

                var newData = new
                {
                    movie.Name,
                    movie.FromDate,
                    movie.ToDate,
                    movie.Actors,
                    movie.ActorsUrl,
                    movie.Director,
                    movie.RunningTime,
                    movie.TrailerUrl,
                    movie.Genres,
                    movie.Description,
                    movie.PosterImage,
                    movie.BackgroundImage,
                    movie.Rating,
                    movie.Status
                };

                var changedFields = JsonSerializer.Serialize(new
                {

                    movie.Name,
                    movie.FromDate,
                    movie.ToDate,
                    movie.Actors,
                    movie.ActorsUrl,
                    movie.Director,
                    movie.RunningTime,
                    movie.TrailerUrl,
                    movie.Genres,
                    movie.Description,
                    movie.PosterImage,
                    movie.BackgroundImage,
                    movie.Rating,
                    movie.Status
                });



                // Thêm bộ phim vào cơ sở dữ liệu
                await _unitOfWork.Movies.AddAsync(movie);
                await _unitOfWork.SaveChangesAsync();
                await _redisService.RemoveByPatternAsync("movies:list:");

                await _auditLogService.LogAsync
                    (
                    adminId,
                    AuditActionType.Create,
                    "Movie",
                    movie.Id,
                    null!,
                    newData,
                    changedFields,
                    "Admin created new movie."
                    );

                _loggerService.Success($"[AddMovieAsync] Movie {movie.Name} added successfully.");
                // Trả về MovieResponseDto
                var responseDto = new MovieResponseDto
                {
                    Id = movie.Id,
                    Name = movie.Name,
                    FromDate = movie.FromDate,
                    ToDate = movie.ToDate,
                    Actors = movie.Actors,
                    Director = movie.Director,
                    RunningTime = movie.RunningTime,
                    TrailerUrl = movie.TrailerUrl,
                    Genres = movie.Genres,
                    Description = movie.Description,
                    PosterImage = movie.PosterImage,
                    BackgroundImage = movie.BackgroundImage,
                    Rating = movie.Rating,
                    Status = movie.Status,
                };

                return responseDto;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[AddMovie] Error add movie. Exception: {ex.Message}");
                throw;
            }
        }
        public async Task<MovieUpdateDto> UpdateMovieInfoAsync(Guid movieId, MovieUpdateDto movieUpdateDto)
        {
            try
            {
                _loggerService.Info($"[UpdateMovieInfo] Attempting to update movie info for MovieId: {movieId}");

                var movie = await _unitOfWork.Movies.GetByIdAsync(movieId);
                if (movie == null || movie.IsDeleted)
                {
                    _loggerService.Warn($"[UpdateMovieInfo] Movie with ID {movieId} not found.");
                    throw new KeyNotFoundException($"Movie with ID {movieId} not found.");
                }

                var oldData = new
                {
                    movie.Name,
                    movie.FromDate,
                    movie.ToDate,
                    movie.Actors,
                    movie.ActorsUrl,
                    movie.Director,
                    movie.RunningTime,
                    movie.TrailerUrl,
                    movie.Genres,
                    movie.Description,
                    movie.PosterImage,
                    movie.Status,
                    movie.Rating,
                };

                bool isUpdated = false;

                if (!string.IsNullOrWhiteSpace(movieUpdateDto.Name) && movie.Name != movieUpdateDto.Name)
                {
                    movie.Name = movieUpdateDto.Name;
                    isUpdated = true;
                }
                if (movieUpdateDto.FromDate.HasValue && movie.FromDate != movieUpdateDto.FromDate)
                {
                    if (movieUpdateDto.FromDate.Value < DateTime.UtcNow)
                        throw new ArgumentException("FromDate cannot be in the past.");
                    movie.FromDate = movieUpdateDto.FromDate.Value;
                    isUpdated = true;
                }
                if (movieUpdateDto.ToDate.HasValue && movie.ToDate != movieUpdateDto.ToDate)
                {
                    var to = movieUpdateDto.ToDate.Value;
                    var from = movieUpdateDto.FromDate ?? movie.FromDate;

                    if (to < from)
                        throw new ArgumentException("ToDate must be after FromDate.");

                    movie.ToDate = to;
                    isUpdated = true;
                }

                if (!string.IsNullOrWhiteSpace(movieUpdateDto.Director) && movie.Director != movieUpdateDto.Director)
                {
                    movie.Director = movieUpdateDto.Director;
                    isUpdated = true;
                }

                if (movieUpdateDto.RunningTime.HasValue && movie.RunningTime != movieUpdateDto.RunningTime)
                {
                    movie.RunningTime = movieUpdateDto.RunningTime;
                    isUpdated = true;
                }

                if (!string.IsNullOrWhiteSpace(movieUpdateDto.TrailerUrl) && movie.TrailerUrl != movieUpdateDto.TrailerUrl)
                {
                    movie.TrailerUrl = movieUpdateDto.TrailerUrl;
                    isUpdated = true;
                }

                if (movieUpdateDto.Genres is { Count: > 0 } &&
                    !movie.Genres?.SequenceEqual(movieUpdateDto.Genres) == true)
                {
                    movie.Genres = new List<string>(movieUpdateDto.Genres);
                    isUpdated = true;
                }

                if (!string.IsNullOrWhiteSpace(movieUpdateDto.Description) && movie.Description != movieUpdateDto.Description)
                {
                    movie.Description = movieUpdateDto.Description;
                    isUpdated = true;
                }

                if (movieUpdateDto.Rating >= 0 && movieUpdateDto.Rating <= 10 && movie.Rating != movieUpdateDto.Rating)
                {
                    movie.Rating = movieUpdateDto.Rating.Value;
                    isUpdated = true;
                }

                if (Enum.IsDefined(typeof(MovieStatus), movieUpdateDto.Status!) && movie.Status != movieUpdateDto.Status)
                {
                    movie.Status = movieUpdateDto.Status!.Value;
                    isUpdated = true;
                }

                if (!isUpdated)
                {
                    _loggerService.Warn($"[UpdateMovieInfo] No changes detected for MovieId: {movieId}");
                    return new MovieUpdateDto
                    {
                        Name = movie.Name,
                        FromDate = movie.FromDate,
                        ToDate = movie.ToDate,
                        Director = movie.Director,
                        RunningTime = movie.RunningTime,
                        TrailerUrl = movie.TrailerUrl,
                        Genres = movie.Genres,
                        Description = movie.Description,
                        Rating = movie.Rating,
                        Status = movie.Status,
                    };
                }

                await _unitOfWork.Movies.Update(movie);
                await _unitOfWork.SaveChangesAsync();

                await _redisService.RemoveByPatternAsync("movie:list:*");
                await _redisService.RemoveByPatternAsync("movie:detail:*");

                var newData = new
                {
                    movie.Name,
                    movie.FromDate,
                    movie.ToDate,
                    movie.Actors,
                    movie.ActorsUrl,
                    movie.Director,
                    movie.RunningTime,
                    movie.TrailerUrl,
                    movie.Genres,
                    movie.Description,
                    movie.PosterImage,
                    movie.Status,
                    movie.Rating,
                };

                var changedFields = JsonSerializer.Serialize(new
                {
                    movie.Name,
                    movie.FromDate,
                    movie.ToDate,
                    movie.Actors,
                    movie.ActorsUrl,
                    movie.Director,
                    movie.RunningTime,
                    movie.TrailerUrl,
                    movie.Genres,
                    movie.Description,
                    movie.PosterImage,
                    movie.Status,
                    movie.Rating,
                });

                var adminId = _claimsService.GetCurrentUserId;

                await _auditLogService.LogAsync
                    (
                    adminId,
                    AuditActionType.Update,
                    "Movie",
                    movieId,
                    oldData,
                    newData,
                    changedFields,
                    "Admin updated movie information."
                    );

                _loggerService.Success($"[UpdateMovieInfo] Movie info updated successfully for MovieId: {movieId}");
                return new MovieUpdateDto
                {
                    Name = movie.Name,
                    FromDate = movie.FromDate,
                    ToDate = movie.ToDate,
                    Director = movie.Director,
                    RunningTime = movie.RunningTime,
                    TrailerUrl = movie.TrailerUrl,
                    Genres = movie.Genres,
                    Description = movie.Description,
                    Rating = movie.Rating,
                    Status = movie.Status,
                };
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[UpdateMovieInfo] Error updating movie info for MovieId: {movieId}. Exception: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> DeleteMovieAsync(Guid movieId)
        {
            try
            {
                var movie = await _unitOfWork.Movies.GetByIdAsync(movieId);
                if (movie == null)
                {
                    _loggerService.Warn($"Movie with ID {movieId} not found.");
                    return false;
                }

                var oldValue = new
                {
                    movie.IsDeleted
                };

                await _unitOfWork.Movies.SoftRemove(movie);
                await _unitOfWork.SaveChangesAsync();

                await _redisService.RemoveByPatternAsync("movie:list:*");
                await _redisService.RemoveByPatternAsync("movie:detail:*");

                var newValue = new
                {
                    movie.IsDeleted
                };

                var changedFields = JsonSerializer.Serialize(new
                {
                    movie.IsDeleted
                });

                var adminId = _claimsService.GetCurrentUserId;

                await _auditLogService.LogAsync
                        (
                        adminId,
                        AuditActionType.Delete,
                        "Movie",
                        movieId,
                        oldValue,
                        newValue,
                        changedFields,
                        "Admin deleted movie."
                        );

                _loggerService.Info($"Successfully deleted movie with {movieId}.");

                return true;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"An error occurred while deleting movie : {ex.Message}");
                return false;
            }
        }



        //===================== Add Movie with Files =====================
        public async Task<MovieResponseDto> AddMovieWithFilesAsync(MovieCreateWithFilesDto dto)
        {
            // 1. Add the movie (without images first)
            var movieResponse = await AddMovieAsync(dto.Movie);

            // 2. Upload poster
            if (dto.Poster != null)
            {
                var posterUrl = await UploadMoviePosterInternal(movieResponse.Id, dto.Poster);
                movieResponse.PosterImage = posterUrl;
            }

            // 3. Upload background
            if (dto.Background != null)
            {
                var backgroundUrl = await UploadMovieBackgroundInternal(movieResponse.Id, dto.Background);
                movieResponse.BackgroundImage = backgroundUrl;
            }

            // 4. Upload cast images
            if (dto.CastImages != null)
            {
                foreach (var cast in dto.CastImages)
                {
                    if (cast.File != null && !string.IsNullOrWhiteSpace(cast.ActorName))
                    {
                        await UploadCastImageInternal(movieResponse.Id, cast);
                    }
                }
            }

            // Optionally, reload the movie to get updated URLs
            return await GetMovieDetailAsync(movieResponse.Id);
        }

        //===================== File Upload Methods =====================
        private async Task<string> UploadMoviePosterInternal(Guid movieId, IFormFile file)
        {
            var folder = $"movies/{movieId}/poster";
            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var objectName = $"{folder}/{uniqueFileName}";
            using var stream = file.OpenReadStream();
            await _blobService.UploadFileAsync(uniqueFileName, stream, folder);
            var previewUrl = await _blobService.GetPreviewUrlAsync(objectName);

            // Update DB
            var movie = await _unitOfWork.Movies.GetByIdAsync(movieId);
            movie.PosterImage = previewUrl;
            await _unitOfWork.Movies.Update(movie);
            await _unitOfWork.SaveChangesAsync();

            return previewUrl;
        }

        private async Task<string> UploadMovieBackgroundInternal(Guid movieId, IFormFile file)
        {
            var folder = $"movies/{movieId}/background";
            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var objectName = $"{folder}/{uniqueFileName}";
            using var stream = file.OpenReadStream();
            await _blobService.UploadFileAsync(uniqueFileName, stream, folder);
            var previewUrl = await _blobService.GetPreviewUrlAsync(objectName);

            // Update DB
            var movie = await _unitOfWork.Movies.GetByIdAsync(movieId);
            movie.BackgroundImage = previewUrl;
            await _unitOfWork.Movies.Update(movie);
            await _unitOfWork.SaveChangesAsync();

            return previewUrl;
        }

        private async Task UploadCastImageInternal(Guid movieId, MovieCastUploadDto cast)
        {
            var actorName = cast.ActorName?.Trim();
            var safeActor = actorName.Replace(" ", "_").ToLowerInvariant();
            var folder = $"movies/{movieId}/cast/{safeActor}";
            var uniqueFileName = $"{Guid.NewGuid()}_{cast.File.FileName}";
            var objectName = $"{folder}/{uniqueFileName}";
            using var stream = cast.File.OpenReadStream();
            await _blobService.UploadFileAsync(uniqueFileName, stream, folder);
            var previewUrl = await _blobService.GetPreviewUrlAsync(objectName);

            // Update DB
            var movie = await _unitOfWork.Movies.GetByIdAsync(movieId);
            movie.Actors ??= new List<string>();
            movie.ActorsUrl ??= new List<string>();
            if (!movie.Actors.Contains(actorName, StringComparer.OrdinalIgnoreCase))
            {
                movie.Actors.Add(actorName);
                movie.ActorsUrl.Add(previewUrl);
                await _unitOfWork.Movies.Update(movie);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}