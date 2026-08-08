using API.BackgroundServices;
using Application.Common;
using MassTransit;

namespace API.Configurations;

public static class MessagingConfiguration
{
    public static IServiceCollection AddMessagingConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Result).Assembly);
        });

        // MassTransit + RabbitMQ
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

                cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                cfg.ConfigureEndpoints(context);
            });
        });

        // Redis
        var redisConnString = configuration["Redis:ConnectionString"] ?? "localhost:6379";
        services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp => 
            StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnString));

        // Background Jobs
        services.AddHostedService<RedisSubscriberService>();

        return services;
    }
}
