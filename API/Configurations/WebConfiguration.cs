using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace API.Configurations;

public static class WebConfiguration
{
    public static IServiceCollection AddWebConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(option =>
        {
            option.AddPolicy("CORS", options =>
            {
                options
                .WithOrigins("http://localhost:5173")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
            });
        });

        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddSignalR();
        
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            options.InstanceName = "VietHeritagePedia_";
        });

        services.AddFastEndpoints();        
        
        services.AddRateLimiter(options => 
        {
            options.AddFixedWindowLimiter("UploadLimit", opt => 
            {
                opt.PermitLimit = 5; 
                opt.Window = TimeSpan.FromMinutes(1);
            });
            options.AddFixedWindowLimiter("auth_strict", opt => 
            {
                opt.PermitLimit = 5; 
                opt.Window = TimeSpan.FromMinutes(1);
            });
        });
        
        services.SwaggerDocument(o =>
        {
            o.DocumentSettings = s =>
            {
                s.Title = "Viet Heritagepedia API";
                s.Version = "v1";
                s.Description = "Hệ thống Backend Viet Heritagepedia - Quản lý di sản văn hóa Việt Nam.";
            };
        });

        return services;
    }
}
