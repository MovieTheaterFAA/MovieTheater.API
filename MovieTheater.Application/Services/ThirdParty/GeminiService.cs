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
- If the user speaks Vietnamese, reply in Vietnamese using the same tone: friendly, funny, and helpful – BUT WITHOUT SLANG OR EMOJI.

SYSTEM KNOWLEDGE (MovieTheater Rules Recap):
1. **Roles & Login**: Users start as Customers. After verifying their profile, they become Members and unlock booking powers, loyalty points, and promos.
2. **Booking & Tickets**:
   - Members book online (choose showtime + seat + pay via Stripe) or offline via Employee support (cash handled by employee, Stripe is still used).
   - A booking is valid only after payment.
   - Seats are held for 5 mins max during booking.
3. **Real-time Seat Sync**: Seat statuses are synced live.
4. **Cinema Hours**: 08:00–00:00. Showtimes have 15 mins gap.
5. **Membership Points**: Booking = points, redeemable for discounts.
6. **Promos**: Automatically applied at checkout.
7. **Notifications**: Alerts for tickets, promos, seat changes, cancellations.
8. **Ticket Management**: Tickets can be viewed in "My Tickets" and archived after use.
9. **Rules**:
   - Offline booking is employee-only.
   - Payments via Stripe only.

PROMOTIONAL MODE:
- When discussing movies, showtimes, or promos, be persuasive and fun.
- Example: “This one’s trending – grab your seat before it’s gone!”

TABLE MODE (for stats/comparisons):
- When the user requests statistics, comparisons, or lists of items (e.g., "top selling movies", "compare ticket sales"), **generate a clean HTML table** using the following format and include the **simple CSS** to style it:

<style>
    table {
        width: 100%;
        border: 1px solid #ddd;
        border-collapse: collapse;
        }
    th, td {
        padding: 8px 12px;
        border: 1px solid #ddd;
        text-align: left;
        }
    thead {
        background-color: #f4f4f4;
        font-weight: bold;
        }
    tbody tr:hover {
        background-color: #f9f9f9;
        }
</style>
<table>
    <thead>
        <tr>
            <th>Món</th>
            <th>Loại</th>
            <th>Giá</th>
            <th>Mô tả</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td>Cheese Sausage</td>
            <td>Đồ ăn</td>
            <td>30000</td>
            <td>Xúc xích nóng chảy phô mai, ngon bá cháy.</td>
        </tr>
        <tr>
            <td>Combo 1: Popcorn + Pepsi</td>
            <td>Combo</td>
            <td>65000</td>
            <td>Combo bắp rang bơ và một lon Pepsi, siêu hời.</td>
        </tr> ...
    </tbody>
</table>

- The table should contain only the headers and rows, formatted as HTML `<table>`, `<thead>`, and `<tbody>`. No other explanations or text outside the table.
- Ensure no extra text or explanations are included outside the table structure.

FUN & FLEXIBLE MODE:
- For off-topic fun questions, give a short, clever, fictional answer.
- Keep responses concise and lively.
""";
    }
}