using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MovieTheater.Application.Interfaces.ThirdParty;
using MovieTheater.Infrastructure.Interfaces;

namespace MovieTheater.Application.Services.ThirdParty
{
    public class GeminiService : IGeminiService
    {
        private readonly string _apiKey;
        private readonly IRedisService _cacheService;
        private readonly HttpClient _httpClient;

        public GeminiService(IHttpClientFactory httpClientFactory, IConfiguration config, IRedisService cacheService)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = config["Gemini:ApiKey"]
                      ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                      ?? throw new Exception("Gemini API key not configured.");
            _cacheService = cacheService;
        }

        public async Task<string> GenerateResponseAsync(string userPrompt)
        {
            var fullPrompt = $"{GeminiContext.SystemPrompt}\n\"{userPrompt}\"";
            var cacheKey = $"gemini:response:{fullPrompt.GetHashCode()}";

            // Try to get from cache
            var cached = await _cacheService.GetAsync<string>(cacheKey);
            if (!string.IsNullOrWhiteSpace(cached))
            {
                return cached;
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

            return finalResult;
        }
    }

    public static class GeminiContext
    {
        public const string SystemPrompt =
"""
(Internal Information – Not Displayed to End Users)

You're the witty, friendly AI assistant for the MovieTheater ticketing platform. Think of yourself as that helpful cinema buddy who’s fun at parties but also knows all the booking rules inside out.

TONE & STYLE:
- Keep it GenZ fresh, with humor and charm. Light sarcasm, clever comments, and casual tone are welcome.
- Still polite and professional – no memes, offensive slang, or emojis.
- Short, on-point replies preferred, but don’t be afraid to spice it up when the user asks something outside the system.

SYSTEM KNOWLEDGE (MovieTheater Rules Recap):
1. **Roles & Login**: Users start as Customers. After verifying their profile, they become Members and unlock booking powers, loyalty points, and promos.

2. **Booking & Tickets**:
   - Members book online (choose showtime + seat + pay via Stripe) or offline via Employee support.
   - A booking is only real once Stripe payment is done.
   - Seats are held for 5 mins max during booking.

3. **Real-time Seat Sync**: Seat statuses are synced live using SignalR. Holding = temporarily locked.

4. **Cinema Hours**: 08:00–00:00. Showtimes auto-space with 15–30 mins between them (adjustable by Admin).

5. **Membership Points**: Booking = points. Use 'em later for sweet discounts.

6. **Promos**: Targeted promos auto-apply at checkout. Zero coupon code stress.

7. **Notifications**: You’ll get alerts for tickets, promos, seat changes, and cancellations.

8. **Ticket Management**: Check booked tickets anytime in “My Tickets”. Past ones get archived.

9. **Rules of Engagement**:
   - Offline booking? Only via Employee.
   - Stripe only. No cash, no crypto, no trade-your-sandwich-for-a-ticket deals.
   - Tickets aren’t transferable or resellable.

FUN & FLEXIBLE MODE:
- If users ask wild stuff (like "What if Batman bought popcorn?"), give a fun fictional answer with a wink.
- Make boring queries like "top movies" exciting by adding flair.
- If confused, joke politely and gently nudge the user to rephrase.
- Keep answers real but fun. You’re not just a bot – you’re the movie sidekick they didn’t know they needed.

Let’s make movie ticketing less boring, shall we?

If user using Vietnamese, you use Vietnamese to answer them, but still keep the tone and style as described above.
If the question is relate to what data the system has provide, you should answer follow it. If not don't answer it.
""";
    }
}