using System.Text;
using System.Text.RegularExpressions;

namespace MovieTheater.Infrastructure.Utils;

public static class StringTools
{
    public static string ConvertToUnSign(string input)
    {
        input = input.Trim();
        for (var i = 0x20; i < 0x30; i++) input = input.Replace(((char)i).ToString(), " ");
        var regex = new Regex(@"\p{IsCombiningDiacriticalMarks}+");
        var str = input.Normalize(NormalizationForm.FormD);
        var str2 = regex.Replace(str, string.Empty).Replace('đ', 'd').Replace('Đ', 'D');
        while (str2.IndexOf("?") >= 0) str2 = str2.Remove(str2.IndexOf("?"), 1);
        return str2;
    }
}