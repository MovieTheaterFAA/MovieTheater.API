using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain;
using MovieTheater.Domain.DTOs.BlobDTOs;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.API.Controllers
{
    [Route("api/files")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IBlobService _blobService;
        private readonly ILogger<FileController> _logger;
        private readonly MovieTheaterDbContext _dbContext;
        private readonly IRedisService _redisService;

        public FileController(IBlobService blobService, ILogger<FileController> logger, MovieTheaterDbContext dbContext, IRedisService redisService)
        {
            _blobService = blobService;
            _logger = logger;
            _dbContext = dbContext;
            _redisService = redisService;
        }

        [HttpPost("upload-avatar")]
        [Consumes("multipart/form-data")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResult<string>), 200)]
        [ProducesResponseType(typeof(ApiResult<string>), 400)]
        [ProducesResponseType(typeof(ApiResult<string>), 500)]
        public async Task<IActionResult> UploadAvatar([FromForm] FileUploadRequestDto request)
        {
            // 1) Basic file validation
            var file = request.File;
            if (file == null || file.Length == 0)
                return BadRequest(ApiResult<string>.Failure("400", "No file provided."));

            // 2) Resolve the current user ID from the JWT/claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                // The client is authenticated, but we don't have a valid user ID claim
                return Unauthorized(ApiResult<string>.Failure("401", "Invalid user context."));
            }

            // 3) Use cancellation token so we can abort if client disconnects
            CancellationToken ct = HttpContext.RequestAborted;

            try
            {
                // 4) Split the file name to sanitize it and create a folder structure for each user
                var sanitizedFolder = $"avatars/{userId}";
                using var stream = file.OpenReadStream();
                var objectName = $"{sanitizedFolder}/{file.FileName}";

                // 5) Upload to MinIO
                await _blobService.UploadFileAsync(file.FileName, stream, sanitizedFolder, ct);

                // 6) Generate a presigned URL that can be stored in User.AvatarUrl - also check if the URL is valid
                var previewUrl = await _blobService.GetPreviewUrlAsync(objectName);
                if (previewUrl == null)
                {
                    _logger.LogError($"Failed to generate presigned URL for object '{objectName}'.");
                    return StatusCode(500, ApiResult<string>.Failure("500", "Could not generate file URL."));
                }

                // 7) Fetch the user from the database, update AvatarUrl, and save
                var user = await _dbContext.Users.FindAsync(new object[] { userId }, ct);
                if (user == null)
                {
                    // User not found—even though they were authenticated
                    return NotFound(ApiResult<string>.Failure("404", "User not found."));
                }

                user.AvatarUrl = previewUrl;
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync(ct);
                await _redisService.RemoveAsync($"user:detail:{userId}");

                // 8) Return the presigned URL so the client can immediately display/store it
                return Ok(ApiResult<string>.Success(previewUrl, "200", "Avatar uploaded successfully."));
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Upload was cancelled by the client.");
                return BadRequest(ApiResult<string>.Failure("499", "Upload was cancelled."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar.");
                return StatusCode(500, ApiResult<string>.Failure("500", "An unexpected error occurred while uploading the avatar."));
            }
        }

        [HttpPost("upload-event-img/{id}")]
        [Consumes("multipart/form-data")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResult<string>), 200)]
        [ProducesResponseType(typeof(ApiResult<string>), 400)]
        [ProducesResponseType(typeof(ApiResult<string>), 404)]
        [ProducesResponseType(typeof(ApiResult<string>), 500)]
        public async Task<IActionResult> UploadEvent(Guid id, [FromForm] FileUploadRequestDto request)
        {
            var file = request.File;
            if (file == null || file.Length == 0)
                return BadRequest(ApiResult<string>.Failure("400", "No file provided."));

            CancellationToken ct = HttpContext.RequestAborted;

            try
            {
                var eventEntity = await _dbContext.Events.FindAsync(new object[] { id }, ct);
                if (eventEntity == null)
                {
                    return NotFound(ApiResult<string>.Failure("404", "Event not found."));
                }

                var sanitizedFolder = $"event-images/{id}";
                using var stream = file.OpenReadStream();
                var objectName = $"{sanitizedFolder}/{file.FileName}";

                await _blobService.UploadFileAsync(file.FileName, stream, sanitizedFolder, ct);

                var previewUrl = await _blobService.GetPreviewUrlAsync(objectName);
                if (previewUrl == null)
                {
                    _logger.LogError($"Failed to generate presigned URL for object '{objectName}'.");
                    return StatusCode(500, ApiResult<string>.Failure("500", "Could not generate file URL."));
                }

                eventEntity.Image = previewUrl;
                _dbContext.Events.Update(eventEntity);
                await _dbContext.SaveChangesAsync(ct);
                await _redisService.RemoveByPatternAsync("event:list:");

                return Ok(ApiResult<string>.Success(previewUrl, "200", "Event image uploaded successfully."));
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Upload was cancelled by the client.");
                return BadRequest(ApiResult<string>.Failure("499", "Upload was cancelled."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading event image.");
                return StatusCode(500, ApiResult<string>.Failure("500", "An unexpected error occurred while uploading the event image."));
            }
        }

        [HttpPost("upload-food-img/{id}")]
        [Consumes("multipart/form-data")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResult<string>), 200)]
        [ProducesResponseType(typeof(ApiResult<string>), 400)]
        [ProducesResponseType(typeof(ApiResult<string>), 404)]
        [ProducesResponseType(typeof(ApiResult<string>), 500)]
        public async Task<IActionResult> UploadFoodImage(Guid id, [FromForm] FileUploadRequestDto request)
        {
            var file = request.File;
            if (file == null || file.Length == 0)
                return BadRequest(ApiResult<string>.Failure("400", "No file provided."));

            CancellationToken ct = HttpContext.RequestAborted;

            try
            {
                // 1) Kiểm tra tồn tại món ăn/thức uống
                var food = await _dbContext.FoodAndDrinks.FindAsync(new object[] { id }, ct);
                if (food == null)
                {
                    return NotFound(ApiResult<string>.Failure("404", "Food or drink item not found."));
                }

                // 2) Tạo đường dẫn lưu trữ ảnh theo ID món
                var sanitizedFolder = $"food/drink/combo-images/{id}";
                using var stream = file.OpenReadStream();
                var objectName = $"{sanitizedFolder}/{file.FileName}";

                // 3) Upload lên MinIO
                await _blobService.UploadFileAsync(file.FileName, stream, sanitizedFolder, ct);

                // 4) Tạo URL ảnh
                var previewUrl = await _blobService.GetPreviewUrlAsync(objectName);
                if (previewUrl == null)
                {
                    _logger.LogError($"Failed to generate preview URL for object '{objectName}'.");
                    return StatusCode(500, ApiResult<string>.Failure("500", "Could not generate file URL."));
                }

                // 5) Cập nhật image URL trong database
                food.ImageUrl = previewUrl;
                _dbContext.FoodAndDrinks.Update(food);
                await _dbContext.SaveChangesAsync(ct);
                await _redisService.RemoveByPatternAsync("fooddrink:list:");

                return Ok(ApiResult<string>.Success(previewUrl, "200", "Food and drink image uploaded successfully."));
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Upload was cancelled by the client.");
                return BadRequest(ApiResult<string>.Failure("499", "Upload was cancelled."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading food image.");
                return StatusCode(500, ApiResult<string>.Failure("500", "An unexpected error occurred while uploading the food image."));
            }
        }

        [HttpPost("upload-movie-poster/{id}")]
        [Consumes("multipart/form-data")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResult<string>), 200)]
        [ProducesResponseType(typeof(ApiResult<string>), 400)]
        [ProducesResponseType(typeof(ApiResult<string>), 404)]
        [ProducesResponseType(typeof(ApiResult<string>), 500)]
        public async Task<IActionResult> UploadMoviePoster(Guid id, [FromForm] FileUploadRequestDto request)
        {
            var file = request.File;
            if (file == null || file.Length == 0)
                return BadRequest(ApiResult<string>.Failure("400", "No file provided."));

            CancellationToken ct = HttpContext.RequestAborted;

            try
            {
                var movie = await _dbContext.Movies.FindAsync(new object[] { id }, ct);
                if (movie == null)
                    return NotFound(ApiResult<string>.Failure("404", "Movie not found."));

                var folder = $"movies/{id}/poster";
                var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                var objectName = $"{folder}/{uniqueFileName}";

                using var stream = file.OpenReadStream();
                await _blobService.UploadFileAsync(uniqueFileName, stream, folder, ct);

                var previewUrl = await _blobService.GetPreviewUrlAsync(objectName);
                if (previewUrl == null)
                {
                    _logger.LogError($"Failed to generate URL for poster '{objectName}'.");
                    return StatusCode(500, ApiResult<string>.Failure("500", "Could not generate file URL."));
                }

                movie.PosterImage = previewUrl;
                _dbContext.Movies.Update(movie);
                await _dbContext.SaveChangesAsync(ct);
                await _redisService.RemoveByPatternAsync("movie:list:");
                await _redisService.RemoveAsync($"movie:detail:{id}");

                return Ok(ApiResult<string>.Success(previewUrl, "200", "Movie poster uploaded successfully."));
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Upload was cancelled by the client.");
                return BadRequest(ApiResult<string>.Failure("499", "Upload was cancelled."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading movie poster.");
                return StatusCode(500, ApiResult<string>.Failure("500", "An error occurred while uploading poster."));
            }
        }

        [HttpPost("upload-movie-background/{id}")]
        [Consumes("multipart/form-data")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResult<string>), 200)]
        [ProducesResponseType(typeof(ApiResult<string>), 400)]
        [ProducesResponseType(typeof(ApiResult<string>), 404)]
        [ProducesResponseType(typeof(ApiResult<string>), 500)]
        public async Task<IActionResult> UploadMovieBackground(Guid id, [FromForm] FileUploadRequestDto request)
        {
            var file = request.File;
            if (file == null || file.Length == 0)
                return BadRequest(ApiResult<string>.Failure("400", "No file provided."));

            CancellationToken ct = HttpContext.RequestAborted;

            try
            {
                var movie = await _dbContext.Movies.FindAsync(new object[] { id }, ct);
                if (movie == null)
                    return NotFound(ApiResult<string>.Failure("404", "Movie not found."));

                var folder = $"movies/{id}/background";
                var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                var objectName = $"{folder}/{uniqueFileName}";

                using var stream = file.OpenReadStream();
                await _blobService.UploadFileAsync(uniqueFileName, stream, folder, ct);

                var previewUrl = await _blobService.GetPreviewUrlAsync(objectName);
                if (previewUrl == null)
                {
                    _logger.LogError($"Failed to generate URL for background '{objectName}'.");
                    return StatusCode(500, ApiResult<string>.Failure("500", "Could not generate file URL."));
                }

                movie.BackgroundImage = previewUrl;
                _dbContext.Movies.Update(movie);
                await _dbContext.SaveChangesAsync(ct);
                await _redisService.RemoveByPatternAsync("movie:list:");
                await _redisService.RemoveAsync($"movie:detail:{id}");

                return Ok(ApiResult<string>.Success(previewUrl, "200", "Movie background uploaded successfully."));
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Upload was cancelled by the client.");
                return BadRequest(ApiResult<string>.Failure("499", "Upload was cancelled."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading movie background.");
                return StatusCode(500, ApiResult<string>.Failure("500", "An error occurred while uploading background."));
            }
        }

        [HttpPost("upload-cast-img/{id}")]
        [Consumes("multipart/form-data")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResult<string>), 200)]
        [ProducesResponseType(typeof(ApiResult<string>), 400)]
        [ProducesResponseType(typeof(ApiResult<string>), 404)]
        [ProducesResponseType(typeof(ApiResult<string>), 500)]
        public async Task<IActionResult> UploadCastImage(
            Guid id,
            [FromForm] MovieCastUploadDto request)
        {
            var file = request.File;
            var actorName = request.ActorName?.Trim();

            if (file == null || file.Length == 0)
                return BadRequest(ApiResult<string>.Failure("400", "No file provided."));

            if (string.IsNullOrWhiteSpace(actorName) ||
                actorName.ToLowerInvariant() == "string" ||
                actorName.Length < 2)
                return BadRequest(ApiResult<string>.Failure("400", "Invalid actor name."));

            CancellationToken ct = HttpContext.RequestAborted;

            try
            {
                var movie = await _dbContext.Movies.FindAsync(new object[] { id }, ct);
                if (movie == null)
                    return NotFound(ApiResult<string>.Failure("404", "Movie not found."));

                // Prevent duplicate actor names
                movie.Actors ??= new List<string>();
                if (movie.Actors.Any(a => a.Equals(actorName, StringComparison.OrdinalIgnoreCase)))
                    return BadRequest(ApiResult<string>.Failure("400", "Actor already exists."));

                var safeActor = actorName.Replace(" ", "_").ToLowerInvariant();
                var folder = $"movies/{id}/cast/{safeActor}";
                var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                var objectName = $"{folder}/{uniqueFileName}";

                using var stream = file.OpenReadStream();
                await _blobService.UploadFileAsync(uniqueFileName, stream, folder, ct);

                var previewUrl = await _blobService.GetPreviewUrlAsync(objectName);
                if (previewUrl == null)
                {
                    _logger.LogError($"[UploadMovieCastImage] Failed to generate URL for: {objectName}");
                    return StatusCode(500, ApiResult<string>.Failure("500", "Could not generate file URL."));
                }

                movie.Actors.Add(actorName);
                movie.ActorsUrl ??= new List<string>();
                movie.ActorsUrl.Add(previewUrl);

                _dbContext.Movies.Update(movie);
                await _dbContext.SaveChangesAsync(ct);
                await _redisService.RemoveByPatternAsync("movie:list:");
                await _redisService.RemoveAsync($"movie:detail:{id}");

                return Ok(ApiResult<string>.Success(previewUrl, "200", $"Cast image for '{actorName}' uploaded successfully."));
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[UploadMovieCastImage] Upload cancelled by client.");
                return BadRequest(ApiResult<string>.Failure("499", "Upload was cancelled."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UploadMovieCastImage] Unexpected error.");
                return StatusCode(500, ApiResult<string>.Failure("500", "An unexpected error occurred."));
            }
        }
    }
}