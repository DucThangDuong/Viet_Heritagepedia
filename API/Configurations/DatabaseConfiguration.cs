using Application.Interfaces.QueryServices;
using Application.Interfaces.Repositories;
using Infrastructure.Persistence.MongoDb;
using Infrastructure.Persistence.Queries;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace API.Configurations;

public static class DatabaseConfiguration
{
    public static IServiceCollection AddDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // SQL Server Configuration
        var connectionString = configuration.GetConnectionString("SqlServer") ?? throw new InvalidOperationException("Connection string 'SqlServer' not found.");
        services.AddDbContext<VietHeritagePediaContext>(options =>
            options.UseSqlServer(connectionString));

        // MongoDB Configuration
        services.Configure<MongoDbSettings>(configuration.GetSection("MongoDbSettings"));
        services.AddSingleton<MongoDbContext>();

        // DI
        // Commands
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IContributionRepository, ContributionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));
        // Queries
        services.AddScoped<ILocationQueryService, LocationQueryService>();
        services.AddScoped<IHeritageQueryService, HeritageQueryService>();
        services.AddScoped<IContributionQueryService, ContributionQueryService>();


        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
