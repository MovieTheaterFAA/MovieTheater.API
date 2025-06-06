using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Domain.DTOs.MovieDTOs;
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

        public async Task<MovieUpdateDto> UpdateMovieInfo(Guid movieId, MovieUpdateDto movieUpdateDto)
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
