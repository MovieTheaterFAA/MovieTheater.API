using MovieTheater.Application.Utils;

namespace MovieTheater.Application.Utils;

public static class ExceptionUtils
{
    public static int ExtractStatusCode(Exception ex)
    {
        if (ex.Data.Contains("StatusCode") && int.TryParse(ex.Data["StatusCode"]?.ToString(), out var code))
            return code;
        return 500;
    }

    public static string ExtractMessage(Exception ex)
    {
        return ex.Message ?? "Lỗi không xác định.";
    }

    public static ApiResult<T> CreateErrorResponse<T>(Exception ex)
    {
        var code = ExtractStatusCode(ex).ToString();
        var message = ExtractMessage(ex);
        return ApiResult<T>.Failure(code, message);
    }
}