namespace MovieTheater.Application.Interfaces;

public interface IBlobService
{
    Task UploadFileAsync(string fileName, Stream fileStream);

    Task<string> GetPreviewUrlAsync(string fileName);

    Task<string> GetFileUrlAsync(string fileName);
}