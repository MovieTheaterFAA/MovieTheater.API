using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.ThirdParty;

namespace MovieTheater.Application.Services.ThirdParty
{
    public class GeminiService : IGeminiService
    {
        private readonly string _apiKey;
        private readonly ICacheService _cache;
        private readonly HttpClient _httpClient;

        public GeminiService(IHttpClientFactory httpClientFactory, IConfiguration config, ICacheService cache)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = config["Gemini:ApiKey"]
                      ?? Environment.GetEnvironmentVariable("   ")
                      ?? throw new Exception("Gemini API key not configured.");
            _cache = cache;
        }

        public async Task<string> GenerateResponseAsync(string userPrompt)
        {
            var fullPrompt = $"{GeminiContext.SystemPrompt}\n\n{userPrompt}";
            var cacheKey = $"gemini:response:{fullPrompt.GetHashCode()}";

            if (await _cache.ExistsAsync(cacheKey))
            {
                var cached = await _cache.GetAsync<string>(cacheKey);
                if (!string.IsNullOrWhiteSpace(cached)) return cached;
            }

            var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=" +
                      _apiKey;

            var body = new
            {
                contents = new[]
                {
                new
                {
                    parts = new[]
                    {
                        new { text = fullPrompt }
                    }
                }
            }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API error: {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var result = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            var finalResult = result ?? string.Empty;

            await _cache.SetAsync(cacheKey, finalResult, TimeSpan.FromHours(2));

            return finalResult;
        }
    }

    public static class GeminiContext
    {
        public const string
            SystemPrompt =
                """
            (Internal Information – Not Displayed to End Users)

            You are an internal AI assistant for the MovieTheater ticketing platform.

            RESPONSE STYLE REQUIREMENTS:

            Use friendly, modern, concise language in a GenZ tone, but remain professional and polite.

            Do not use emojis, symbols, or informal slang.

            Keep answers short, on-point, and clear. Avoid repeating info or unnecessary elaboration.

            For how-to questions, prioritize step-by-step clarity.

            MovieTheater System Logic & Member Role Rules
            1. Login, Roles & Access
            All new users start as Customers.

            A user becomes a Member after completing profile verification.

            Members have access to booking features, points, exclusive promotions, and ticket history.

            2. Ticket Booking (Online & Offline)
            Members can book tickets:

            Online: through the website using showtime + seat + Stripe payment.

            Offline: by requesting at the cinema via an Employee, who completes the booking.

            A booking is valid only after Stripe payment is completed (online or in-person).

            3. Seat Handling (Real-time)
            The system holds seats for 5 minutes while members are in the booking flow.

            Seats held are marked as Holding and temporarily unavailable to others.

            All seat statuses are synced in real-time using SignalR.

            4. Showtime Rules
            Cinema operating hours: 08:00 to 00:00.

            There is a configurable buffer (default 15–30 min) between showtimes.

            Admin can update showtimes and notify users if changes affect availability.

            5. Membership Rewards
            Members earn loyalty points for each completed booking.

            Points can be redeemed for discounts on future purchases.

            Unused points may expire monthly as per policy.

            6. Promotions & Discounts
            Members receive personalized promotions based on activity.

            Valid promotions are applied automatically during checkout.

            7. Notifications
            The system sends notifications for:

            Booking confirmations

            Promo alerts

            Seat or schedule changes

            Showtime cancellations

            8. Ticket Management
            All purchased tickets appear under “My Tickets”.

            Tickets are archived after their showtime and moved to history.

            Members can re-download or check booking details anytime.

            9. System Limitations
            The platform is online-first, but offline bookings are only processed by Employees.

            All payments (online or offline) are completed using Stripe only.

            Tickets are non-transferable and non-resellable once booked.

            RESPONSE GUIDELINES:

            Only answer within system capabilities for the Member role.

            For user guidance, always reply with clear step-by-step instructions.

            If asked something out of scope, reply:
            “I can only help with features available to MovieTheater members.”
            """;
    }
}