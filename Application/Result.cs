namespace Application.Common;

public record ErrorDetail(string Field, string Message);

public class Result
{
    public bool IsSuccess { get; }
    public string? ErrorCode { get; }
    public List<ErrorDetail>? Errors { get; }
    public int StatusCode { get; }

    protected Result(bool isSuccess, string? error, int statusCode, List<ErrorDetail>? Errors = null)
    {
        IsSuccess = isSuccess;
        ErrorCode = error;
        StatusCode = statusCode;
        this.Errors = Errors;
    }

    public static Result Success(int status = 201) => new(true, null, status);
    public static Result Failure(string error, int statusCode = 400, List<ErrorDetail>? validationErrors = null) => new(false, error, statusCode, validationErrors);
}

public class Result<T> : Result
{
    public T? Data { get; }

    private Result(bool isSuccess, T? data, string? errorCode, int statusCode, List<ErrorDetail>? Errors = null)
        : base(isSuccess, errorCode, statusCode, Errors)
    {
        Data = data;
    }

    public static Result<T> Success(T data, int status = 200) => new(true, data, null, status);
    public static Result<T> Failure(string error, int statusCode = 400, T? data = default, List<ErrorDetail>? validationErrors = null) => new(false, data, error, statusCode, validationErrors);
}
