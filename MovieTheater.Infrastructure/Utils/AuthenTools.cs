using System.Security.Claims;

#pragma warning disable CS8603

namespace MovieTheater.Infrastructure.Utils;

public static class AuthenTools
{
    public static string? GetCurrentUserId(ClaimsIdentity? identity)
    {
        if (identity == null)
            return null;

        var userId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // Log userId value
        Console.WriteLine($"Extracted UserId from claims: {userId}");
        return userId;
    }
}