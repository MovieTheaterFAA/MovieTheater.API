using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Moq;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;

namespace MovieTheater.UnitTest.Services
{
    public class BlobServiceTests
    {
        private readonly Mock<ILoggerService> _mockLogger;
        private readonly Mock<IMinioClient> _mockMinioClient;
        private readonly BlobService _blobService;

        public BlobServiceTests()
        {
            _mockLogger = new Mock<ILoggerService>();
            _mockMinioClient = new Mock<IMinioClient>();

            // Setup environment variables for testing
            Environment.SetEnvironmentVariable("MINIO_ENDPOINT", "test-endpoint:9000");
            Environment.SetEnvironmentVariable("MINIO_ACCESS_KEY", "test-access-key");
            Environment.SetEnvironmentVariable("MINIO_SECRET_KEY", "test-secret-key");
            Environment.SetEnvironmentVariable("MINIO_HOST", "https://test-minio.com");
        }

        private BlobService CreateBlobServiceWithMockClient()
        {
            // Create a partial mock that allows us to inject the mocked MinioClient
            var service = new Mock<BlobService>(_mockLogger.Object) { CallBase = true };

            // Use reflection to set the private _minioClient field
            var field = typeof(BlobService).GetField("_minioClient",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(service.Object, _mockMinioClient.Object);

            return service.Object;
        }

        [Fact]
        public void Constructor_ShouldInitializeSuccessfully_WithValidEnvironmentVariables()
        {
            // Arrange & Act
            var service = new BlobService(_mockLogger.Object);

            // Assert
            _mockLogger.Verify(x => x.Info("Initializing BlobService..."), Times.Once);
            _mockLogger.Verify(x => x.Info("Connecting to MinIO at: test-endpoint:9000"), Times.Once);
            _mockLogger.Verify(x => x.Success("MinIO client initialized successfully."), Times.Once);
        }

        [Fact]
        public void Constructor_ShouldUseDefaultEndpoint_WhenEnvironmentVariableNotSet()
        {
            // Arrange
            Environment.SetEnvironmentVariable("MINIO_ENDPOINT", null);

            // Act
            var service = new BlobService(_mockLogger.Object);

            // Assert
            _mockLogger.Verify(x => x.Info("Connecting to MinIO at: 103.211.201.162:9000"), Times.Once);
        }

        [Fact]
        public async Task EnsureBucketExistsAsync_ShouldCreateBucket_WhenBucketDoesNotExist()
        {
            // Arrange
            var service = CreateBlobServiceWithMockClient();
            _mockMinioClient.Setup(x => x.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(false);

            // Act
            await service.EnsureBucketExistsAsync();

            // Assert
            _mockMinioClient.Verify(x => x.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockMinioClient.Verify(x => x.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockLogger.Verify(x => x.Warn("Bucket 'movietheater-bucket' not found. Creating..."), Times.Once);
            _mockLogger.Verify(x => x.Success("Bucket 'movietheater-bucket' created."), Times.Once);
        }

        [Fact]
        public async Task EnsureBucketExistsAsync_ShouldNotCreateBucket_WhenBucketExists()
        {
            // Arrange
            var service = CreateBlobServiceWithMockClient();
            _mockMinioClient.Setup(x => x.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);

            // Act
            await service.EnsureBucketExistsAsync();

            // Assert
            _mockMinioClient.Verify(x => x.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockMinioClient.Verify(x => x.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockLogger.Verify(x => x.Info("Bucket 'movietheater-bucket' already exists."), Times.Once);
        }

        [Fact]
        public async Task EnsureBucketExistsAsync_ShouldThrowAndLogError_WhenMinioExceptionOccurs()
        {
            // Arrange
            var service = CreateBlobServiceWithMockClient();
            var minioException = new MinioException("MinIO error");
            _mockMinioClient.Setup(x => x.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
                           .ThrowsAsync(minioException);

            // Act & Assert
            await Assert.ThrowsAsync<MinioException>(() => service.EnsureBucketExistsAsync());

            // The actual error message format includes "MinIO API responded with message=" prefix
            _mockLogger.Verify(x => x.Error("MinIO error in EnsureBucketExists: MinIO API responded with message=MinIO error"), Times.Once);
        }

        [Fact]
        public async Task EnsureBucketExistsAsync_ShouldThrowAndLogWarning_WhenOperationCancelled()
        {
            // Arrange
            var service = CreateBlobServiceWithMockClient();
            var cancellationToken = new CancellationToken(true);
            _mockMinioClient.Setup(x => x.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
                           .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => service.EnsureBucketExistsAsync(cancellationToken));
            _mockLogger.Verify(x => x.Warn("Bucket creation cancelled."), Times.Once);
        }

        [Fact]
        public async Task EnsureBucketExistsAsync_ShouldThrowAndLogError_WhenUnexpectedExceptionOccurs()
        {
            // Arrange
            var service = CreateBlobServiceWithMockClient();
            var exception = new Exception("Unexpected error");
            _mockMinioClient.Setup(x => x.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
                           .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => service.EnsureBucketExistsAsync());
            _mockLogger.Verify(x => x.Error("Unexpected error in EnsureBucketExists: Unexpected error"), Times.Once);
        }

        [Fact]
        public async Task UploadFileAsync_ShouldUploadFile_WithoutFolder()
        {
            // Arrange
            var service = CreateBlobServiceWithMockClient();
            var fileName = "test.jpg";
            var folder = string.Empty;
            var fileStream = new MemoryStream();

            _mockMinioClient.Setup(x => x.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);

            // Act
            await service.UploadFileAsync(fileName, fileStream, folder);

            // Assert
            _mockMinioClient.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockLogger.Verify(x => x.Info("Uploading 'test.jpg' (type=image/jpeg)..."), Times.Once);
            _mockLogger.Verify(x => x.Success("Upload completed: test.jpg"), Times.Once);
        }

        [Fact]
        public async Task UploadFileAsync_ShouldUploadFile_WithFolder()
        {
            // Arrange
            var service = CreateBlobServiceWithMockClient();
            var fileName = "test.png";
            var fileStream = new MemoryStream();
            var folder = "images";

            _mockMinioClient.Setup(x => x.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);

            // Act
            await service.UploadFileAsync(fileName, fileStream, folder);

            // Assert
            _mockMinioClient.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockLogger.Verify(x => x.Info("Uploading 'images/test.png' (type=image/png)..."), Times.Once);
            _mockLogger.Verify(x => x.Success("Upload completed: images/test.png"), Times.Once);
        }

        [Fact]
        public async Task UploadFileAsync_ShouldTrimFolderSlashes()
        {
            // Arrange
            var service = CreateBlobServiceWithMockClient();
            var fileName = "test.pdf";
            var fileStream = new MemoryStream();
            var folder = "documents/";

            _mockMinioClient.Setup(x => x.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);

            // Act
            await service.UploadFileAsync(fileName, fileStream, folder);

            // Assert
            _mockLogger.Verify(x => x.Info("Uploading 'documents/test.pdf' (type=application/pdf)..."), Times.Once);
            _mockLogger.Verify(x => x.Success("Upload completed: documents/test.pdf"), Times.Once);
        }

        [Fact]
        public async Task GetPreviewUrlAsync_ShouldReturnCorrectUrl()
        {
            // Arrange
            var service = new BlobService(_mockLogger.Object);
            var fileName = "test.jpg";

            // Act
            var result = await service.GetPreviewUrlAsync(fileName);

            // Assert
            var expectedUrl = "https://test-minio.com/api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=test.jpg&version_id=null";
            Assert.Equal(expectedUrl, result);
            _mockLogger.Verify(x => x.Info("Generating preview URL for: test.jpg"), Times.Once);
            _mockLogger.Verify(x => x.Info($"Preview URL: {expectedUrl}"), Times.Once);
        }

        [Fact]
        public async Task GetPreviewUrlAsync_ShouldUseDefaultHost_WhenEnvironmentVariableNotSet()
        {
            // Arrange
            Environment.SetEnvironmentVariable("MINIO_HOST", null);
            var service = new BlobService(_mockLogger.Object);
            var fileName = "test.jpg";

            // Act
            var result = await service.GetPreviewUrlAsync(fileName);

            // Assert
            var expectedUrl = "https://minio.fpt-devteam.fun//api/v1/buckets/movietheater-bucket/objects/download?preview=true&prefix=test.jpg&version_id=null";
            Assert.Equal(expectedUrl, result);
        }

        [Fact]
        public async Task GetFileUrlAsync_ShouldReturnPresignedUrl_Successfully()
        {
            // Arrange
            var service = CreateBlobServiceWithMockClient();
            var fileName = "test.mp4";
            var originalUrl = "http://103.211.201.162:9000/movietheater-bucket/test.mp4?signature=123";
            var expectedUrl = "https://test-minio.com/movietheater-bucket/test.mp4?signature=123";

            _mockMinioClient.Setup(x => x.PresignedGetObjectAsync(It.IsAny<PresignedGetObjectArgs>()))
                           .ReturnsAsync(originalUrl);

            // Act
            var result = await service.GetFileUrlAsync(fileName);

            // Assert
            Assert.Equal(expectedUrl, result);
            _mockLogger.Verify(x => x.Info("Generating presigned URL for: test.mp4"), Times.Once);
            _mockLogger.Verify(x => x.Success($"Presigned URL: {expectedUrl}"), Times.Once);
        }

        [Fact]
        public async Task GetFileUrlAsync_ShouldReplaceHttpsUrl_Successfully()
        {
            // Arrange
            var service = CreateBlobServiceWithMockClient();
            var fileName = "test.gif";
            var originalUrl = "https://103.211.201.162:9000/movietheater-bucket/test.gif?signature=456";
            var expectedUrl = "https://test-minio.com/movietheater-bucket/test.gif?signature=456";

            _mockMinioClient.Setup(x => x.PresignedGetObjectAsync(It.IsAny<PresignedGetObjectArgs>()))
                           .ReturnsAsync(originalUrl);

            // Act
            var result = await service.GetFileUrlAsync(fileName);

            // Assert
            Assert.Equal(expectedUrl, result);
        }

        [Fact]
        public async Task GetFileUrlAsync_ShouldUseDefaultHost_WhenEnvironmentVariableNotSet()
        {
            // Arrange
            Environment.SetEnvironmentVariable("MINIO_HOST", null);
            var service = CreateBlobServiceWithMockClient();
            var fileName = "test.jpg";
            var originalUrl = "http://103.211.201.162:9000/movietheater-bucket/test.jpg?signature=789";
            var expectedUrl = "https://minio.fpt-devteam.fun/movietheater-bucket/test.jpg?signature=789";

            _mockMinioClient.Setup(x => x.PresignedGetObjectAsync(It.IsAny<PresignedGetObjectArgs>()))
                           .ReturnsAsync(originalUrl);

            // Act
            var result = await service.GetFileUrlAsync(fileName);

            // Assert
            Assert.Equal(expectedUrl, result);
        }

        [Fact]
        public async Task GetFileUrlAsync_ShouldReturnNull_WhenExceptionOccurs()
        {
            // Arrange
            var service = CreateBlobServiceWithMockClient();
            var fileName = "test.jpg";
            var exception = new Exception("Test exception");

            _mockMinioClient.Setup(x => x.PresignedGetObjectAsync(It.IsAny<PresignedGetObjectArgs>()))
                           .ThrowsAsync(exception);

            // Act
            var result = await service.GetFileUrlAsync(fileName);

            // Assert
            Assert.Null(result);
            _mockLogger.Verify(x => x.Error("Error generating presigned URL: Test exception"), Times.Once);
        }

        [Fact]
        public async Task GetFileUrlAsync_ShouldThrowAndLogWarning_WhenOperationCancelled()
        {
            // Arrange
            var service = CreateBlobServiceWithMockClient();
            var fileName = "test.jpg";
            var cancellationToken = new CancellationToken(true);

            _mockMinioClient.Setup(x => x.PresignedGetObjectAsync(It.IsAny<PresignedGetObjectArgs>()))
                           .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => service.GetFileUrlAsync(fileName, cancellationToken));
            _mockLogger.Verify(x => x.Warn("Presigned URL generation cancelled: test.jpg"), Times.Once);
        }

        [Theory]
        [InlineData("test.jpg", "image/jpeg")]
        [InlineData("test.jpeg", "image/jpeg")]
        [InlineData("test.png", "image/png")]
        [InlineData("test.gif", "image/gif")]
        [InlineData("test.pdf", "application/pdf")]
        [InlineData("test.mp4", "video/mp4")]
        [InlineData("test.txt", "application/octet-stream")]
        [InlineData("test", "application/octet-stream")]
        [InlineData("test.unknown", "application/octet-stream")]
        public async Task UploadFileAsync_ShouldUseCorrectContentType(string fileName, string expectedContentType)
        {
            // Arrange
            var service = CreateBlobServiceWithMockClient();
            var fileStream = new MemoryStream();
            var folder = String.Empty;

            _mockMinioClient.Setup(x => x.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);

            // Act
            await service.UploadFileAsync(fileName, fileStream, folder);

            // Assert
            _mockLogger.Verify(x => x.Info($"Uploading '{fileName}' (type={expectedContentType})..."), Times.Once);
        }

        [Fact]
        public async Task GetFileUrlAsync_ShouldHandleCancellationTokenCorrectly()
        {
            // Arrange
            var service = CreateBlobServiceWithMockClient();
            var fileName = "test.jpg";
            var cancellationToken = CancellationToken.None;
            var expectedUrl = "https://test-minio.com/movietheater-bucket/test.jpg?signature=123";

            _mockMinioClient.Setup(x => x.PresignedGetObjectAsync(It.IsAny<PresignedGetObjectArgs>()))
                           .ReturnsAsync("http://103.211.201.162:9000/movietheater-bucket/test.jpg?signature=123");

            // Act
            var result = await service.GetFileUrlAsync(fileName, cancellationToken);

            // Assert
            Assert.Equal(expectedUrl, result);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Clean up environment variables
                Environment.SetEnvironmentVariable("MINIO_ENDPOINT", null);
                Environment.SetEnvironmentVariable("MINIO_ACCESS_KEY", null);
                Environment.SetEnvironmentVariable("MINIO_SECRET_KEY", null);
                Environment.SetEnvironmentVariable("MINIO_HOST", null);
            }
        }
    }
}