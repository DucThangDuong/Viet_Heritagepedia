using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.Interfaces.Storage;
using Infrastructure.Storage;
using Amazon.S3;
namespace API.Configurations;

public static class StorageConfiguration
{
    public static IServiceCollection AddStorageConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. S3 Client for MinIO 
        var s3Config = new AmazonS3Config
        {
            ServiceURL = configuration["S3:Endpoint"] ?? "http://localhost:9000",
            ForcePathStyle = true
        };
        var accessKey = configuration["S3:AccessKey"] ?? "minioadmin";
        var secretKey = configuration["S3:SecretKey"] ?? "minioadmin123";
        services.AddSingleton<IAmazonS3>(new AmazonS3Client(accessKey, secretKey, s3Config));

        // File Storage Service
        services.AddScoped<IFileStorageService, MinioFileStorageService>();

        // 2. Azure Storage for Images
        var azureConnString = configuration["Azure:StorageConnectionString"] ?? "UseDevelopmentStorage=true";
        services.AddScoped<IImageStorageService>(provider => 
            new AzureImageStorageService(azureConnString, "vietheritage-images"));

        return services;
    }
}
