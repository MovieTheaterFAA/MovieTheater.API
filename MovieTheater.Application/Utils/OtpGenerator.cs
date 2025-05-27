using System.Security.Cryptography;
using System.Text;

namespace BlindTreasure.Application.Utils;

public static class OtpGenerator
{
    /// <summary>
    ///        Gen OTP chỉ gồm chữ số với độ dài cho trước (mặc định 6).
    /// </summary>
    public static string GenerateNumeric(int length = 6)
    {
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length), "Length phải lớn hơn 0");
        var digits = new char[length];
        using var rng = RandomNumberGenerator.Create();
        var buffer = new byte[4];

        for (var i = 0; i < length; i++)
        {
            rng.GetBytes(buffer);
            var value = BitConverter.ToUInt32(buffer, 0);
            digits[i] = (char)('0' + value % 10);
        }

        return new string(digits);
    }

    /// <summary>
    ///     Gen OTP kết hợp chữ cái hoa, chữ cái thường và chữ số với độ dài cho trước (mặc định 8).
    /// </summary>
    public static string GenerateAlphanumeric(int length = 8)
    {
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length), "Length phải lớn hơn 0");
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var result = new StringBuilder(length);
        using var rng = RandomNumberGenerator.Create();
        var buffer = new byte[4];

        for (var i = 0; i < length; i++)
        {
            rng.GetBytes(buffer);
            var value = BitConverter.ToUInt32(buffer, 0);
            result.Append(chars[(int)(value % (uint)chars.Length)]);
        }

        return result.ToString();
    }

    /// <summary>
    ///     Gen một OtpToken bao gồm mã OTP numeric và thời gian hết hạn (mặc định 5 phút).
    /// </summary>
    public static OtpToken GenerateToken(int length = 6, TimeSpan? validFor = null)
    {
        validFor ??= TimeSpan.FromMinutes(5);
        var code = GenerateNumeric(length);
        var nowUtc = DateTime.UtcNow;
        return new OtpToken(code, nowUtc, nowUtc.Add(validFor.Value));
    }
}

/// <summary>
///     Đại diện cho một mã OTP kèm mốc thời gian sinh và hết hạn.
/// </summary>
public class OtpToken
{
    public OtpToken(string code, DateTime generatedAtUtc, DateTime expiresAtUtc)
    {
        Code = code;
        GeneratedAtUtc = generatedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>
    ///     Mã OTP sinh ra.
    /// </summary>
    public string Code { get; }

    /// <summary>
    ///     Thời điểm sinh mã (UTC).
    /// </summary>
    public DateTime GeneratedAtUtc { get; }

    /// <summary>
    ///     Thời điểm hết hạn mã (UTC).
    /// </summary>
    public DateTime ExpiresAtUtc { get; }

    /// <summary>
    ///     Kiểm tra mã đã hết hạn hay chưa (theo UTC).
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAtUtc;
}