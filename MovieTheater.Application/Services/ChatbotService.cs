using System.Text.RegularExpressions;
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

        /// <summary>
        /// Flexible method for member AI analysis and Q&A.
        /// Detects intent from prompt and generates the appropriate analysis or answer.
        /// </summary>
        /// <param name="prompt">User's question or request.</param>
        public async Task<string> AskMemberAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("Prompt is required.");

            // Lowercase for easier matching
            var lowerPrompt = prompt.ToLowerInvariant();

            // Detect "most booked" intent
            if (Regex.IsMatch(lowerPrompt, @"(most\s+(booked|popular|reserved|frequent(ed)?|chosen|picked))|(best\s+selling)", RegexOptions.IgnoreCase))
            {
                int top = ExtractTopNumber(prompt) ?? 5;
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

                var generatedPrompt = $"""
                    Here are the top {top} most booked movies in our cinema system:
                    {formatted}

                    Please provide a concise analysis for members:
                    - What trends do you notice?
                    - Which genres or directors are most popular?
                    - Any recommendations for members?
                    Answer in clear, friendly English, using bullet points if possible.
                """;

                return await _geminiService.GenerateResponseAsync(generatedPrompt);
            }

            // Detect "top rating" intent
            if (Regex.IsMatch(lowerPrompt, @"(top\s+(rating|rated|score|scored|reviewed|best))|(highest\s+rated)", RegexOptions.IgnoreCase))
            {
                int top = ExtractTopNumber(prompt) ?? 5;
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

                var generatedPrompt = $"""
                    Here are the top {top} highest rated movies in our cinema system:
                    {formatted}

                    Please provide a concise analysis for members:
                    - What makes these movies highly rated?
                    - Any notable patterns in genres or directors?
                    - Suggestions for members interested in top-rated films?
                    Answer in clear, friendly English, using bullet points if possible.
                """;

                return await _geminiService.GenerateResponseAsync(generatedPrompt);
            }

            // Default: treat as a general prompt
            return await _geminiService.GenerateResponseAsync(prompt);
        }

        /// <summary>
        /// Extracts a number (e.g. "top 3", "top 10") from the prompt, or returns null if not found.
        /// </summary>
        private int? ExtractTopNumber(string prompt)
        {
            var match = Regex.Match(prompt, @"top\s*(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int top))
            {
                return top;
            }
            return null;
        }
    }
}