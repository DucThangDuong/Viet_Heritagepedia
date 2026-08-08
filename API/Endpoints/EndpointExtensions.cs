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
            string message = "SUCCESS_OPERATION",
            string errorCode = "ERR_BAD_REQUEST")
        {
            if (result.StatusCode == 204 || result.StatusCode == 205 || result.StatusCode == 304)
            {
                ep.HttpContext.Response.StatusCode = result.StatusCode;
                return;
            }

            var localizer = ep.HttpContext.RequestServices.GetService<Microsoft.Extensions.Localization.IStringLocalizer<API.SharedResource>>();
            if (result.IsSuccess)
            {
                var response = new ApiSuccessResponse<TData>
                {
                    Message = localizer?[message]?.Value ?? message,
                    Data = result.Data,
                };
                await ep.HttpContext.Response.SendAsync(response, result.StatusCode, cancellation: ct);
            }
            else
            {
                var localizedError = localizer?[result.ErrorCode ?? "ERR_UNKNOWN"];
                var response = new ApiErrorResponse
                {
                    Message = (localizedError != null && !localizedError.ResourceNotFound) ? localizedError.Value : "Đã xảy ra lỗi",
                    ErrorCode = result.ErrorCode ?? (result.StatusCode == 500 ? "ERR_INTERNAL_SERVER" : errorCode),
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
            string message = "SUCCESS_OPERATION",
            string defaultErrorCode = "ERR_BAD_REQUEST")
        {
            if (result.StatusCode == 204 || result.StatusCode == 205 || result.StatusCode == 304)
            {
                ep.HttpContext.Response.StatusCode = result.StatusCode;
                return;
            }
            var localizer = ep.HttpContext.RequestServices.GetService<Microsoft.Extensions.Localization.IStringLocalizer<API.SharedResource>>();
            if (result.IsSuccess)
            {
                var response = new ApiSuccessResponse<object>
                {
                    Message = localizer?[message]?.Value ?? message,
                };
                await ep.HttpContext.Response.SendAsync(response, result.StatusCode, cancellation: ct);
            }
            else
            {
                var localizedError = localizer?[result.ErrorCode ?? "ERR_UNKNOWN"];
                var response = new ApiErrorResponse
                {
                    Message = (localizedError != null && !localizedError.ResourceNotFound) ? localizedError.Value : "Đã xảy ra lỗi",
                    ErrorCode = result.ErrorCode ?? (result.StatusCode == 500 ? "ERR_INTERNAL_SERVER" : defaultErrorCode),
                    Errors = result.Errors != null && result.Errors.Any() ? result.Errors : null,
                    TraceId = ep.HttpContext.TraceIdentifier
                };
                await ep.HttpContext.Response.SendAsync(response, result.StatusCode, cancellation: ct);
            }
        }
    }
}
