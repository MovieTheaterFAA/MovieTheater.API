using System.Security.Cryptography;
using System.Text;

namespace MovieTheater.Infrastructure.Utils
{
    public class HashHelper
    {
        public static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue) hash.Append(theByte.ToString("x2"));
            }

            return hash.ToString();
        }

        public static string HmacSHA256(string inputData, string key)
        {
            var keyByte = Encoding.UTF8.GetBytes(key);
            var messageBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                var hashmessage = hmacsha256.ComputeHash(messageBytes);
                var hex = BitConverter.ToString(hashmessage);
                hex = hex.Replace("-", "").ToLower();
                return hex;
            }
        }
    }
}
