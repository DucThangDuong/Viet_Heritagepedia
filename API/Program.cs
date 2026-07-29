using FastEndpoints;
using FastEndpoints.Swagger;

namespace API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register API, Infrastructure, and Application services directly in API layer
        builder.Services.AddApiDependencies(builder.Configuration)
                        .AddApiConfiguration(builder.Configuration);

        // Register FastEndpoints & Swagger
        builder.Services.AddFastEndpoints();
        builder.Services.SwaggerDocument(o =>
        {
            o.DocumentSettings = s =>
            {
                s.Title = "Viet Heritagepedia API";
                s.Version = "v1";
                s.Description = "Hệ thống Backend Viet Heritagepedia - Quản lý di sản văn hóa Việt Nam.";
            };
        });

        var app = builder.Build();

        var supportedCultures = new[] { "vi", "en" };
        var localizationOptions = new RequestLocalizationOptions()
            .SetDefaultCulture(supportedCultures[0])
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);

        app.UseRequestLocalization(localizationOptions);

        app.UseCors("CORS");
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseFastEndpoints(c => 
        {
            c.Errors.ResponseBuilder = (failures, ctx, statusCode) =>
            {
                var localizer = ctx.RequestServices.GetService<Microsoft.Extensions.Localization.IStringLocalizer<API.SharedResource>>();
                
                var errors = System.Linq.Enumerable.Select(failures, f => new 
                {
                    propertyName = f.PropertyName,
                    errorMessage = localizer?[f.ErrorMessage]?.ToString() ?? f.ErrorMessage
                }).ToList();

                return new API.DTOs.ApiErrorResponse
                {
                    Message = localizer?["ERR_VALIDATION_FAILED"]?.ToString() ?? "Validation Failed",
                    ErrorCode = "ERR_BAD_REQUEST",
                    Errors = errors,
                    TraceId = ctx.TraceIdentifier
                };
            };
        });
        app.UseSwaggerGen();
        app.MapHub<API.Hubs.DocumentProcessingHub>("/hubs/document-processing");

        app.Run();
    }
}
