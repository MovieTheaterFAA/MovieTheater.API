namespace MovieTheater.Application.Interfaces.ThirdParty
{
    public interface IGeminiService
    {
        Task<string> GenerateResponseAsync(string prompt);
    }
}