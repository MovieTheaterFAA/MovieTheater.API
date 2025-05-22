namespace MovieTheater.Application.Utils;

public class ApiResult
{
    public bool IsSuccess { get; set; }
    public bool IsFailure => !IsSuccess;
    public ResponseContent Value { get; set; }
    public ErrorContent Error { get; set; }

    public static ApiResult Success(string code = "200", string message = "Operation successful.")
    {
        return new ApiResult
        {
            IsSuccess = true,
            Value = new ResponseContent
            {
                Code = code,
                Message = message
            },
            Error = null
        };
    }

    public static ApiResult Failure(string code = "400", string message = "Operation failed.")
    {
        return new ApiResult
        {
            IsSuccess = false,
            Value = null,
            Error = new ErrorContent
            {
                Code = code,
                Message = message
            }
        };
    }
}

public class ApiResult<T>
{
    public bool IsSuccess { get; set; }
    public bool IsFailure => !IsSuccess;
    public ResponseDataContent<T> Value { get; set; }
    public ErrorContent Error { get; set; }

    public static ApiResult<T> Success(T data, string code = "200", string message = "Operation successful.")
    {
        return new ApiResult<T>
        {
            IsSuccess = true,
            Value = new ResponseDataContent<T>
            {
                Code = code,
                Message = message,
                Data = data
            },
            Error = null
        };
    }

    public static ApiResult<T> Failure(string code = "400", string message = "Operation failed.")
    {
        return new ApiResult<T>
        {
            IsSuccess = false,
            Value = null,
            Error = new ErrorContent
            {
                Code = code,
                Message = message
            }
        };
    }
}

public class ResponseContent
{
    public string Code { get; set; }
    public string Message { get; set; }
}

public class ResponseDataContent<T>
{
    public string Code { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
}

public class ErrorContent
{
    public string Code { get; set; }
    public string Message { get; set; }
}