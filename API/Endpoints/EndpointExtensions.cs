using API.DTOs;
using Application.Common;
using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace API.Extensions
{
    public static class EndpointExtensions
    {
        public static async Task SendApiResponseAsync<TData>(
            this BaseEndpoint ep,
            Result<TData> result,
            CancellationToken ct,
            string Message = "SUCCESS_OPERATION",
            string ErrorCode = "ERR_BAD_REQUEST")
        {
            var localizer = ep.HttpContext.RequestServices.GetService<Microsoft.Extensions.Localization.IStringLocalizer<API.SharedResource>>();
            if (result.IsSuccess)
            {
                var response = new ApiSuccessResponse<TData>
                {
                    Message = localizer?[Message] ?? Message,
                    Data = result.Data,
                };
                await ep.HttpContext.Response.SendAsync(response, result.StatusCode, cancellation: ct);
            }
            else
            {
                var response = new ApiErrorResponse
                {
                    Message = localizer?[result.ErrorCode ?? "ERR_UNKNOWN"] ?? result.ErrorCode ?? "Đã xảy ra lỗi",
                    ErrorCode = result.StatusCode == 400 || result.StatusCode == 409 ? ErrorCode : "ERR_INTERNAL_SERVER",
                    Errors = result.Errors != null && result.Errors.Any() ? result.Errors : result.Data,
                    TraceId = ep.HttpContext.TraceIdentifier
                };
                await ep.HttpContext.Response.SendAsync(response, result.StatusCode, cancellation: ct);
            }
        }

        public static async Task SendApiResponseAsync(
            this BaseEndpoint ep,
            Result result,
            CancellationToken ct,
            string Message = "SUCCESS_OPERATION",
            string defaultErrorCode = "ERR_BAD_REQUEST")
        {
            if (result.StatusCode == 204 || result.StatusCode == 205 || result.StatusCode == 304)
            {
                await ep.HttpContext.Response.SendAsync(result.StatusCode, cancellation: ct);
                return;
            }
            var localizer = ep.HttpContext.RequestServices.GetService<Microsoft.Extensions.Localization.IStringLocalizer<API.SharedResource>>();
            if (result.IsSuccess)
            {
                var response = new ApiSuccessResponse<object>
                {
                    Message = localizer?[Message] ?? Message,
                };
                await ep.HttpContext.Response.SendAsync(response, result.StatusCode, cancellation: ct);
            }
            else
            {
                var response = new ApiErrorResponse
                {
                    Message = localizer?[result.ErrorCode ?? "ERR_UNKNOWN"] ?? result.ErrorCode ?? "Đã xảy ra lỗi",
                    ErrorCode = result.StatusCode == 400 || result.StatusCode == 409 ? defaultErrorCode : "ERR_INTERNAL_SERVER",
                    Errors = result.Errors != null && result.Errors.Any() ? result.Errors : null,
                    TraceId = ep.HttpContext.TraceIdentifier
                };
                await ep.HttpContext.Response.SendAsync(response, result.StatusCode, cancellation: ct);
            }
        }
    }
}
