using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.ThirdParty;
using MovieTheater.Infrastructure.Hubs;

namespace MovieTheater.Application.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly IDataAnalyzerService _analyzerService;
        private readonly IGeminiService _geminiService;
        private readonly IHubContext<ChatbotHub> _chatbotHub;

        public ChatbotService(IDataAnalyzerService analyzerService, IGeminiService geminiService, IHubContext<ChatbotHub> chatbotHub)
        {
            _analyzerService = analyzerService;
            _geminiService = geminiService;
            _chatbotHub = chatbotHub;
        }

        public async Task<string> AskMemberAsync(string prompt, string? groupId = null)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("Prompt is required.");

            string response;

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

                response = await _geminiService.GenerateResponseAsync(generatedPrompt);
            }
            // Detect "top rating" intent
            else if (Regex.IsMatch(lowerPrompt, @"(top\s+(rating|rated|score|scored|reviewed|best))|(highest\s+rated)", RegexOptions.IgnoreCase))
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

                response = await _geminiService.GenerateResponseAsync(generatedPrompt);
            }
            else
            {
                response = await _geminiService.GenerateResponseAsync(prompt);
            }

            // Broadcast if groupId is provided
            if (!string.IsNullOrWhiteSpace(groupId))
            {
                await BroadcastChatbotResponseAsync(groupId, response);
            }

            return response;
        }

        public async Task BroadcastChatbotResponseAsync(string groupId, string response)
        {
            await _chatbotHub.Clients.Group(groupId)
                .SendAsync("ReceiveChatbotResponse", new
                {
                    GroupId = groupId,
                    Response = response,
                    Timestamp = DateTime.UtcNow
                });
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