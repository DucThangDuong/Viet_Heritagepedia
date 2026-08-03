using API.Configurations;
using API.Middlewares;
using FastEndpoints;
using FastEndpoints.Swagger;

namespace API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // --- Services ---
        builder.Services.AddDatabaseConfiguration(builder.Configuration);
        builder.Services.AddAuthConfiguration(builder.Configuration);
        builder.Services.AddMessagingConfiguration(builder.Configuration);
        builder.Services.AddStorageConfiguration(builder.Configuration);
        builder.Services.AddWebConfiguration(builder.Configuration);
        var app = builder.Build();
        // --- Middlewares ---
        var supportedCultures = new[] { "vi", "en" };
        var localizationOptions = new RequestLocalizationOptions()
            .SetDefaultCulture(supportedCultures[0])
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);

        app.UseSecurityHeaders();
        app.UseRequestLocalization(localizationOptions);

        app.UseCors("CORS");
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        
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
