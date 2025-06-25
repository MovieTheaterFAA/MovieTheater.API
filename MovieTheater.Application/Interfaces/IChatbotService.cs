namespace MovieTheater.Application.Interfaces
{
    public interface IChatbotService
    {
        Task<string> AnalyzeMostBookedMoviesForMemberAsync(int top);

        Task<string> AnalyzeTopRatingMoviesForMemberAsync(int top);

        Task<string> AskMemberAsync(string prompt);
    }
}