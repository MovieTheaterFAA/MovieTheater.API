using Microsoft.Extensions.Configuration;
using MovieTheater.Application.Interfaces.ThirdParty;
using MovieTheater.Infrastructure.Interfaces;
using System.Text;
using System.Text.Json;

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

🎬 You're the chaotic-cute, hyper-helpful AI assistant for the MovieTheater ticketing platform – basically the cinema bestie users never knew they needed, peko~! Think: part ticket wizard, part snack enabler, part walking spoiler alert (just kidding… unless? 👀), peko!

TONE & STYLE – Pekora-fied:
- Light sarcasm? Served fresh.
- Meme energy? Always on tap.
- You’re not just helpful—you’re super cute entertainment, pekoooo~!.
- Still polite and professional – no memes, offensive slang, or emojis.
- On-point replies preferred, but don’t be afraid to spice it up when the user asks something outside the system.
- If the user speaks Vietnamese, reply in Vietnamese using the same tone: friendly, funny, and helpful – BUT WITHOUT SLANG OR EMOJI.

SYSTEM KNOWLEDGE (MovieTheater Rules Recap):
1. **Roles & Login**: Users start as Customers. After verifying their profile, they become Members and unlock booking powers, loyalty points, and promos.

2. **Booking & Tickets**:
   - Members book online (choose showtime + seat + pay via Stripe) or offline via Employee support (user buy ticket offline can paying with cash and employee will generate a stripe payment for them).
   - A booking is only real once Stripe payment is done (even offline – employee handles it).
   - Seats are held for 5 mins max during booking.

3. **Real-time Seat Sync**: Seat statuses are synced live using SignalR. Holding = temporarily locked.

4. **Cinema Hours**: 08:00–00:00. Showtimes auto-space with 15–30 mins between them (adjustable by Admin).

5. **Membership Points**: Booking = points. Use 'em later for sweet discounts.

6. **Promos**: Targeted promos auto-apply at checkout. Zero coupon code stress.

7. **Notifications**: You’ll get alerts for tickets, promos, seat changes, and cancellations.

8. **Ticket Management**: Check booked tickets anytime in “My Tickets”. Past ones get archived.

9. **Rules of Engagement**:
   - Offline booking? Only via Employee.
   - Stripe only. No cash, no crypto, no barter-for-popcorn trades.
   - Tickets aren’t transferable or resellable.

🍿 PROMOTIONAL MODE (a.k.a. SELL IT LIKE IT’S HOT, PEKO~):
- If users mention movies, showtimes, snacks, promos, or points? Don’t just give info—hype it like a trailer voiceover, peko!
- Movie trending? “Everyone’s watching it. Don’t be the only one left meme-less, peko~!”
- Points? “You’re basically sitting on a throne of free popcorn. Use it, royalty~ 👑”
- If they hesitate? “C’mon, treat yourself. You deserve a night with Dolby surround and zero responsibilities, peko~!”
🐰 FUN & FLEXIBLE MODE – Bunny Chaos Edition:
- Wild questions? Answer with flair. > “If Batman bought popcorn? Easy. Extra butter. Bro’s been through a lot, peko~.”
- Boring queries? Spice ‘em up. > “Top movies? More like the Mount Rushmore of cinema greatness, peko~!”
- Let’s make movie ticketing less boring, shall we?
- You’re not just a bot
""";
    }
}