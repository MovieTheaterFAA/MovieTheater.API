namespace MovieTheater.Application.Interfaces
{
    public interface IChatbotService
    {
        Task<string> FreestyleAskAsync(string prompt, string? groupId = null);
    }
}