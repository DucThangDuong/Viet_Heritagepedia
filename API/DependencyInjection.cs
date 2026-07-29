using Application.Common;
using Application.Interfaces.Auth;
using Application.Interfaces.QueryServices;
using Application.Interfaces.Repositories;
using Application.IServices;
using FastEndpoints;
using Infrastructure.BackgroundJobs;
using Infrastructure.Persistence.MongoDb;
using Infrastructure.Persistence.Queries;
using Infrastructure.Persistence.SqlServer;
using Infrastructure.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

namespace API;

public static class DependencyInjection
{
    public static IServiceCollection AddApiConfiguration(this IServiceCollection services, IConfiguration configuration)
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
        return services;
    }
    public static IServiceCollection AddApiDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. SQL Server Configuration
        var connectionString = configuration.GetConnectionString("SqlServer") ?? throw new InvalidOperationException("Connection string 'SqlServer' not found.");

        services.AddDbContext<VietHeritagePediaContext>(options =>
            options.UseSqlServer(connectionString));

        // Register Custom Strongly-Typed Repositories (Write / Domain Rules)
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IContributionRepository, ContributionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

        // Register Pure Transaction-Only Unit of Work (Write)
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register Auth Services
        services.AddScoped<IJWTTokenServices, JwtTokenService>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddSingleton<ITokenCacheService, TokenCacheService>();

        // Register JWT Authentication & Authorization
        var jwtSecretKey = configuration["Jwt:SecretKey"] ??throw new InvalidOperationException("Jwt:SecretKey not found in configuration.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization();

        // Register Query Services (Read Path returning DTOs)
        services.AddScoped<ILocationQueryService, LocationQueryService>();
        services.AddScoped<IHeritageQueryService, HeritageQueryService>();
        services.AddScoped<IContributionQueryService, ContributionQueryService>();

        // Register File Storage Service
        services.AddScoped<Application.Interfaces.Storage.IFileStorageService, Infrastructure.Storage.LocalFileStorageService>();

        // 2. MongoDB Configuration
        services.Configure<MongoDbSettings>(configuration.GetSection("MongoDbSettings"));
        services.AddSingleton<MongoDbContext>();
        services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));

        // 3. Register Application MediatR Handlers directly in API Layer
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Result).Assembly);
        });

        // 4. MassTransit + RabbitMQ Configuration
        services.AddMassTransit(x =>
        {
            x.AddConsumer<Consumers.LocationDocumentProcessedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitHost = configuration["RabbitMQ:Host"] ?? "localhost";
                var rabbitUser = configuration["RabbitMQ:Username"] ?? "guest";
                var rabbitPass = configuration["RabbitMQ:Password"] ?? "guest";

                cfg.Host(rabbitHost, "/", h =>
                {
                    h.Username(rabbitUser);
                    h.Password(rabbitPass);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        // 5. Background Jobs
        services.AddHostedService<OutboxProcessorWorker>();

        return services;
    }
}
