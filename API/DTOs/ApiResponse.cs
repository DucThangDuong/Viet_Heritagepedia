namespace API.DTOs;

public class ApiSuccessResponse<TData>
{
    public string Message { get; set; } = "SUCCESS_OPERATION";
    public TData? Data { get; set; }
}

public class ApiErrorResponse
{
    public string Message { get; set; } = "Đã xảy ra lỗi";
    public string ErrorCode { get; set; } = "ERR_BAD_REQUEST";
    public object? Errors { get; set; }
    public string? TraceId { get; set; }
}
