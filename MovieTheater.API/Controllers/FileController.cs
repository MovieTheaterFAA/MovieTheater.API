using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain;
using MovieTheater.Domain.DTOs.BlobDTOs;

namespace MovieTheater.API.Controllers
{
    [Route("api/files")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IBlobService _blobService;
        private readonly ILogger<FileController> _loggerService;
        private readonly MovieTheaterDbContext _dbContext;

        public FileController(IBlobService blobService, ILogger<FileController> logger, MovieTheaterDbContext dbContext)
        {
            _blobService = blobService;
            _loggerService = logger;
            _dbContext = dbContext;
        }

        [HttpPost("test-upload-file")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResult<string>), 200)]
        [ProducesResponseType(typeof(ApiResult<string>), 400)]
        public async Task<IActionResult> Upload([FromForm] FileUploadRequestDto request)
        {
            var file = request.File;
            if (file == null || file.Length == 0)
                return BadRequest(ApiResult<string>.Failure("400", "No file provided."));

            // Check cancel token để stop request khi user abort upload
            CancellationToken ct = HttpContext.RequestAborted;

            try
            {
                // 1) Upload to MinIO - tùy setup folder, tự truyền param tên folder
                using var stream = file.OpenReadStream();
                await _blobService.UploadFileAsync(file.FileName, stream, "tests", ct);

                // 2) Generate a presigned URL - follow AWS S3 standard
                var url = await _blobService.GetFileUrlAsync(file.FileName, ct);

                return Ok(ApiResult<string>.Success(url!, "200", "File uploaded successfully."));
            }
            catch (OperationCanceledException)
            {
                _loggerService.LogWarning("Upload was cancelled by the client.");
                return BadRequest(ApiResult<string>.Failure("499", "Upload was cancelled."));
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Error uploading file.");
                return StatusCode(500, ApiResult<string>.Failure("500", "An error occurred while uploading the file."));
            }
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
                var presignedUrl = await _blobService.GetFileUrlAsync(objectName, ct);
                if (presignedUrl == null)
                {
                    _loggerService.LogError($"Failed to generate presigned URL for object '{objectName}'.");
                    return StatusCode(500, ApiResult<string>.Failure("500", "Could not generate file URL."));
                }

                // 7) Fetch the user from the database, update AvatarUrl, and save
                var user = await _dbContext.Users.FindAsync(new object[] { userId }, ct);
                if (user == null)
                {
                    // User not found—even though they were authenticated
                    return NotFound(ApiResult<string>.Failure("404", "User not found."));
                }

                user.AvatarUrl = presignedUrl;
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync(ct);

                // 8) Return the presigned URL so the client can immediately display/store it
                return Ok(ApiResult<string>.Success(presignedUrl, "200", "Avatar uploaded successfully."));
            }
            catch (OperationCanceledException)
            {
                _loggerService.LogWarning("Upload was cancelled by the client.");
                return BadRequest(ApiResult<string>.Failure("499", "Upload was cancelled."));
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex, "Error uploading avatar.");
                return StatusCode(500, ApiResult<string>.Failure("500", "An unexpected error occurred while uploading the avatar."));
            }
        }
    }
}