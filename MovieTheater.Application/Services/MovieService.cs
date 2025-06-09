using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services.Commons;
using MovieTheater.Domain.DTOs.MovieDTOs;
using MovieTheater.Domain.Entities;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Application.Services
{
    public class MovieService : IMovieService
    {

        private readonly ILoggerService _loggerService;
        private readonly IUnitOfWork _unitOfWork;
        public MovieService(IUnitOfWork unitOfWork, ILoggerService loggerService)
        {
            _unitOfWork = unitOfWork;
            _loggerService = loggerService;
        }

        public async Task<Pagination<MovieResponseDto>> GetAllMoviesAsync(string? search, string? sortBy, bool isDescending, int page, int pageSize)
        {
            try
            {
                _loggerService.Info($"Fetching movies - Page {page}, PageSize {pageSize}, Search: {search}");

                var movies = await _unitOfWork.Movies.GetAllAsync();

                var query = movies.AsQueryable();

                // Filter by search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowerSearch = search.ToLower();
                    query = query.Where(m =>
                        (!string.IsNullOrEmpty(m.Name) && m.Name.ToLower().Contains(lowerSearch)) ||
                        (!string.IsNullOrEmpty(m.Director) && m.Director.ToLower().Contains(lowerSearch)) ||
                        (m.Actors != null && m.Actors.Any(a => a.ToLower().Contains(lowerSearch)))
                    );
                }

                var totalMovies = query.Count();

                // Sort
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
                    Version = m.Version,
                    TrailerUrl = m.TrailerUrl,
                    Genres = m.Genres,
                    Description = m.Description,
                    PosterImage = m.PosterImage
                }).ToList();

                _loggerService.Success($"Retrieved {result.Count} movies on page {page} successfully.");

                return new Pagination<MovieResponseDto>(result, totalMovies, page, pageSize);
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Failed to retrieve movies. Exception: {ex.Message}");
                throw new Exception("An error occurred while retrieving movies. Please try again later.");
            }
        }
        public async Task<List<MovieResponseDto>> GetMovieByNameAsync(string? Name)
        {
            try
            {
                var movies = await _unitOfWork.Movies.GetAllAsync();
                var movieQuery = movies.AsQueryable();

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
                    Version = m.Version,
                    TrailerUrl = m.TrailerUrl,
                    Genres = m.Genres,
                    Description = m.Description,
                    PosterImage = m.PosterImage
                }).ToList();

                return movieList;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"SearchMoviesByNameAsync failed: {ex.Message}");
                throw new Exception("An error occurred while searching for movies. Please try again later.");
            }
        }


        public async Task<MovieResponseDto> AddMovieAsync(MovieRequestDTO movieRequestDto)
        {
            _loggerService.Info($"[AddMovieAsync] Start adding movie {movieRequestDto.Name}");

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
                Actors = movieRequestDto.Actors,
                ActorsUrl = movieRequestDto.ActorsUrl,
                Director = movieRequestDto.Director,
                RunningTime = movieRequestDto.RunningTime,
                Version = movieRequestDto.Version,
                TrailerUrl = movieRequestDto.TrailerUrl,
                Genres = movieRequestDto.Genres,
                Description = movieRequestDto.Description,
                PosterImage = movieRequestDto.PosterImage,
                BackgroundImage = movieRequestDto.BackgroundImage,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _claimsService.GetCurrentUserId // Gán giá trị CreatedBy từ service của Claims
            };

            // Thêm bộ phim vào cơ sở dữ liệu
            await _unitOfWork.Movies.AddAsync(movie);
            
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
                Version = movie.Version,
                TrailerUrl = movie.TrailerUrl,
                Genres = movie.Genres,
                Description = movie.Description,
                PosterImage = movie.PosterImage,
                BackgroundImage = movie.BackgroundImage,
            };

            return responseDto;
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

                if (movieUpdateDto.Actors is { Count: > 0 } &&
                    !movie.Actors?.SequenceEqual(movieUpdateDto.Actors) == true)
                {
                    movie.Actors = new List<string>(movieUpdateDto.Actors);
                    isUpdated = true;
                }

                if (movieUpdateDto.ActorsUrl is { Count: > 0 } &&
                    !movie.ActorsUrl?.SequenceEqual(movieUpdateDto.ActorsUrl) == true)
                {
                    movie.ActorsUrl = new List<string>(movieUpdateDto.ActorsUrl);
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

                if (movieUpdateDto.Version.HasValue && movie.Version != movieUpdateDto.Version)
                {
                    movie.Version = movieUpdateDto.Version.Value;
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

                if (!string.IsNullOrWhiteSpace(movieUpdateDto.PosterImage) && movie.PosterImage != movieUpdateDto.PosterImage)
                {
                    movie.PosterImage = movieUpdateDto.PosterImage;
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
                        Actors = movie.Actors,
                        ActorsUrl = movie.ActorsUrl,
                        Director = movie.Director,
                        RunningTime = movie.RunningTime,
                        Version = movie.Version,
                        TrailerUrl = movie.TrailerUrl,
                        Genres = movie.Genres,
                        Description = movie.Description,
                        PosterImage = movie.PosterImage
                    };
                }

                await _unitOfWork.Movies.Update(movie);
                await _unitOfWork.SaveChangesAsync();

                _loggerService.Success($"[UpdateMovieInfo] Movie info updated successfully for MovieId: {movieId}");
                return new MovieUpdateDto
                {
                    Name = movie.Name,
                    FromDate = movie.FromDate,
                    ToDate = movie.ToDate,
                    Actors = movie.Actors,
                    ActorsUrl = movie.ActorsUrl,
                    Director = movie.Director,
                    RunningTime = movie.RunningTime,
                    Version = movie.Version,
                    TrailerUrl = movie.TrailerUrl,
                    Genres = movie.Genres,
                    Description = movie.Description,
                    PosterImage = movie.PosterImage
                };
            }
            catch (Exception ex)
            {
                _loggerService.Error($"[UpdateMovieInfo] Error updating movie info for MovieId: {movieId}. Exception: {ex.Message}");
                throw;
            }
        }
    }
}

