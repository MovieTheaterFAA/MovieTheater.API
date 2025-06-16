// BlobService.cs
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;

namespace MovieTheater.Application.Services
{
    public class BlobService : IBlobService
    {
        private readonly string _bucketName = "movietheater-bucket";
        private readonly ILoggerService _loggerService;
        private readonly IMinioClient _minioClient;

        public BlobService(ILoggerService logger)
        {
            _loggerService = logger;

            // Get key từ docker-compose
            var endpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT") ?? "103.211.201.162:9000";
            var accessKey = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY");
            var secretKey = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY");

            _loggerService.Info("Initializing BlobService...");
            _loggerService.Info($"Connecting to MinIO at: {endpoint}");

            try
            {
                // Kết nối MinIO không dùng SSL (vì đang dùng IP:port hoặc HTTP)
                _minioClient = new MinioClient()
                    .WithEndpoint(endpoint)
                    .WithCredentials(accessKey, secretKey)
                    .WithSSL(false)
                    .Build();

                _loggerService.Success("MinIO client initialized successfully.");
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Failed to initialize MinIO client: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        ///     Check xem bucket đã tồn tại trên Minio chưa (creates nếu bucket ko tồn tiai5 ).
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task EnsureBucketExistsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var exists = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName), cancellationToken);

                if (!exists)
                {
                    _loggerService.Warn($"Bucket '{_bucketName}' not found. Creating...");
                    await _minioClient.MakeBucketAsync(
                        new MakeBucketArgs().WithBucket(_bucketName),
                        cancellationToken);
                    _loggerService.Success($"Bucket '{_bucketName}' created.");
                }
                else
                {
                    _loggerService.Info($"Bucket '{_bucketName}' already exists.");
                }
            }
            catch (MinioException mex)
            {
                _loggerService.Error($"MinIO error in EnsureBucketExists: {mex.Message}");
                throw;
            }
            catch (OperationCanceledException)
            {
                _loggerService.Warn("Bucket creation cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Unexpected error in EnsureBucketExists: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        ///     Upload file lên MinIO bucket
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileStream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task UploadFileAsync(string fileName, Stream fileStream, string? folder = null, CancellationToken cancellationToken = default)
        {
            await EnsureBucketExistsAsync(cancellationToken);

            string objectName = string.IsNullOrWhiteSpace(folder)
                ? fileName
                : $"{folder.TrimEnd('/')}/{fileName}";

            var contentType = GetContentType(fileName);

            var putArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);

            _loggerService.Info($"Uploading '{objectName}' (type={contentType})...");
            await _minioClient.PutObjectAsync(putArgs, cancellationToken);
            _loggerService.Success($"Upload completed: {objectName}");
        }

        /// <summary>
        ///     Tạo 1 cái preview URL cho file đã upload lên MinIO.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Task<string> GetPreviewUrlAsync(string fileName)
        {
            var minioHost = Environment.GetEnvironmentVariable("MINIO_HOST")
                            ?? "https://minio.fpt-devteam.fun/";

            _loggerService.Info($"Generating preview URL for: {fileName}");
            var previewUrl = $"{minioHost}/api/v1/buckets/{_bucketName}/objects/download?"
                           + $"preview=true&prefix={fileName}&version_id=null";

            _loggerService.Info($"Preview URL: {previewUrl}");
            return Task.FromResult(previewUrl);
        }

        /// <summary>
        ///     Generates a presigned URL for downloading a file from MinIO.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> GetFileUrlAsync(string fileName, CancellationToken cancellationToken = default)
        {
            try
            {
                _loggerService.Info($"Generating presigned URL for: {fileName}");

                var args = new PresignedGetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName)
                    .WithExpiry(7 * 24 * 60 * 60);

                // Kick off the presign task
                var presignTask = _minioClient.PresignedGetObjectAsync(args);

                // Await with cancellation support
                string url = cancellationToken == default
                    ? await presignTask
                    : await presignTask.WaitAsync(cancellationToken);

                _loggerService.Success($"Presigned URL: {url}");
                return url;
            }
            catch (OperationCanceledException)
            {
                _loggerService.Warn($"Presigned URL generation cancelled: {fileName}");
                throw;
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error generating presigned URL: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        ///     Check content type dựa trên file extension.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string GetContentType(string fileName)
        {
            _loggerService.Info($"Determining content type for: {fileName}");
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();

            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".mp4" => "video/mp4",
                _ => "application/octet-stream"     // Fallback nếu định dạng ko xác định
            };
        }
    }
}