using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace API.BackgroundServices;

public class RedisSubscriberService : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<DocumentProcessingHub> _hubContext;
    private readonly ILogger<RedisSubscriberService> _logger;

    public RedisSubscriberService(
        IConnectionMultiplexer redis,
        IHubContext<DocumentProcessingHub> hubContext,
        ILogger<RedisSubscriberService> logger)
    {
        _redis = redis;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = _redis.GetSubscriber();
        var channelPattern = new RedisChannel("document.stream.*", RedisChannel.PatternMode.Pattern);

        await subscriber.SubscribeAsync(channelPattern, async (channel, message) =>
        {
            try
            {
                var payload = message.ToString();
                if (string.IsNullOrEmpty(payload)) return;
                using var document = JsonDocument.Parse(payload);
                var root = document.RootElement;
                
                if (root.TryGetProperty("JobId", out var jobIdElement) && root.TryGetProperty("Text", out var textElement))
                {
                    var jobId = jobIdElement.GetString();
                    var text = textElement.GetString();
                    
                    int index = 0;
                    if (root.TryGetProperty("Index", out var indexElement) && indexElement.ValueKind == JsonValueKind.Number)
                    {
                        index = indexElement.GetInt32();
                    }

                    if (!string.IsNullOrEmpty(jobId) && !string.IsNullOrEmpty(text))
                    {
                        await _hubContext.Clients.Group(jobId).SendAsync("ReceiveChunk", new
                        {
                            JobId = jobId,
                            Index = index,
                            Text = text
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Redis message on channel {Channel}", channel);
            }
        });

        _logger.LogInformation("RedisSubscriberService started. Subscribed to pattern: document.stream.*");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
