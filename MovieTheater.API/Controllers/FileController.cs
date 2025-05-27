using Microsoft.AspNetCore.Mvc;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Utils;
using MovieTheater.Domain.DTOs.BlobDTOs;

namespace MovieTheater.API.Controllers
{
    [Route("api/files")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IBlobService _blobService;
        private readonly ILogger<FileController> _loggerService;

        public FileController(IBlobService blobService, ILogger<FileController> logger)
        {
            _blobService = blobService;
            _loggerService = logger;
        }

        [HttpPost("upload-file")]
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
    }
}