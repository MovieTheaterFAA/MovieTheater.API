using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.ThirdParty;

namespace MovieTheater.Application.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly IDataAnalyzerService _analyzerService;
        private readonly IGeminiService _geminiService;

        public ChatbotService(IDataAnalyzerService analyzerService, IGeminiService geminiService)
        {
            _analyzerService = analyzerService;
            _geminiService = geminiService;
        }

        public async Task<string> AnalyzeMostBookedMoviesForMemberAsync(int top)
        {
            var movies = await _analyzerService.GetMostBookedMoviesAsync(top);

            var formatted = string.Join("\n", movies.Select(m =>
                $"""
                - Name: {m.Name}
                  Director: {m.Director}
                  Rating: {m.Rating}
                  Status: {m.Status}
                  Genres: {string.Join(", ", m.Genres ?? new List<string>())}
                  From: {m.FromDate:yyyy-MM-dd} To: {m.ToDate:yyyy-MM-dd}
                """
            ));

            var prompt = $"""
                Here are the top {top} most booked movies in our cinema system:
                {formatted}

                Please provide a concise analysis for members:
                - What trends do you notice?
                - Which genres or directors are most popular?
                - Any recommendations for members?
                Answer in clear, friendly English, using bullet points if possible.
            """;

            return await AskMemberAsync(prompt);
        }

        public async Task<string> AnalyzeTopRatingMoviesForMemberAsync(int top)
        {
            var movies = await _analyzerService.GetTopRatingMoviesAsync(top);

            var formatted = string.Join("\n", movies.Select(m =>
                $"""
                - Name: {m.Name}
                  Director: {m.Director}
                  Rating: {m.Rating}
                  Status: {m.Status}
                  Genres: {string.Join(", ", m.Genres ?? new List<string>())}
                  From: {m.FromDate:yyyy-MM-dd} To: {m.ToDate:yyyy-MM-dd}
                """
            ));

            var prompt = $"""
                Here are the top {top} highest rated movies in our cinema system:
                {formatted}

                Please provide a concise analysis for members:
                - What makes these movies highly rated?
                - Any notable patterns in genres or directors?
                - Suggestions for members interested in top-rated films?
                Answer in clear, friendly English, using bullet points if possible.
            """;

            return await AskMemberAsync(prompt);
        }

        public async Task<string> AskMemberAsync(string prompt)
        {
            return await _geminiService.GenerateResponseAsync(prompt);
        }
    }
}