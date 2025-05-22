using System.Security.Cryptography;
using System.Text;

namespace MovieTheater.Application.Utils;

public class PasswordHasher
{
    private const int SaltSize = 16; // 128-bit salt
    private const int HashSize = 32; // 256-bit hash

    public string? HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty.");

        using (var rng = new RNGCryptoServiceProvider())
        {
            // Generate a random salt
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);

            // Hash the password
            var hash = HashPasswordWithSalt(password, salt);

            // Combine the salt and hash into a single string
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }
    }

    public bool VerifyPassword(string password, string? storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            return false;

        var parts = storedHash.Split(':');
        if (parts.Length != 2)
            return false;

        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);

        // Hash the input password with the stored salt
        var hashToVerify = HashPasswordWithSalt(password, salt);

        // Compare the stored hash with the computed hash
        return CryptographicOperations.FixedTimeEquals(hash, hashToVerify);
    }

    private byte[] HashPasswordWithSalt(string password, byte[] salt)
    {
        using (var hmac = new HMACSHA256(salt))
        {
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }
    }
}