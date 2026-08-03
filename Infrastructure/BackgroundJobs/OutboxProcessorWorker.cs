using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Contracts;
using Domain.Entities;
using Infrastructure.Persistence.SqlServer;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs;
public class OutboxProcessorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorWorker> _logger;

    /// Khoảng thời gian giữa mỗi lần Worker poll DB để kiểm tra tin nhắn chưa xử lý.
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);
    private const int BatchSize = 20;

    public OutboxProcessorWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessorWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxProcessorWorker encountered an error. Will retry after delay.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        _logger.LogInformation("OutboxProcessorWorker stopped.");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VietHeritagePediaContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (messages.Count == 0) return;

        _logger.LogInformation("OutboxProcessorWorker found {Count} unprocessed message(s).", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                await PublishEventAsync(publishEndpoint, message, ct);

                message.MarkAsProcessed();
                _logger.LogInformation(
                    "Outbox message {Id} ({Type}) published and marked as processed.",
                    message.Id, message.MessageType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish outbox message {Id} ({Type}). Will retry next cycle.",
                    message.Id, message.MessageType);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private static async Task PublishEventAsync(
        IPublishEndpoint publishEndpoint,
        OutboxMessage message,
        CancellationToken ct)
    {
        switch (message.MessageType)
        {
            case "ContributionSubmittedEvent":
                var contributionEvent = JsonSerializer.Deserialize<ContributionSubmittedEvent>(message.Payload)
                    ?? throw new InvalidOperationException($"Failed to deserialize payload for OutboxMessage {message.Id}");
                await publishEndpoint.Publish(contributionEvent, ct);
                break;
            default:
                throw new NotSupportedException($"Unknown outbox message type: '{message.MessageType}'");
        }
    }
}
