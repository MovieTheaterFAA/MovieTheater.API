using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;

namespace MovieTheater.Application.Services;

public class BlobService : IBlobService
{
    private readonly string _bucketName = "movietheater-bucket";
    private readonly ILoggerService _loggerService;
    private readonly IMinioClient _minioClient;

    public BlobService(ILoggerService logger)
    {
        _loggerService = logger;

        var endpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT");
        var accessKey = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY");
        var secretKey = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY");

        try
        {
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

    public async Task UploadFileAsync(string fileName, Stream fileStream)
    {
        _loggerService.Info($"Starting file upload: {fileName}");

        try
        {
            var beArgs = new BucketExistsArgs().WithBucket(_bucketName);
            var found = await _minioClient.BucketExistsAsync(beArgs);
            _loggerService.Info($"Checking if bucket '{_bucketName}' exists: {found}");

            if (!found)
            {
                _loggerService.Warn($"Bucket '{_bucketName}' not found. Creating a new one...");
                var mbArgs = new MakeBucketArgs().WithBucket(_bucketName);
                await _minioClient.MakeBucketAsync(mbArgs);
                _loggerService.Success($"Bucket '{_bucketName}' created successfully.");
            }

            var contentType = GetContentType(fileName);
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);

            _loggerService.Info($"Uploading file: {fileName} with content type {contentType}");
            await _minioClient.PutObjectAsync(putObjectArgs);
            _loggerService.Success($"File '{fileName}' uploaded successfully.");
        }
        catch (MinioException minioEx)
        {
            _loggerService.Error($"MinIO Error during upload: {minioEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Unexpected error during file upload: {ex.Message}");
            throw;
        }
    }

    public async Task<string> GetPreviewUrlAsync(string fileName)
    {
        var minioHost = Environment.GetEnvironmentVariable("MINIO_HOST") ??
                        "https://minio.ae-tao-fullstack-api.site";
        _loggerService.Info($"Generating preview URL for file: {fileName}");

        var previewUrl =
            $"{minioHost}/api/v1/buckets/{_bucketName}/objects/download?preview=true&prefix={fileName}&version_id=null";
        _loggerService.Info($"Preview URL generated: {previewUrl}");

        return previewUrl;
    }

    public async Task<string> GetFileUrlAsync(string fileName)
    {
        try
        {
            _loggerService.Info($"Generating presigned URL for file: {fileName}");
            var args = new PresignedGetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName)
                .WithExpiry(7 * 24 * 60 * 60); // URL expires in 7 days

            var fileUrl = await GetPreviewUrlAsync(fileName);
            _loggerService.Success($"Presigned file URL generated: {fileUrl}");
            return fileUrl;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error generating file URL: {ex.Message}");
            return null;
        }
    }

    private string GetContentType(string fileName)
    {
        _loggerService.Info($"Determining content type for file: {fileName}");
        var extension = Path.GetExtension(fileName)?.ToLower();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            ".mp4" => "video/mp4",
            _ => "application/octet-stream"
        };
    }
}