using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using MovieTheater.Application.Hubs;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.ThirdParty;

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

        public async Task<string> FreestyleAskAsync(string prompt, string? groupId = null)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("Prompt is required.");

            // Fetch all movies and food/drinks
            var movies = await _analyzerService.GetAllMoviesAsync();
            var foods = await _analyzerService.GetAllFoodAndDrinksAsync();
            var promotions = await _analyzerService.GetAllPromotionsAsync();
            var cinemaRooms = await _analyzerService.GetAllCinemaRoomsAsync();
            var seatTypes = await _analyzerService.GetAllSeatTypesAsync();

            // Format movie data
            var movieContext = string.Join("\n", movies.Select(m =>
                $"""
                - Name: {m.Name}
                Director: {m.Director}
                Rating: {m.Rating}
                Status: {m.Status}
                Genres: {string.Join(", ", m.Genres ?? new List<string>())}
                Cast: {string.Join(", ", m.Actors ?? new List<string>())}
                From: {m.FromDate:yyyy-MM-dd} To: {m.ToDate:yyyy-MM-dd}
                RunningTime: {m.RunningTime} minutes
                Description: {m.Description}
                """
            ));

            // Format food and drink data
            var foodContext = string.Join("\n", foods.Select(f =>
                $"""
                - Name: {f.Name}
                Type: {f.Type}
                Price: {f.Price}
                Description: {f.Description}
                """
            ));

            // Format promotions with their parent event
            var promotionContext = string.Join("\n", promotions.Select(p =>
                $"""
                - Title: {p.Title}
                Discount: {p.DiscountValue}
                Detail: {p.Detail}
                Event: {(p.Event != null ? p.Event.Name : "N/A")}
                EventTime: {(p.Event != null ? $"{p.Event.StartTime:yyyy-MM-dd} to {p.Event.EndTime:yyyy-MM-dd}" : "N/A")}
                """
            ));

            // Format cinema room data
            var cinemaRoomContext = string.Join("\n", cinemaRooms.Select(r =>
                $"""
                - Name: {r.Name}
                Type: {r.Type}
                """
            ));

            // Format seat types
            var seatTypeContext = string.Join("\n", seatTypes.Select(st =>
                $"- {st} ({(int)st})"
            ));

            // Combine context and prompt
            var contextPrompt = $"""
            [Movie Database Context]
            {movieContext}

            [Food & Drink Menu]
            {foodContext}

            [Promotions & Events]
            {promotionContext}

            [Cinema Rooms]
            {cinemaRoomContext}

            [Seat Types]
            {seatTypeContext}

            [User Question]
            {prompt}
            """;

            var response = await _geminiService.GenerateResponseAsync(contextPrompt);
            return response;
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