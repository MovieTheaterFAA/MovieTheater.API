namespace MovieTheater.Application.Interfaces;

public interface IBlobService
{
    Task EnsureBucketExistsAsync(CancellationToken cancellationToken = default);

    Task UploadFileAsync(
        string fileName,
        Stream fileStream,
        string folder,
        CancellationToken cancellationToken = default);

    Task<string> GetPreviewUrlAsync(string fileName);

    Task<string> GetFileUrlAsync(string fileName, CancellationToken cancellationToken = default);
}