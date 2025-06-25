namespace MovieTheater.Application.Interfaces
{
    public interface IChatbotService
    {
        Task<string> AskMemberAsync(string prompt, string? groupId = null);
    }
}